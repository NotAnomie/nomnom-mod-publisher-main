using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Security.Cryptography;

namespace AnomieNomnomPublisher;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public string OAuthClientId { get; set; } = "";
    public string EncryptedAccessToken { get; set; } = "";
    public string GitHubLogin { get; set; } = "";
    public string ReleaseRepo { get; set; } = "Nomnom-Mod-Releases";
    public bool CreateReleaseRepo { get; set; } = true;
    public bool PrivateReleaseRepo { get; set; }
    public string UpstreamOwner { get; set; } = "KopterBuzz";
    public string UpstreamRepo { get; set; } = "NOMNOM";
    public string UpstreamBranch { get; set; } = "main";
    public string ManifestFolder { get; set; } = "modManifests";
    public string DllPath { get; set; } = "";
    public string ExtraFolder { get; set; } = "";
    public string OutputZip { get; set; } = "";
    public string ReleaseHash { get; set; } = "";
    public bool IncludeBepInExLayout { get; set; } = true;
    public string ModId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public Dictionary<string, string> SavedDescriptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string Tags { get; set; } = "QoL,Utility";
    public string Authors { get; set; } = "Anomie";
    public string InfoUrl { get; set; } = "";
    public string GameVersion { get; set; } = "0.32";
    public string Version { get; set; } = "";
    public string ArtifactType { get; set; } = "plugin";
    public string Category { get; set; } = "Release";
    public string Dependencies { get; set; } = "";
    public string Incompatibilities { get; set; } = "";
    public string ExtendsId { get; set; } = "";
    public string ExtendsVersion { get; set; } = "";
    public bool AutoUpdateArtifacts { get; set; } = true;
    public bool CreatePullRequest { get; set; } = true;

    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new AppSettings();
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json, Encoding.UTF8);
    }
}

public static class SecureTokenStore
{
    public static string Protect(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "";
        var bytes = Encoding.UTF8.GetBytes(token);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string encrypted)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encrypted)) return "";
            var bytes = Convert.FromBase64String(encrypted);
            var unprotectedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotectedBytes);
        }
        catch
        {
            return "";
        }
    }
}
