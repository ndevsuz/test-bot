using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Helpers;
using TestBot.Interfaces;

namespace TestBot.Services;

public class HandleService(
    Lazy<IHandler> handler,
    UserService userService,
    HandleNextUpdate handleNext,
    IConfiguration configuration,
    IOptions<BotConfiguration> botConfiguration)
    : EasyBot(botConfiguration.Value.BotToken, configuration)
{
    protected override async Task OnPrivateChat(Chat chat, User user, UpdateInfo update)
    {
        var updateMessage = update.Message.Text;
        await IsSubscribed(update, chat, user);
        switch (updateMessage)
        {
            case "/panel" :
                await handler.Value.HandleAdminTask(chat, user, update);
                break;
            case "/start":
                _ = Task.Run(async () => await userService.AddUser(chat));
                await handler.Value.HandleUserTask(chat, user, update);
                break;	

            default:
            {
                await handler.Value.HandleUserTask(chat, user, update);
                break;	
            }
        }

        if (update.CallbackData == "Subscribed")
        {
            var result = await CheckMember.CheckMemberAsync(Telegram ,chat, configuration);
            if (result)
            {
                ReplyCallback(update, "Muvaffaqiyatli\u2705");
                await handler.Value.HandleUserTask(chat, user, update);
            }
        }
    }
    public Task HandleRequest()
    {
        this.Run();
        return Task.CompletedTask;
    }

    private async Task IsSubscribed(UpdateInfo update, Chat chat, User user)
    {
        while (true)
        {
            var isMember = await CheckMember.CheckMemberAsync(Telegram, chat, configuration);
            if (isMember)
                return;

            var message = await Telegram.SendTextMessageAsync(chat.Id,
                "Botdan foydalanish uchun, pasdagi tugmani bosib kanalga obuna bo'ling, va Tekshirish ni bosing.",
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[]
                {
                    new("Obuna bo'lish") { Url = "https://t.me/dotnetsharpist" },
                    new("Tekshirish✅") { CallbackData = "Subscribed" }
                }));

            var callbackResult = await handleNext.ButtonClicked(update, update.Message);
            if (callbackResult == "Subscribed")
            {
                var result = await CheckMember.CheckMemberAsync(Telegram, chat, configuration);
                if (!result)
                {
                    await Telegram.DeleteMessageAsync(chat.Id, message.MessageId);
                    continue;
                }

                ReplyCallback(update, "Muvaffaqiyatli\u2705");
                await Telegram.DeleteMessageAsync(chat.Id, message.MessageId);
                return;
            }
        }
    }

}