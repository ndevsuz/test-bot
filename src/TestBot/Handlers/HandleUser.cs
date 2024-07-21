using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Services;

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
                new KeyboardButton[] { "\ud83d\udc64 Mening ma'lumotlarim" },
            };
            
            await _telegram.SendTextMessageAsync(chat.Id, "Bosh menu:", replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true } );

            var userResponse = await handle.NewTextMessage(update);

            switch (userResponse)
            {
                case "\u2705Javobni tekshirish":
                    await HandleExam(chat, update);
                    break;
            }

            return;
        }
    }

    private async Task HandleExam(Chat chat, UpdateInfo update)
    {
        await _telegram.SendTextMessageAsync(chat.Id, "Example: {testid}*1a2b3c or 1a3c or abc...", replyMarkup: new ReplyKeyboardRemove());
        var testMessage = await handle.NewTextMessage(update);

        // Check if the message contains '*' indicating test ID and answers
        string userAnswers;
        long testId = 0;
        if (testMessage.Contains('*'))
        {
            var parts = testMessage.Split('*');
            if (parts.Length != 2 || !long.TryParse(parts[0], out testId))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Invalid format. Please provide the test ID and answers in the correct format.");
                return;
            }
            userAnswers = parts[1];
        }
        else
        {
            userAnswers = testMessage;
        }

        // Retrieve the test
        var test = await adminService.GetTestById(testId);
        if (test == null)
        {
            await _telegram.SendTextMessageAsync(chat.Id, "Test not found.");
            return;
        }

        // Check if the test has expired
        if (DateTime.UtcNow > test.ExpirationDate)
        {
            await _telegram.SendTextMessageAsync(chat.Id, "This test has expired.");
            return;
        }

        // Calculate the percentage of correct answers
        var correctAnswers = test.Answers;
        var percentage = CalculateCorrectAnswerPercentage(userAnswers, correctAnswers);

        await _telegram.SendTextMessageAsync(chat.Id, $"Your score: {percentage}%");
    }

    private double CalculateCorrectAnswerPercentage(string userAnswers, string correctAnswers)
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

        return (double)correctCount / correctAnswerDict.Count * 100;
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