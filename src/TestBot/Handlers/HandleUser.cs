using Telegram.Bot.Types;
using TestBot.EasyBotFramework;

namespace TestBot.Handlers;

public class HandleUser
{
    public Task Handle(Chat chat, User user, UpdateInfo update)
    {
        return Task.CompletedTask;
    }
} 