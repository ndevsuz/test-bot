using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Helpers;
using TestBot.Interfaces;
using TestBot.Services;
using User = Telegram.Bot.Types.User;

namespace TestBot.Handlers;

public class HandleUser(HandleNextUpdate handle,
    Lazy<IHandler> handler, 
    AdminService adminService,
    IOptions<BotConfiguration> botConfiguration)
{
    private readonly ITelegramBotClient _telegram = new TelegramBotClient(botConfiguration.Value.BotToken);

    public async Task Handle(Chat chat, User user, UpdateInfo update)
    {
        while(true) 
        {
            var buttons = new KeyboardButton[][]
            {
                ["Javobni tekshirish\ud83d\udd0d:"],
            };
            
            await _telegram.SendTextMessageAsync(chat.Id, "Bosh menu\ud83c\udfe0:", replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true } );

            var userResponse = await handle.NewTextMessage(update);

            switch (userResponse)
            {
                case "Javobni tekshirish\ud83d\udd0d:":
                    await HandleExam(chat, update);
                    break;
                case "/panel":
                    await handler.Value.HandleAdminTask(chat, user, update);
                    return;
            }
        }
    }

    private async Task HandleExam(Chat chat, UpdateInfo update)
    {
        var cancelButton = new[]
        {
            new KeyboardButton[] {"Bekor qilish\u274c"}
        };

        await _telegram.SendTextMessageAsync(chat.Id, "Ism familiyangizni kirting: ", replyMarkup: new ReplyKeyboardMarkup(cancelButton));
        var userName = await handle.NewTextMessage(update);
        if (userName is "Bekor qilish\u274c")
        {
            await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.", replyMarkup: new ReplyKeyboardRemove());
            return;
        }
        string userAnswers;
        long testId = 0;
        await _telegram.SendTextMessageAsync(chat.Id, 
            "\u2705Test kodini kiritib \\* \\(yulduzcha\\) belgisini qo'yasiz va barcha kalitni kiritasiz\\.\n\n\u270d\ufe0fMisol uchun: \n>123\\*abcdabcdabcd\\.\\.\\.  yoki\n>123\\*1a2b3c4d5a6b7c\\.\\.\\.",
            parseMode: ParseMode.MarkdownV2);        
        while (true)
        {
            var testMessage = await handle.NewTextMessage(update);
            if (testMessage is "Bekor qilish\u274c")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.", replyMarkup: new ReplyKeyboardRemove());
                return;
            }


            if (testMessage.Contains('*'))
            {
                var parts = testMessage.Split('*');
                if (parts.Length != 2 || !long.TryParse(parts[0], out testId))
                {
                    await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, misoldagidek kiriting:\nMisol: {testid}*1a2b3c yoki {testid}*abc");
                    continue;
                }
                userAnswers = parts[1];
                break;
            }
            userAnswers = testMessage;
            break;

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