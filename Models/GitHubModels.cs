using System.Text.Json.Serialization;

namespace AmonieNomnomPublisher.Models;

public sealed class OAuthDeviceCodeResponse
{
    [JsonPropertyName("device_code")]
    public string DeviceCode { get; set; } = "";

    [JsonPropertyName("user_code")]
    public string UserCode { get; set; } = "";

    [JsonPropertyName("verification_uri")]
    public string VerificationUri { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("interval")]
    public int Interval { get; set; }
}

public sealed class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "";

    [JsonPropertyName("error")]
    public string Error { get; set; } = "";

    [JsonPropertyName("error_description")]
    public string ErrorDescription { get; set; } = "";
}

public sealed class GitHubIdentity
{
    public string Login { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
}

public sealed class GitHubReleaseResult
{
    public long Id { get; set; }
    public string UploadUrl { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
}

public sealed class UploadAssetResult
{
    public string BrowserDownloadUrl { get; set; } = "";
    public string Name { get; set; } = "";
    public string Digest { get; set; } = "";
}

public sealed class GitHubAssetSummary
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string BrowserDownloadUrl { get; set; } = "";
    public string Digest { get; set; } = "";
    public long Size { get; set; }
}

public sealed class GitHubReleaseSummary
{
    public long Id { get; set; }
    public string TagName { get; set; } = "";
    public string Name { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public System.Collections.Generic.List<GitHubAssetSummary> Assets { get; set; } = new();
}

public sealed class GitHubContentItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Sha { get; set; } = "";
    public string Type { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
}

public sealed class GitHubFileContent
{
    public string Path { get; set; } = "";
    public string Sha { get; set; } = "";
    public string Text { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
}

public sealed class BrowserModItem
{
    public string Source { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Repo { get; set; } = "";
    public string ManifestPath { get; set; } = "";
    public string ManifestSha { get; set; } = "";
    public string ManifestJson { get; set; } = "";
    public string ModId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Version { get; set; } = "";
    public string ArtifactType { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public string Category { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string Hash { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public long ReleaseId { get; set; }
}
