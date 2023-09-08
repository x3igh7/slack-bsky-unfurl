using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SlackBskyUnfurl.Models.Bsky;
using SlackBskyUnfurl.Models.Bsky.Requests;
using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Services.Interfaces;

namespace SlackBskyUnfurl.Services; 

public class BlueSkyService : IBlueSkyService {
    private const string BaseUri = "https://bsky.social/xrpc/";
    private readonly string _bskyAppPassword;
    private readonly string _bskyUsername;
    private string _accessToken;
    private string _refreshToken;
    private readonly HttpClient HttpClient;

    public BlueSkyService(IConfiguration configuration) {
        this._bskyUsername = configuration["BlueSkyUserId"];
        this._bskyAppPassword = configuration["BlueSkyAppPassword"];

        this.HttpClient = new HttpClient();
        this.HttpClient.BaseAddress = new Uri(BaseUri);

        this.Authenticate().Wait();
    }

    protected async Task Authenticate() {
        var sessionRequest = new CreateSessionRequest
            { Identifier = this._bskyUsername, Password = this._bskyAppPassword };
        var result = await this.HttpClient.PostAsJsonAsync("com.atproto.server.createSession", sessionRequest);
        this.SetSessionHeaders(result);
    }

    protected async Task Refresh() {
        var refreshHttpClient = new HttpClient();
        refreshHttpClient.BaseAddress = new Uri(BaseUri);
        refreshHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", this._refreshToken);
        try {
            var result = await refreshHttpClient.GetAsync("com.atproto.server.refreshSession");
            this.SetSessionHeaders(result);
            refreshHttpClient.Dispose();
        }
        catch {
            await this.Authenticate();
        }
    }

    public async Task<GetPostThreadResponse> HandleGetPostThreadRequest(string url) {
        var username = Regex.Match(url, @"(?<=profile/).*?(?=/post)").Value;
        var resolvedHandle = await this.ResolveHandle(username);

        var postId = Regex.Match(url, @"(?<=post/).*").Value;
        var threadPost = await this.GetPostThread(resolvedHandle.Did, postId);

        return threadPost;
    }

    public async Task<GetPostThreadResponse> GetPostThread(string did, string postId) {
        var postUri = $"at://{did}/app.bsky.feed.post/{postId}";
        var result =
            await this.HttpClient.GetAsync($"app.bsky.feed.getPostThread?uri={Uri.EscapeDataString(postUri)}");
        if (result.StatusCode == HttpStatusCode.Unauthorized) {
            await this.Refresh();
            return await this.GetPostThread(did, postId);
        }

        if (!result.IsSuccessStatusCode) {
            throw new InvalidOperationException("Failed to get post thread");
        }

        var getPostThreadResponse =
            JsonConvert.DeserializeObject<GetPostThreadResponse>(await result.Content.ReadAsStringAsync());

        if (getPostThreadResponse == null) {
            throw new InvalidOperationException("Failed to get post thread");
        }

        return getPostThreadResponse;
    }

    public async Task<ResolveHandleResponse> ResolveHandle(string username) {
        var result =
            await this.HttpClient.GetAsync(
                $"com.atproto.identity.resolveHandle?handle={Uri.EscapeDataString(username)}");
        if (result.StatusCode == HttpStatusCode.Unauthorized) {
            await this.Refresh();
            return await this.ResolveHandle(username);
        }

        var resolveHandleResponse =
            JsonConvert.DeserializeObject<ResolveHandleResponse>(await result.Content.ReadAsStringAsync());
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

        this.HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", this._accessToken);
    }
}