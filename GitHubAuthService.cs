using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using AmonieNomnomPublisher.Models;

namespace AmonieNomnomPublisher;

public sealed class GitHubAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    private readonly HttpClient http;

    public GitHubAuthService()
    {
        http = new HttpClient();
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        http.DefaultRequestHeaders.UserAgent.ParseAdd("AmonieNomnomPublisher/1.0");
    }

    public async Task<OAuthDeviceCodeResponse> RequestDeviceCodeAsync(string clientId, string scopes, CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = scopes
        });
        using var response = await http.PostAsync("https://github.com/login/device/code", content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"GitHub rejected the device-code request: {response.StatusCode}\r\n{json}");
        return JsonSerializer.Deserialize<OAuthDeviceCodeResponse>(json, JsonOptions) ?? throw new InvalidOperationException("GitHub returned an empty device-code response.");
    }

    public async Task<OAuthTokenResponse> PollForTokenAsync(string clientId, OAuthDeviceCodeResponse device, Action<string> status, CancellationToken cancellationToken)
    {
        var interval = Math.Max(5, device.Interval);
        var stopAt = DateTimeOffset.UtcNow.AddSeconds(device.ExpiresIn);
        while (DateTimeOffset.UtcNow < stopAt)
        {
            await Task.Delay(TimeSpan.FromSeconds(interval), cancellationToken);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["device_code"] = device.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            });
            using var response = await http.PostAsync("https://github.com/login/oauth/access_token", content, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var token = JsonSerializer.Deserialize<OAuthTokenResponse>(json, JsonOptions) ?? new OAuthTokenResponse();
            if (!string.IsNullOrWhiteSpace(token.AccessToken)) return token;
            if (token.Error == "authorization_pending")
            {
                status("Waiting for GitHub authorization...");
                continue;
            }
            if (token.Error == "slow_down")
            {
                interval += 5;
                status("GitHub asked us to slow down. Waiting...");
                continue;
            }
            if (token.Error == "expired_token") throw new TimeoutException("The GitHub login code expired. Start sign-in again.");
            if (token.Error == "access_denied") throw new InvalidOperationException("GitHub authorization was denied.");
            if (!response.IsSuccessStatusCode || !string.IsNullOrWhiteSpace(token.Error)) throw new InvalidOperationException($"GitHub authorization failed: {token.Error} {token.ErrorDescription}".Trim());
        }
        throw new TimeoutException("The GitHub login code expired. Start sign-in again.");
    }

    public static void OpenBrowser(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
