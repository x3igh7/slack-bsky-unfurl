using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet.Events;

namespace SlackBskyUnfurl.Controllers;

[Route("api/slack")]
[ApiController]
public class SlackController : Controller {
    private readonly ILogger<SlackController> _logger;
    private readonly ISlackService _slackService;

    public SlackController(ISlackService slackService, ILogger<SlackController> logger) {
        this._logger = logger;
        this._slackService = slackService;
    }

    [HttpGet("test")]
    public IActionResult Test() {
        return this.Ok("test");
    }


    [HttpPost("events/handle")]
    public async Task<IActionResult> Event([FromBody] JsonElement body) {
        var request = JsonConvert.DeserializeObject<EventRequest>(body.ToString()); 
        if (request.Type == "url_verification") {
            var urlVerificationEvent = JsonConvert.DeserializeObject<UrlVerification>(body.ToString());
            var result = await this._slackService.HandleVerification(urlVerificationEvent);
            return this.Ok(result);
        }

        this._logger.LogDebug(body.ToString());

        Task.Run(() => this._slackService.HandleIncomingEvent(body));

        return this.Ok();
    }
}