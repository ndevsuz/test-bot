using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TestBot.Helpers;

public static class CheckMember
{
    public static async Task<bool> CheckMemberAsync(ITelegramBotClient telegramBotClient, Chat chat, IConfiguration configuration)
    {
        var channelId = long.Parse(configuration["ChannelId"]!);
        var userId = chat.Id;

        var chatMember = await telegramBotClient.GetChatMemberAsync(channelId, userId);
        
        return chatMember.Status != ChatMemberStatus.Left && chatMember.Status != ChatMemberStatus.Kicked;
    }
}