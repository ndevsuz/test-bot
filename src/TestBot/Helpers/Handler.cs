using Telegram.Bot.Types;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Interfaces;

namespace TestBot.Helpers;

public class Handler(Lazy<HandleAdmin> handleAdmin, Lazy<HandleUser> handleUser) : IHandler
{
    public async Task HandleAdminTask(Chat chat, User user, UpdateInfo update)
    {
        await handleAdmin.Value.Handle(chat, user, update);
    }

    public async Task HandleUserTask(Chat chat, User user, UpdateInfo update)
    {
        await handleUser.Value.Handle(chat, user, update);
    }
}