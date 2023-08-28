using Microsoft.AspNetCore.Mvc;

namespace SlackBskyUnfurl.Controllers; 

[Route("api/slack")]
[ApiController]
public class SlackController : Controller {
    private readonly ILogger<SlackController> _logger;

    public SlackController(ILogger<SlackController> logger) {
        this._logger = logger;
    }

    [HttpGet("test")]
    public ActionResult Test() {
        return this.Ok("test");
    }
}