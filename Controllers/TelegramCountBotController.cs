using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

[ApiController]
[Route("api/bot")]
public class TelegramCountBotController : ControllerBase
{
    private async Task<IActionResult> handle_request([FromBody] Update update, TelegramCountBot bot)
    {
        if (update.Message is not { } message)
            return BadRequest();
        // Only process text messages
        if (message.Text is not { } messageText)
            return BadRequest();

        long sender_id = update.Message.From!.Id;

        string response = await bot.handle_message(messageText, sender_id!);

        if (response != "")
        {
            await bot.send_message_async(update.Message.From!.Id, response);
        }

        return Ok();
    }

    [HttpPost("debt")]
    public async Task<IActionResult> PostDebt([FromBody] Update update)
    {
        return await handle_request(update, new TelegramDebtBot());
    }

    [HttpPost("scrabble")]
    public async Task<IActionResult> PostScrabble([FromBody] Update update)
    {
        return await handle_request(update, new TelegramScrabbleBot());
    }

}