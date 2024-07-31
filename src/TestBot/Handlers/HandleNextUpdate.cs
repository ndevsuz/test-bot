using AnswerBot.Repositories;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TestBot.EasyBotFramework;
using TestBot.Services;

namespace TestBot.Handlers;

public class HandleNextUpdate(IOptions<BotConfiguration> botConfiguration, CallbackService callbackService, CancellationTokenSource cancel)
{
    private readonly ITelegramBotClient _telegram = new TelegramBotClient(botConfiguration.Value.BotToken);
    public async Task<string?> NewTextMessage(UpdateInfo updateInfo, Update update, CancellationToken ct = default)
    {
        while (await NewMessage(updateInfo, update, ct) != MsgCategory.Text) { }
        return updateInfo.Message.Text;
    }

    private async Task<MsgCategory> NewMessage(UpdateInfo updateInfo, Update update, CancellationToken ct = default)
    {
        while (true)
        {
            switch (await NextEvent(updateInfo, ct))
            {
                case UpdateKind.NewMessage
                    when updateInfo.MsgCategory is MsgCategory.Text or MsgCategory.MediaOrDoc or MsgCategory.StickerOrDice:
                    return updateInfo.MsgCategory; // NewMessage only returns for messages from these 3 categories
                case UpdateKind.CallbackQuery:
                    await callbackService.HandleCallbackQueryAsync(_telegram, update.CallbackQuery);
                    return updateInfo.MsgCategory;
            }
        }
    }

    private async Task<UpdateKind> NextEvent(UpdateInfo update, CancellationToken ct = default)
    {
        using var bothCt = CancellationTokenSource.CreateLinkedTokenSource(ct, cancel.Token);
        var newUpdate = await ((IGetNext)update).NextUpdate(bothCt.Token);
        update.Message = newUpdate.Message;
        update.CallbackData = newUpdate.CallbackData;
        update.Update = newUpdate.Update;
        return update.UpdateKind = newUpdate.UpdateKind;
    }
    
    public async Task<string> ButtonClicked(UpdateInfo update, Message msg = null, CancellationToken ct = default)
    {
        /*
        while (true)
        {
            switch (await NextEvent(update, ct))
            {
                case UpdateKind.CallbackQuery:
                    if (msg != null && update.Message.MessageId != msg.MessageId)
                        _ = _telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, null, cancellationToken: ct);
                    else
                        return update.CallbackData;
                    continue;
            }
        }
        */
        var nextEvent = await NextEvent(update, ct);
        return update.CallbackData;
    }
}