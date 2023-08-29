using Microsoft.AspNetCore.Mvc;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet.Events;

namespace SlackBskyUnfurl.Controllers;

[Route("api/slack")]
[ApiController]
public class SlackController : Controller {
    private readonly ILogger<SlackController> _logger;
    private readonly ISlackService _slack;

    public SlackController(ISlackService slack, ILogger<SlackController> logger) {
        this._logger = logger;
        this._slack = slack;
    }

    [HttpGet("test")]
    public IActionResult Test() {
        return this.Ok("test");
    }


    [HttpPost("events/message")]
    public IActionResult Event([FromBody] UrlVerification message) {
        return this.Ok(message.Challenge);
        //this._slack.HandleMessageAsync(message);
        //return this.Ok("test");
    }
}