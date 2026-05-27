using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace AnomieNomnomPublisher;

public static class ZipBuilder
{
    public static AssemblyNameInfo ReadAssemblyName(string dllPath)
    {
        var assemblyName = AssemblyName.GetAssemblyName(dllPath);
        var fallbackName = assemblyName.Name ?? Path.GetFileNameWithoutExtension(dllPath);
        var fallbackVersion = assemblyName.Version?.ToString(3) ?? "1.0.0";
        var fallback = new AssemblyNameInfo(fallbackName, fallbackName, NormalizeVersion(fallbackVersion), fallbackName, false);
        return TryReadBepInPluginInfoFromMetadata(dllPath, fallback) ?? fallback;
    }

    private static AssemblyNameInfo? TryReadBepInPluginInfoFromMetadata(string dllPath, AssemblyNameInfo fallback)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var peReader = new PEReader(stream);
            if (!peReader.HasMetadata) return null;

            var reader = peReader.GetMetadataReader();
            foreach (var handle in reader.CustomAttributes)
            {
                var attribute = reader.GetCustomAttribute(handle);
                var attributeTypeName = ResolveCustomAttributeTypeName(reader, attribute);
                if (!attributeTypeName.Contains("BepInPlugin", StringComparison.OrdinalIgnoreCase)) continue;

                var blob = reader.GetBlobReader(attribute.Value);
                if (blob.RemainingBytes < 2) continue;
                if (blob.ReadUInt16() != 0x0001) continue;

                var pluginId = blob.ReadSerializedString();
                var pluginName = blob.ReadSerializedString();
                var pluginVersion = blob.ReadSerializedString();

                var id = string.IsNullOrWhiteSpace(pluginId) ? fallback.ModId : pluginId.Trim();
                var displayName = string.IsNullOrWhiteSpace(pluginName) ? fallback.DisplayName : pluginName.Trim();
                var version = string.IsNullOrWhiteSpace(pluginVersion) ? fallback.Version : NormalizeVersion(pluginVersion);

                return new AssemblyNameInfo(id, displayName, version, fallback.AssemblyName, true);
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string ResolveCustomAttributeTypeName(MetadataReader reader, CustomAttribute attribute)
    {
        return attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => ResolveMemberReferenceTypeName(reader, (MemberReferenceHandle)attribute.Constructor),
            HandleKind.MethodDefinition => ResolveMethodDefinitionTypeName(reader, (MethodDefinitionHandle)attribute.Constructor),
            _ => string.Empty
        };
    }

    private static string ResolveMemberReferenceTypeName(MetadataReader reader, MemberReferenceHandle handle)
    {
        var member = reader.GetMemberReference(handle);
        return ResolveTypeHandleName(reader, member.Parent);
    }

    private static string ResolveMethodDefinitionTypeName(MetadataReader reader, MethodDefinitionHandle handle)
    {
        var method = reader.GetMethodDefinition(handle);
        return ResolveTypeHandleName(reader, method.GetDeclaringType());
    }

    private static string ResolveTypeHandleName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
            {
                var type = reader.GetTypeReference((TypeReferenceHandle)handle);
                return CombineNamespaceAndName(reader.GetString(type.Namespace), reader.GetString(type.Name));
            }
            case HandleKind.TypeDefinition:
            {
                var type = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                return CombineNamespaceAndName(reader.GetString(type.Namespace), reader.GetString(type.Name));
            }
            default:
                return string.Empty;
        }
    }

    private static string CombineNamespaceAndName(string ns, string name)
    {
        if (string.IsNullOrWhiteSpace(ns)) return name;
        if (string.IsNullOrWhiteSpace(name)) return ns;
        return ns + "." + name;
    }

    private static string NormalizeVersion(string version)
    {
        return string.IsNullOrWhiteSpace(version) ? "1.0.0" : version.Trim().TrimStart('v', 'V');
    }

    public static string BuildZip(string dllPath, string extraFolder, string outputZipPath, bool includeBepInExLayout)
    {
        if (!File.Exists(dllPath)) throw new FileNotFoundException("Plugin DLL not found.", dllPath);
        if (string.IsNullOrWhiteSpace(outputZipPath)) throw new InvalidOperationException("Output ZIP path is empty.");
        var dir = Path.GetDirectoryName(outputZipPath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        if (File.Exists(outputZipPath)) File.Delete(outputZipPath);

        using var archive = ZipFile.Open(outputZipPath, ZipArchiveMode.Create);
        var dllEntry = includeBepInExLayout ? $"BepInEx/plugins/{Path.GetFileName(dllPath)}" : Path.GetFileName(dllPath);
        archive.CreateEntryFromFile(dllPath, dllEntry, CompressionLevel.Optimal);

        if (!string.IsNullOrWhiteSpace(extraFolder) && Directory.Exists(extraFolder))
        {
            var baseName = Path.GetFileName(extraFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            foreach (var file in Directory.GetFiles(extraFolder, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(extraFolder, file).Replace('\\', '/');
                var entry = includeBepInExLayout ? $"BepInEx/plugins/{baseName}/{relative}" : $"{baseName}/{relative}";
                archive.CreateEntryFromFile(file, entry, CompressionLevel.Optimal);
            }
        }

        return outputZipPath;
    }
}

public readonly record struct AssemblyNameInfo(string ModId, string DisplayName, string Version, string AssemblyName, bool FromBepInPlugin)
{
    public string Name => DisplayName;
}
