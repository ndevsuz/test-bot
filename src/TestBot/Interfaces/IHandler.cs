using Telegram.Bot.Types;
using TestBot.EasyBotFramework;

namespace TestBot.Interfaces;

public interface IHandler
{
    Task HandleAdminTask(Chat chat, User user, UpdateInfo updateInfo, Update update);
    Task HandleUserTask(Chat chat, User user, UpdateInfo updateInfo, Update update);
}