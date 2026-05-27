using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AnomieNomnomPublisher.Models;

namespace AnomieNomnomPublisher;

public sealed class GitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly HttpClient http;
    private readonly string token;

    public GitHubClient(string accessToken)
    {
        token = accessToken;
        http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AnomieNomnomPublisher/1.0");
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<GitHubIdentity> GetUserAsync(CancellationToken cancellationToken)
    {
        var node = await SendJsonAsync(HttpMethod.Get, "https://api.github.com/user", null, cancellationToken);
        return new GitHubIdentity
        {
            Login = node?["login"]?.GetValue<string>() ?? "",
            DisplayName = node?["name"]?.GetValue<string>() ?? "",
            AvatarUrl = node?["avatar_url"]?.GetValue<string>() ?? ""
        };
    }

    public async Task<bool> RepoExistsAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return false;
        if (!response.IsSuccessStatusCode) throw await BuildErrorAsync(response, cancellationToken);
        return true;
    }

    public async Task CreateRepoAsync(string name, bool isPrivate, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["name"] = name,
            ["private"] = isPrivate,
            ["auto_init"] = true
        };
        await SendJsonAsync(HttpMethod.Post, "https://api.github.com/user/repos", payload, cancellationToken);
    }

    public async Task<GitHubReleaseResult> GetOrCreateReleaseAsync(string owner, string repo, string tag, string title, string body, CancellationToken cancellationToken)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{Uri.EscapeDataString(tag)}"))
        using (var response = await http.SendAsync(request, cancellationToken))
        {
            if (response.IsSuccessStatusCode)
            {
                var existingJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseRelease(JsonNode.Parse(existingJson));
            }
            if (response.StatusCode != HttpStatusCode.NotFound) throw await BuildErrorAsync(response, cancellationToken);
        }

        var payload = new JsonObject
        {
            ["tag_name"] = tag,
            ["name"] = title,
            ["body"] = body,
            ["draft"] = false,
            ["prerelease"] = false
        };
        var node = await SendJsonAsync(HttpMethod.Post, $"https://api.github.com/repos/{owner}/{repo}/releases", payload, cancellationToken);
        return ParseRelease(node);
    }

    public async Task<UploadAssetResult> UploadReleaseAssetAsync(string owner, string repo, long releaseId, string filePath, Action<string> log, CancellationToken cancellationToken)
    {
        var assetName = Path.GetFileName(filePath);
        await TryDeleteReleaseAssetByNameAsync(owner, repo, releaseId, assetName, log, cancellationToken);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var result = await TryUploadReleaseAssetOnceAsync(owner, repo, releaseId, filePath, assetName, cancellationToken);
            if (result.Success && result.Node is not null)
            {
                return new UploadAssetResult
                {
                    BrowserDownloadUrl = result.Node["browser_download_url"]?.GetValue<string>() ?? "",
                    Name = result.Node["name"]?.GetValue<string>() ?? assetName,
                    Digest = result.Node["digest"]?.GetValue<string>() ?? ""
                };
            }

            if (result.StatusCode == HttpStatusCode.UnprocessableEntity && result.Body.Contains("already_exists", StringComparison.OrdinalIgnoreCase))
            {
                log($"GitHub still reports an existing asset named {assetName}. Cleaning it up and retrying ({attempt}/3)...");
                await TryDeleteReleaseAssetByNameAsync(owner, repo, releaseId, assetName, log, cancellationToken);
                await Task.Delay(1500 * attempt, cancellationToken);
                continue;
            }

            throw new InvalidOperationException($"GitHub asset upload failed: {result.StatusCode}\r\n{result.Body}");
        }

        throw new InvalidOperationException($"GitHub refused the release asset because an asset named '{assetName}' still exists. Delete the release asset in GitHub or use a new release tag/file name.");
    }

    private async Task TryDeleteReleaseAssetByNameAsync(string owner, string repo, long releaseId, string assetName, Action<string> log, CancellationToken cancellationToken)
    {
        var assets = await SendJsonArrayAsync(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets?per_page=100", null, cancellationToken);
        foreach (var asset in assets)
        {
            var existingName = asset?["name"]?.GetValue<string>() ?? "";
            if (!AssetNamesEquivalent(existingName, assetName)) continue;
            var id = asset?["id"]?.GetValue<long>() ?? 0;
            if (id <= 0) continue;
            log($"Replacing existing release asset: {existingName}");
            await SendRawAsync(HttpMethod.Delete, $"https://api.github.com/repos/{owner}/{repo}/releases/assets/{id}", null, cancellationToken);
            await Task.Delay(1500, cancellationToken);
        }
    }

    private static bool AssetNamesEquivalent(string left, string right)
    {
        return string.Equals(NormalizeAssetName(left), NormalizeAssetName(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAssetName(string value)
    {
        var builder = new StringBuilder();
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
            else if (c == '_' || c == '-') builder.Append(c);
            else builder.Append('.');
        }
        var result = builder.ToString();
        while (result.Contains("..", StringComparison.Ordinal)) result = result.Replace("..", ".", StringComparison.Ordinal);
        return result.Trim('.');
    }

    private async Task<(bool Success, HttpStatusCode StatusCode, string Body, JsonNode? Node)> TryUploadReleaseAssetOnceAsync(string owner, string repo, long releaseId, string filePath, string assetName, CancellationToken cancellationToken)
    {
        var url = $"https://uploads.github.com/repos/{owner}/{repo}/releases/{releaseId}/assets?name={Uri.EscapeDataString(assetName)}";
        await using var stream = File.OpenRead(filePath);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        using var response = await http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) return (false, response.StatusCode, json, null);
        return (true, response.StatusCode, json, JsonNode.Parse(json));
    }

    public async Task CreateForkAsync(string upstreamOwner, string upstreamRepo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{upstreamOwner}/{upstreamRepo}/forks");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created) return;
        if (response.StatusCode == HttpStatusCode.Conflict) return;
        if (!response.IsSuccessStatusCode) throw await BuildErrorAsync(response, cancellationToken);
    }

    public async Task WaitForForkAsync(string owner, string repo, Action<string> log, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 12; i++)
        {
            if (await RepoExistsSafeAsync(owner, repo, cancellationToken)) return;
            log("Waiting for fork to become available...");
            await Task.Delay(2500, cancellationToken);
        }
        throw new TimeoutException("The GitHub fork did not become available in time. Try Submit PR again in a few seconds.");
    }

    public async Task<string> GetBranchShaAsync(string owner, string repo, string branch, CancellationToken cancellationToken)
    {
        var node = await SendJsonAsync(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/branches/{Uri.EscapeDataString(branch)}", null, cancellationToken);
        return node?["commit"]?["sha"]?.GetValue<string>() ?? throw new InvalidOperationException("Could not read branch SHA.");
    }

    public async Task EnsureBranchAsync(string owner, string repo, string branch, string fromSha, CancellationToken cancellationToken)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/git/ref/heads/{Uri.EscapeDataString(branch)}"))
        using (var response = await http.SendAsync(request, cancellationToken))
        {
            if (response.IsSuccessStatusCode) return;
            if (response.StatusCode != HttpStatusCode.NotFound) throw await BuildErrorAsync(response, cancellationToken);
        }
        var payload = new JsonObject
        {
            ["ref"] = $"refs/heads/{branch}",
            ["sha"] = fromSha
        };
        await SendJsonAsync(HttpMethod.Post, $"https://api.github.com/repos/{owner}/{repo}/git/refs", payload, cancellationToken);
    }

    public async Task<string?> GetFileShaAsync(string owner, string repo, string path, string branch, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/contents/{Uri.EscapeDataString(path).Replace("%2F", "/")}?ref={Uri.EscapeDataString(branch)}");
        using var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) throw await BuildErrorAsync(response, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var node = JsonNode.Parse(json);
        return node?["sha"]?.GetValue<string>();
    }

    public async Task CommitFileAsync(string owner, string repo, string branch, string path, string content, string message, string? sha, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["message"] = message,
            ["content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
            ["branch"] = branch
        };
        if (!string.IsNullOrWhiteSpace(sha)) payload["sha"] = sha;
        await SendJsonAsync(HttpMethod.Put, $"https://api.github.com/repos/{owner}/{repo}/contents/{Uri.EscapeDataString(path).Replace("%2F", "/")}", payload, cancellationToken);
    }

    public async Task<string> CreatePullRequestAsync(string upstreamOwner, string upstreamRepo, string title, string body, string head, string baseBranch, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["title"] = title,
            ["body"] = body,
            ["head"] = head,
            ["base"] = baseBranch,
            ["maintainer_can_modify"] = true
        };
        var node = await SendJsonAsync(HttpMethod.Post, $"https://api.github.com/repos/{upstreamOwner}/{upstreamRepo}/pulls", payload, cancellationToken);
        return node?["html_url"]?.GetValue<string>() ?? "";
    }



    public async Task<List<GitHubReleaseSummary>> ListReleasesAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        var releases = await SendJsonArrayAsync(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/releases", null, cancellationToken);
        var result = new List<GitHubReleaseSummary>();
        foreach (var release in releases)
        {
            var summary = new GitHubReleaseSummary
            {
                Id = release?["id"]?.GetValue<long>() ?? 0,
                TagName = release?["tag_name"]?.GetValue<string>() ?? "",
                Name = release?["name"]?.GetValue<string>() ?? "",
                HtmlUrl = release?["html_url"]?.GetValue<string>() ?? "",
                CreatedAt = release?["created_at"]?.GetValue<string>() ?? ""
            };
            if (release?["assets"] is JsonArray assets)
            {
                foreach (var asset in assets)
                {
                    summary.Assets.Add(new GitHubAssetSummary
                    {
                        Id = asset?["id"]?.GetValue<long>() ?? 0,
                        Name = asset?["name"]?.GetValue<string>() ?? "",
                        BrowserDownloadUrl = asset?["browser_download_url"]?.GetValue<string>() ?? "",
                        Digest = asset?["digest"]?.GetValue<string>() ?? "",
                        Size = asset?["size"]?.GetValue<long>() ?? 0
                    });
                }
            }
            result.Add(summary);
        }
        return result;
    }

    public async Task DeleteReleaseAsync(string owner, string repo, long releaseId, CancellationToken cancellationToken)
    {
        await SendRawAsync(HttpMethod.Delete, $"https://api.github.com/repos/{owner}/{repo}/releases/{releaseId}", null, cancellationToken);
    }

    public async Task<List<GitHubContentItem>> ListDirectoryContentsAsync(string owner, string repo, string path, string branch, CancellationToken cancellationToken)
    {
        var safePath = Uri.EscapeDataString(path.Trim('/')).Replace("%2F", "/");
        var node = await SendJsonAsync(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/contents/{safePath}?ref={Uri.EscapeDataString(branch)}", null, cancellationToken);
        var result = new List<GitHubContentItem>();
        if (node is not JsonArray items) return result;
        foreach (var item in items)
        {
            result.Add(new GitHubContentItem
            {
                Name = item?["name"]?.GetValue<string>() ?? "",
                Path = item?["path"]?.GetValue<string>() ?? "",
                Sha = item?["sha"]?.GetValue<string>() ?? "",
                Type = item?["type"]?.GetValue<string>() ?? "",
                DownloadUrl = item?["download_url"]?.GetValue<string>() ?? "",
                HtmlUrl = item?["html_url"]?.GetValue<string>() ?? ""
            });
        }
        return result;
    }

    public async Task<GitHubFileContent> ReadTextFileAsync(string owner, string repo, string path, string branch, CancellationToken cancellationToken)
    {
        var safePath = Uri.EscapeDataString(path.Trim('/')).Replace("%2F", "/");
        var node = await SendJsonAsync(HttpMethod.Get, $"https://api.github.com/repos/{owner}/{repo}/contents/{safePath}?ref={Uri.EscapeDataString(branch)}", null, cancellationToken);
        var encoded = node?["content"]?.GetValue<string>() ?? "";
        encoded = encoded.Replace("\n", "").Replace("\r", "");
        var text = string.IsNullOrWhiteSpace(encoded) ? "" : Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        return new GitHubFileContent
        {
            Path = node?["path"]?.GetValue<string>() ?? path,
            Sha = node?["sha"]?.GetValue<string>() ?? "",
            Text = text,
            HtmlUrl = node?["html_url"]?.GetValue<string>() ?? ""
        };
    }

    public async Task DeleteFileAsync(string owner, string repo, string branch, string path, string message, string sha, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["message"] = message,
            ["sha"] = sha,
            ["branch"] = branch
        };
        await SendRawAsync(HttpMethod.Delete, $"https://api.github.com/repos/{owner}/{repo}/contents/{Uri.EscapeDataString(path).Replace("%2F", "/")}", payload, cancellationToken);
    }

    private async Task<bool> RepoExistsSafeAsync(string owner, string repo, CancellationToken cancellationToken)
    {
        try { return await RepoExistsAsync(owner, repo, cancellationToken); }
        catch { return false; }
    }

    private static GitHubReleaseResult ParseRelease(JsonNode? node)
    {
        return new GitHubReleaseResult
        {
            Id = node?["id"]?.GetValue<long>() ?? 0,
            UploadUrl = node?["upload_url"]?.GetValue<string>() ?? "",
            HtmlUrl = node?["html_url"]?.GetValue<string>() ?? ""
        };
    }

    private async Task<JsonNode?> SendJsonAsync(HttpMethod method, string url, JsonObject? payload, CancellationToken cancellationToken)
    {
        var response = await SendRawAsync(method, url, payload, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonNode.Parse(json);
    }

    private async Task<JsonArray> SendJsonArrayAsync(HttpMethod method, string url, JsonObject? payload, CancellationToken cancellationToken)
    {
        var node = await SendJsonAsync(method, url, payload, cancellationToken);
        return node as JsonArray ?? new JsonArray();
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, JsonObject? payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (payload is not null) request.Content = new StringContent(payload.ToJsonString(JsonOptions), Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw await BuildErrorAsync(response, cancellationToken);
        return response;
    }

    private static async Task<Exception> BuildErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new InvalidOperationException($"GitHub API failed: {(int)response.StatusCode} {response.ReasonPhrase}\r\n{body}");
    }
}
