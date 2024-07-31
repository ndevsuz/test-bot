using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Interfaces;

namespace TestBot.Helpers;

public class Handler(Lazy<HandleAdmin> handleAdmin, Lazy<HandleUser> handleUser) : IHandler
{
    public async Task HandleAdminTask(Chat chat, User user, UpdateInfo updateInfo, Update update)
    {
        await handleAdmin.Value.Handle(chat, user, updateInfo, update);
    }

    public async Task HandleUserTask(Chat chat, User user, UpdateInfo updateInfo, Update update)
    {
        await handleUser.Value.Handle(chat, user, updateInfo, update);
    }
}