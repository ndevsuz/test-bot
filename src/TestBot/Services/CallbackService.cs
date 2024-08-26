using System.Text;
using AnswerBot.Repositories;
using Newtonsoft.Json;
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
        var originalMessage = callbackQuery.Message?.Text;
        if (originalMessage != null)
        {
            var testId = ExtractTestId(originalMessage);
            var answerId = ExtractAnswerId(originalMessage);

            var answer = await answerRepository.SelectAsync(a => a.Id == answerId);
            var test = await testRepository.SelectAsync(t => t.Id == testId);

            // Deserialize the answers from JSON
            var userAnswersDict = JsonConvert.DeserializeObject<Dictionary<int, char>>(answer.Answers);
            var correctAnswersDict = test.Answers;

            var detailedMessage = BuildDetailedResultsMessage(originalMessage, userAnswersDict, correctAnswersDict);

            // Edit the original message with the detailed results
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: detailedMessage,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: null // Remove the inline keyboard
            );
        }
        
        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
    }

    private string BuildDetailedResultsMessage(string originalMessage, Dictionary<int, char> userAnswers, Dictionary<int, string> correctAnswers)
    {
        var sb = new StringBuilder(EscapeMarkdown(originalMessage));
        sb.AppendLine("\n\n*Batafsil ma'lumotlar:*");

        foreach (var correctAnswer in correctAnswers)
        {
            if (userAnswers.TryGetValue(correctAnswer.Key, out var userAnswer))
            {
                bool isCorrect = userAnswer.ToString().Equals(correctAnswer.Value, StringComparison.OrdinalIgnoreCase);

                sb.AppendLine($"{correctAnswer.Key}\\. {EscapeMarkdown(userAnswer.ToString())} " +
                              $"{(isCorrect ? "✅" : "❌")} " +
                              $"{(isCorrect ? "" : $"\\(To'g'ri javob: {EscapeMarkdown(correctAnswer.Value)}\\)")}");
            }
            else
            {
                sb.AppendLine($"{correctAnswer.Key}\\. ❓ Hech qanday javob berilmadi " +
                              $"\\(To'g'ri javob: {EscapeMarkdown(correctAnswer.Value)}\\)");
            }
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