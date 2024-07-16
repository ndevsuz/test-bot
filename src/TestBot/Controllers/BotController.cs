using Microsoft.AspNetCore.Mvc;
using TestBot.EasyBotFramework;
using TestBot.Repositories;
using TestBot.Services;

namespace TestBot.Controllers;

public class BotController(HandleService handleService) : ControllerBase
{
    [HttpPost("start-bot")]
    public async Task<IActionResult> StartBot()
    {
        await handleService.HandleRequest();
        return Ok("Bot started");
    }

}