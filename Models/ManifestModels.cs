using System.Collections.Generic;

namespace AnomieNomnomPublisher.Models;

public sealed class ManifestInput
{
    public string ModId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Tags { get; set; } = new();
    public List<string> Authors { get; set; } = new();
    public string InfoUrl { get; set; } = "";
    public string GitHubOwner { get; set; } = "";
    public string GitHubRepoName { get; set; } = "";
    public bool AutoUpdateArtifacts { get; set; }
    public string ArtifactType { get; set; } = "plugin";
    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Hash { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public string Version { get; set; } = "";
    public string Category { get; set; } = "Release";
    public string ExtendsId { get; set; } = "";
    public string ExtendsVersion { get; set; } = "";
    public List<ManifestRelation> Dependencies { get; set; } = new();
    public List<ManifestRelation> Incompatibilities { get; set; } = new();
}

public sealed class ManifestRelation
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "";
}
