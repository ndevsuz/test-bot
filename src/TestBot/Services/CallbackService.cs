using System.Text;
using AnswerBot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TestBot.Models;
using TestBot.Repositories;

namespace TestBot.Services;

public class CallbackService(IAnswerRepository answerRepository, ITestRepository testRepository)
{
    public async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        await SendDetailedResultsAsync(botClient, callbackQuery);
    }
    private async Task SendDetailedResultsAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        // Parse the original message to extract necessary information
        var originalMessage = callbackQuery.Message.Text;
        var testId = ExtractTestId(originalMessage);
        var answerId = ExtractAnswerId(originalMessage);

        var answers = await answerRepository.SelectAsync(a => a.Id == answerId);
        var test = await testRepository.SelectAsync(t => t.Id == testId);

        // Build the detailed results message
        var detailedMessage = BuildDetailedResultsMessage(originalMessage, answers, test.Answers);

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
    
    private string BuildDetailedResultsMessage(string originalMessage, Answer answer, string correctAnswers)
    {
        var sb = new StringBuilder(EscapeMarkdown(originalMessage));
        sb.AppendLine("\n\n*Batafsil ma'lumotlar:*");

        var userAnswers = answer.Answers.ToCharArray();
        var correctAnswersList = correctAnswers.ToCharArray();

        for (int i = 0; i < userAnswers.Length; i++)
        {
            bool isCorrect = i < correctAnswersList.Length && 
                             char.ToLower(userAnswers[i]) == char.ToLower(correctAnswersList[i]);
        
            sb.AppendLine($"{i + 1}\\. {EscapeMarkdown(userAnswers[i].ToString())} " + 
                          $"{(isCorrect ? "✅" : "❌")} " +
                          $"{(isCorrect ? "" : $"\\(To'g'ri javob: {EscapeMarkdown(correctAnswersList[i].ToString())}\\)")}");
        }

        return sb.ToString();
    }

    private string EscapeMarkdown(string text)
    {
        return text.Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }    
    private int ExtractTestId(string message)
    {
        var idLine = message.Split('\n')
            .FirstOrDefault(line => line.StartsWith("\ud83c\udd94 Test ID "));

        if (idLine != null)
        {
            var idString = idLine.Substring("\ud83c\udd94 Test ID ".Length).Trim();
        
            // Try to parse the ID as an integer
            if (int.TryParse(idString, out int testId))
            {
                return testId;
            }
        }

        // Return -1 or throw an exception if ID is not found or not valid
        return -1; // Or throw new Exception("Test ID not found in the message");
    }

    private int ExtractAnswerId(string message)
    {
        var idLine = message.Split('\n')
            .FirstOrDefault(line => line.StartsWith("🆔 Javobning IDsi"));

        if (idLine != null)
        {
            var idString = idLine.Substring("🆔 Javobning IDsi ".Length).Trim();
        
            // Try to parse the ID as an integer
            if (int.TryParse(idString, out int testId))
            {
                return testId;
            }
        }

        // Return -1 or throw an exception if ID is not found or not valid
        return -1; // Or throw new Exception("Test ID not found in the message");

    }
}