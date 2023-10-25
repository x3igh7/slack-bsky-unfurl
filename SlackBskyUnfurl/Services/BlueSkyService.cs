using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SlackBskyUnfurl.Models.Bsky.Requests;
using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Services.Interfaces;

namespace SlackBskyUnfurl.Services;

public class BlueSkyService : IBlueSkyService {
    private const string BaseUri = "https://bsky.social/xrpc/";
    private readonly string _bskyAppPassword;
    private readonly string _bskyUsername;
    private readonly ILogger<BlueSkyService> _logger;
    private readonly HttpClient HttpClient;
    private string _accessToken;
    private string _bskyHandle;
    private string _refreshToken;

    public BlueSkyService(IConfiguration configuration, ILogger<BlueSkyService> logger) {
        this._logger = logger;
        this._bskyUsername = configuration["BlueSkyUserId"];
        this._bskyAppPassword = configuration["BlueSkyAppPassword"];

        this.HttpClient = new HttpClient();
        this.HttpClient.BaseAddress = new Uri(BaseUri);

        this.Authenticate().Wait();
    }

    public async Task<GetPostThreadResponse> HandleGetPostThreadRequest(string url) {
        var username = Regex.Match(url, @"(?<=profile/).*?(?=/post)").Value;
        var resolvedHandle = await this.ResolveHandle(username);

        var postId = Regex.Match(url, @"(?<=post/).*").Value;
        var threadPost = await this.GetPostThread(resolvedHandle.Did, postId);

        return threadPost;
    }

    protected async Task Authenticate() {
        this._logger.LogDebug("Begin authentication");

        var sessionRequest = new CreateSessionRequest
            { Identifier = this._bskyUsername, Password = this._bskyAppPassword };
        var result = await this.HttpClient.PostAsJsonAsync("com.atproto.server.createSession", sessionRequest);

        if (!result.IsSuccessStatusCode) {
            throw new InvalidOperationException(
                $"Failed to authenticate with BlueSky: {await result.Content.ReadAsStringAsync()}");
        }

        this._logger.LogDebug("Authentication complete.");

        this.SetSessionHeaders(result);
    }

    protected async Task Refresh() {
        this._logger.LogDebug("Begin authentication refresh");

        var refreshHttpClient = new HttpClient();
        refreshHttpClient.BaseAddress = new Uri(BaseUri);
        refreshHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", this._refreshToken);

        var refreshRequest = new RefreshSessionRequest {
            AccessJwt = this._accessToken,
            RefreshJwt = this._refreshToken,
            Handle = this._bskyHandle,
            Did = this._bskyUsername
        };
        try {
            var result = await refreshHttpClient.PostAsJsonAsync("com.atproto.server.refreshSession", refreshRequest);

            if (!result.IsSuccessStatusCode) {
                this._logger.LogDebug(
                    $"Refresh failed. Status: {result.StatusCode} Content: {await result.Content.ReadAsStringAsync()}. Begin re-authentication");

                await this.Authenticate();
                refreshHttpClient.Dispose();
                return;
            }

            this.SetSessionHeaders(result);
            refreshHttpClient.Dispose();

            this._logger.LogDebug("Authentication refresh complete");
        }
        catch {
            this._logger.LogDebug("Refresh erred. Begin re-authentication");

            await this.Authenticate();
            refreshHttpClient.Dispose();
        }
    }

    public async Task<GetPostThreadResponse> GetPostThread(string did, string postId) {
        this._logger.LogDebug($"GetPostThread Did: {did} PostId: {postId}");

        var postUri = $"at://{did}/app.bsky.feed.post/{postId}";
        var result =
            await this.HttpClient.GetAsync($"app.bsky.feed.getPostThread?uri={Uri.EscapeDataString(postUri)}");

        var content = await result.Content.ReadAsStringAsync();
        this._logger.LogDebug($"GetPostThread StatusCode: {result.StatusCode} Result: {content}");

        if (result.StatusCode == HttpStatusCode.Unauthorized ||
            (result.StatusCode == HttpStatusCode.BadRequest && content.Contains("ExpiredToken"))) {
            await this.Refresh();
            return await this.GetPostThread(did, postId);
        }

        if (!result.IsSuccessStatusCode) {
            throw new InvalidOperationException("Failed to get post thread");
        }

        var getPostThreadResponse =
            JsonConvert.DeserializeObject<GetPostThreadResponse>(content);

        if (getPostThreadResponse == null) {
            throw new InvalidOperationException("Failed to get post thread");
        }

        return getPostThreadResponse;
    }

    public async Task<ResolveHandleResponse> ResolveHandle(string username) {
        this._logger.LogDebug($"ResolveHandle Username: {username}");

        var result =
            await this.HttpClient.GetAsync(
                $"com.atproto.identity.resolveHandle?handle={Uri.EscapeDataString(username)}");
        if (result.StatusCode == HttpStatusCode.Unauthorized) {
            await this.Refresh();
            return await this.ResolveHandle(username);
        }

        var content = await result.Content.ReadAsStringAsync();
        this._logger.LogDebug($"ResolveHandle Result: {content}");

        var resolveHandleResponse =
            JsonConvert.DeserializeObject<ResolveHandleResponse>(content);
        if (resolveHandleResponse == null) {
            throw new InvalidOperationException("Failed to resolve handle");
        }

        return resolveHandleResponse;
    }

    private async void SetSessionHeaders(HttpResponseMessage httpResponse) {
        var response = JsonConvert.DeserializeObject<SessionResponse>(await httpResponse.Content.ReadAsStringAsync());
        if (response == null) {
            throw new InvalidOperationException("Failed to fetch session");
        }

        this._accessToken = response.AccessJwt;
        this._refreshToken = response.RefreshJwt;
        this._bskyHandle = response.Handle;

        this.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", this._accessToken);
    }
}