using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SlackBskyUnfurl.Data;
using SlackBskyUnfurl.Models.Slack;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet.Events;

namespace SlackBskyUnfurl.Controllers;

[Route("api/slack")]
[ApiController]
public class SlackController : Controller {
    private readonly ILogger<SlackController> _logger;
    private readonly ISlackService _slackService;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public SlackController(ISlackService slackService, IConfiguration configuration, IMemoryCache cache, ILogger<SlackController> logger) {
        this._logger = logger;
        this._slackService = slackService;
        this._configuration = configuration;
        this._cache = cache;
    }

    [HttpGet("test")]
    public IActionResult Test() {
        return this.Ok("test");
    }

    [HttpGet("authorize")]
    public async Task<IActionResult>Authorize() {
        var clientId = this._configuration["SlackClientId"];
        if (clientId.IsNullOrEmpty()) {
            return this.BadRequest("SlackClientId is not configured");
        }

        var stateString = Guid.NewGuid().ToString("N").Substring(0,8);
        this._cache.Set(stateString, stateString, TimeSpan.FromMinutes(5));

        return this.Redirect(
            $"https://slack.com/oauth/v2/authorize?scope=links%3Aread%2Clinks%3Awrite&client_id={Uri.EscapeDataString(clientId)}&state={stateString}");
    }

    [HttpGet("access")]
    public async Task<IActionResult> Access([FromQuery] string code, [FromQuery] string state) {
        var httpClient = new HttpClient();
        if (code.IsNullOrEmpty() || state.IsNullOrEmpty()) {
            this._logger.LogError($"Invalid request: code={code}, state={state}");
            return this.BadRequest("Invalid request");
        }

        var isStateValid = this._cache.Get(state) != null;
        if (!isStateValid) {
            this._logger.LogError($"Invalid state: {state}");
            return this.BadRequest("Invalid state");
        }
        this._cache.Remove(state);

        var response = await httpClient.PostAsJsonAsync("https://slack.com/api/oauth.v2.access", new {
            client_id = this._configuration["SlackClientId"],
            client_secret = this._configuration["SlackClientSecret"],
            code = code
        });

        if (!response.IsSuccessStatusCode) {
            this._logger.LogError($"Error fetching access token: {await response.Content.ReadAsStringAsync()}");
            return this.BadRequest("Error fetching access token");
        }

        var content = await response.Content.ReadAsStringAsync();
        var accessResponse = JsonConvert.DeserializeObject<ScopeResponse>(content, new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        });

        if (accessResponse == null) {
            this._logger.LogError($"Invalid AccessResponse: {content}");
            return this.BadRequest("Invalid access response");
        }

        try {
            await this._slackService.SaveAccessToken(accessResponse);
        }
        catch (Exception e) {
            this._logger.LogError(e, $"Error saving access token", accessResponse);
            this.BadRequest("Error saving access token");
        }

        return this.Ok("Success!");
    }

    [HttpPost("events/handle")]
    public async Task<IActionResult> Event([FromBody] JsonElement body) {
        var request = JsonConvert.DeserializeObject<EventRequest>(body.ToString(), new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        }); 
        if (request.Type == "url_verification") {
            var urlVerificationEvent = JsonConvert.DeserializeObject<UrlVerification>(body.ToString());
            var result = await this._slackService.HandleVerification(urlVerificationEvent);
            return this.Ok(result);
        }

        this._logger.LogInformation($"RequestBody: {body.ToString()}");

        Task.Run(() => this._slackService.HandleIncomingEvent(body));

        return this.Ok();
    }
}