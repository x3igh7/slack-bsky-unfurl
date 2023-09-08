using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SlackBskyUnfurl.Services.Interfaces;

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
    public IActionResult Event([FromBody] string slackEvent) {
        var jsonData = JsonConvert.DeserializeObject<dynamic>(slackEvent);
        if (jsonData != null && jsonData.type == "url_verification")
        {
            return this.Ok(jsonData.challenge);
        }
        Task.Run(this._slackService.HandleIncomingEvent(jsonData, slackEvent));

        return this.Ok();
    }
}