using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
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

    public SlackController(ISlackService slackService, IConfiguration configuration, ILogger<SlackController> logger) {
        this._logger = logger;
        this._slackService = slackService;
        this._configuration = configuration;
    }

    [HttpGet("test")]
    public IActionResult Test() {
        return this.Ok("test");
    }

    public async Task<IActionResult>Authorize() {
        var clientId = this._configuration["SlackClientId"];
        if (clientId.IsNullOrEmpty()) {
            return this.BadRequest("SlackClientId is not configured");
        }

        return this.Redirect(
            $"https://slack.com/oauth/v2/authorize?scope=links%3Aread%2Clinks%3Awrite&client_id={Uri.EscapeDataString(clientId)}");
    }

    public async Task<IActionResult> Access([FromBody] JsonElement body) {
        var response = JsonConvert.DeserializeObject<ScopeResponse>(body.ToString());
        if (response == null || !response.Ok) {
            return this.BadRequest("Error authorizing");
        }

        await this._slackService.SaveAccessToken(response);

        return this.Ok("Success!");
    }

    [HttpPost("events/handle")]
    public async Task<IActionResult> Event([FromBody] JsonElement body) {
        var request = JsonConvert.DeserializeObject<EventRequest>(body.ToString()); 
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