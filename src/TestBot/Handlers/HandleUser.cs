using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Models;
using TestBot.Services;
using User = Telegram.Bot.Types.User;

namespace TestBot.Handlers;

public class HandleUser(HandleNextUpdate handle, AdminService adminService, IOptions<BotConfiguration> botConfiguration)
{
    private readonly ITelegramBotClient _telegram = new TelegramBotClient(botConfiguration.Value.BotToken);

    public async Task Handle(Chat chat, User user, UpdateInfo update)
    {
        while(true) 
        {
            var buttons = new KeyboardButton[][]
            {
                new KeyboardButton[] { "\u2705Javobni tekshirish" },
            };
            
            await _telegram.SendTextMessageAsync(chat.Id, "Bosh menu:", replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true } );

            var userResponse = await handle.NewTextMessage(update);

            switch (userResponse)
            {
                case "\u2705Javobni tekshirish":
                    await HandleExam(chat, update);
                    break;
                case "/admin":
                    return;
            }

            return;
        }
    }

    private async Task HandleExam(Chat chat, UpdateInfo update)
    {
        await _telegram.SendTextMessageAsync(chat.Id, "Ism familiyangizni kirting: ", replyMarkup: new ReplyKeyboardRemove());
        var userName = await handle.NewTextMessage(update);

        await _telegram.SendTextMessageAsync(chat.Id, "Javoblarni kiriting:\n Misol: {testid}*1a2b3c yoki {testid}*abc", replyMarkup: new ReplyKeyboardRemove());
        var testMessage = await handle.NewTextMessage(update);

        string userAnswers;
        long testId = 0;
        if (testMessage.Contains('*'))
        {
            var parts = testMessage.Split('*');
            if (parts.Length != 2 || !long.TryParse(parts[0], out testId))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, misoldagidek kiriting:\nMisol: {testid}*1a2b3c yoki {testid}*abc");
                return;
            }
            userAnswers = parts[1];
        }
        else
        {
            userAnswers = testMessage;
        }

        var test = await adminService.GetTestById(testId);
        if (test == null)
        {
            await _telegram.SendTextMessageAsync(chat.Id, "Bunday test topilmadi..");
            return;
        }

        if (DateTime.UtcNow > test.ExpirationDate)
        {
            await _telegram.SendTextMessageAsync(chat.Id, "Bu testni vaqti tugagan..");
            return;
        }

        var correctAnswers = test.Answers;
        var (percentage, correctCount, incorrectCount) = CalculateCorrectAnswerPercentage(userAnswers, correctAnswers);

        await _telegram.SendTextMessageAsync(chat.Id, $"\ud83d\udc64Foydalanuvchi: {userName}\n\n \u270d\ud83c\udfffMuallif: {test.CreatorUser}\n \ud83d\udcd6Jami savollar: {test.Amount}\n \u2705Tog'ri javoblar: {correctCount}\n \ud83d\udd0dFoyiz: {percentage}");
    }

    private (double percentage, int correctCount, int incorrectCount) CalculateCorrectAnswerPercentage(string userAnswers, string correctAnswers)
    {
        var correctAnswerDict = ParseAnswers(correctAnswers);
        var userAnswerDict = ParseAnswers(userAnswers);

        int correctCount = 0;
        foreach (var userAnswer in userAnswerDict)
        {
            if (correctAnswerDict.TryGetValue(userAnswer.Key, out var correctAnswer) && userAnswer.Value == correctAnswer)
            {
                correctCount++;
            }
        }

        int incorrectCount = correctAnswerDict.Count - correctCount;
        double percentage = (double)correctCount / correctAnswerDict.Count * 100;

        return (percentage, correctCount, incorrectCount);
    }

    private Dictionary<int, char> ParseAnswers(string answers)
    {
        var answerDict = new Dictionary<int, char>();

        if (answers.All(char.IsLetter))
        {
            // If the answers are only letters (e.g., "abc"), assume sequential questions
            for (int i = 0; i < answers.Length; i++)
            {
                answerDict[i + 1] = answers[i];
            }
        }
        else
        {
            // If the answers are in the format "1a2b3c" or "1a3c"
            for (int i = 0; i < answers.Length; i += 2)
            {
                if (i + 1 < answers.Length && char.IsDigit(answers[i]) && char.IsLetter(answers[i + 1]))
                {
                    int questionNumber = int.Parse(answers[i].ToString());
                    char answer = answers[i + 1];
                    answerDict[questionNumber] = answer;
                }
            }
        }

        return answerDict;
    }
} 