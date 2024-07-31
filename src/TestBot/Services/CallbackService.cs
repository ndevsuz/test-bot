using System.Text;
using AnswerBot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TestBot.Models;

namespace TestBot.Services;

public class CallbackService(IAnswerRepository answerRepository)
{
    private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        if (callbackQuery.Data == "GetDetails")
        {
            await SendDetailedResultsAsync(botClient, callbackQuery);
        }
    }
    private async Task SendDetailedResultsAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        // Parse the original message to extract necessary information
        var originalMessage = callbackQuery.Message.Text;
        var testId = ExtractTestId(originalMessage);
        var userName = ExtractUserName(originalMessage);

        // Fetch detailed answers from the database
        var answers = await answerRepository.SelectAsync(a => a.Id == testId && a.UserName == userName);

        // Build the detailed results message
        var detailedMessage = BuildDetailedResultsMessage(originalMessage, answers);

        // Edit the original message with the detailed results
        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: detailedMessage,
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: null // Remove the inline keyboard
        );

        // Answer the callback query to remove the loading indicator
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
    }
    
    private string BuildDetailedResultsMessage(string originalMessage, Answer answers)
    {
        var sb = new StringBuilder(originalMessage);
        sb.AppendLine("\n\n*Batafsil ma'lumotlar:*");

        //sb.AppendLine($"{}\\. {EscapeMarkdown(answer.UserAnswer)} " + 
          //            $"{(answer.IsCorrect ? "✅" : "❌")}");
        return sb.ToString();
    }
    
    private int ExtractTestId(string message)
    {
        return 1;
        // Implementation to extract test name from the message
    }

    private string ExtractUserName(string message)
    {
        return "";
        // Implementation to extract user name from the message
    }

    private static string EscapeMarkdown(string text)
    {
        return text?.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("]", "\\]")
            .Replace("(", "\\(").Replace(")", "\\)").Replace("~", "\\~").Replace("`", "\\`")
            .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+").Replace("-", "\\-")
            .Replace("=", "\\=").Replace("|", "\\|").Replace("{", "\\{").Replace("}", "\\}")
            .Replace(".", "\\.").Replace("!", "\\!") ?? "";
    }
}