using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;

namespace TestBot.Services;

public class AdminService
{
    private readonly ITelegramBotClient _botClient;

    public AdminService()
    {
        
    }

    public async Task<bool> HandleNewTest(/*nmadur viewmodel*/)
    {
        return true;
    }
}