using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using AnomieNomnomPublisher.Models;

namespace AnomieNomnomPublisher;

public static class ManifestBuilder
{
    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static string Build(ManifestInput input)
    {
        Validate(input);
        var mod = new JsonObject
        {
            ["id"] = input.ModId,
            ["displayName"] = input.DisplayName,
            ["description"] = input.Description,
            ["tags"] = StringArray(input.Tags),
            ["urls"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "info",
                    ["url"] = input.InfoUrl
                }
            },
            ["authors"] = StringArray(input.Authors)
        };

        if (input.AutoUpdateArtifacts)
        {
            mod["githubOwner"] = input.GitHubOwner;
            mod["githubRepoName"] = input.GitHubRepoName;
            mod["autoUpdateArtifacts"] = "True";
        }

        var artifact = new JsonObject
        {
            ["type"] = input.ArtifactType,
            ["fileName"] = input.FileName,
            ["downloadUrl"] = input.DownloadUrl,
            ["hash"] = input.Hash,
            ["gameVersion"] = input.GameVersion,
            ["version"] = input.Version,
            ["category"] = input.Category
        };

        if (string.Equals(input.ArtifactType, "addOn", StringComparison.OrdinalIgnoreCase))
        {
            artifact["extends"] = new JsonObject
            {
                ["id"] = input.ExtendsId,
                ["version"] = input.ExtendsVersion
            };
        }

        if (input.Dependencies.Count > 0) artifact["dependencies"] = RelationArray(input.Dependencies);
        if (input.Incompatibilities.Count > 0) artifact["incompatibilities"] = RelationArray(input.Incompatibilities);
        mod["artifacts"] = new JsonArray { artifact };
        return mod.ToJsonString(PrettyJsonOptions);
    }

    public static List<ManifestRelation> ParseRelations(string raw)
    {
        var result = new List<ManifestRelation>();
        if (string.IsNullOrWhiteSpace(raw)) return result;
        var parts = raw.Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            var chunks = part.Contains('@') ? part.Split('@', 2, StringSplitOptions.TrimEntries) : part.Split(':', 2, StringSplitOptions.TrimEntries);
            if (chunks.Length == 2 && !string.IsNullOrWhiteSpace(chunks[0]) && !string.IsNullOrWhiteSpace(chunks[1]))
            {
                result.Add(new ManifestRelation { Id = chunks[0], Version = chunks[1] });
            }
        }
        return result;
    }

    public static List<string> ParseList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    }

    private static void Validate(ManifestInput input)
    {
        Require(input.ModId, "Mod ID");
        Require(input.DisplayName, "Display name");
        Require(input.Description, "Description");
        Require(input.InfoUrl, "Info URL");
        Require(input.FileName, "File name");
        Require(input.DownloadUrl, "Download URL");
        Require(input.Hash, "Release hash");
        Require(input.GameVersion, "Game version");
        Require(input.Version, "Version");
        Require(input.Category, "Category");
        Require(input.ArtifactType, "Artifact type");
        if (string.Equals(input.ArtifactType, "addOn", StringComparison.OrdinalIgnoreCase))
        {
            Require(input.ExtendsId, "Extends ID");
            Require(input.ExtendsVersion, "Extends version");
        }
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException($"{name} is required.");
    }

    private static JsonArray StringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x))) array.Add(value.Trim());
        return array;
    }

    private static JsonArray RelationArray(IEnumerable<ManifestRelation> values)
    {
        var array = new JsonArray();
        foreach (var relation in values)
        {
            array.Add(new JsonObject
            {
                ["id"] = relation.Id,
                ["version"] = relation.Version
            });
        }
        return array;
    }
}
