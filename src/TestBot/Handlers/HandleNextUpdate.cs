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
    public async Task<string?> NewTextMessage(UpdateInfo update, CancellationToken ct = default)
    {
        while (await NewMessage(update, ct) != MsgCategory.Text) { }
        return update.Message.Text;
    }

    private async Task<MsgCategory> NewMessage(UpdateInfo update, CancellationToken ct = default)
    {
        while (true)
        {
            switch (await NextEvent(update, ct))
            {
                case UpdateKind.NewMessage
                    when update.MsgCategory is MsgCategory.Text or MsgCategory.MediaOrDoc or MsgCategory.StickerOrDice:
                    return update.MsgCategory; // NewMessage only returns for messages from these 3 categories
                case UpdateKind.CallbackQuery:
                    
                case UpdateKind.OtherUpdate
                    when update.Update.MyChatMember is ChatMemberUpdated
                        { NewChatMember.Status: ChatMemberStatus.Left or ChatMemberStatus.Kicked }:
                    throw new LeftTheChatException(); // abort the calling method
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