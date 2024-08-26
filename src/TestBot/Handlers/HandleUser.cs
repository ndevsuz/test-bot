using AnswerBot.Repositories;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Helpers;
using TestBot.Interfaces;
using TestBot.Models;
using TestBot.Services;
using User = Telegram.Bot.Types.User;

namespace TestBot.Handlers;

public class HandleUser(
    HandleNextUpdate handle,
    Lazy<IHandler> handler,
    IAnswerRepository answerRepository,
    AdminService adminService,
    IConfiguration configuration,
    IOptions<BotConfiguration> botConfiguration)
{
    private readonly ITelegramBotClient _telegram = new TelegramBotClient(botConfiguration.Value.BotToken);

    public async Task Handle(Chat chat, User user, UpdateInfo updateInfo, Update update)
    {
        while (true)
        {
            await IsSubscribed(updateInfo, chat, user);
            var buttons = new KeyboardButton[][]
            {
                ["Javobni tekshirish\ud83d\udd0d:"],
                ["Panelga o'tish\u2699\ufe0f"]
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Bosh menu\ud83c\udfe0:",
                replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

            var userResponse = await handle.NewTextMessage(updateInfo, update);

            switch (userResponse)
            {
                case "Javobni tekshirish\ud83d\udd0d:":
                    await HandleExam(chat, updateInfo, update);
                    break;
                case "Panelga o'tish\u2699\ufe0f":
                    await handler.Value.HandleAdminTask(chat, user, updateInfo, update);
                    break;
                case "/panel":
                    await handler.Value.HandleAdminTask(chat, user, updateInfo, update);
                    return;
            }
        }
    }

    private async Task HandleExam(Chat chat, UpdateInfo updateInfo, Update update)
    {
        var cancelButton = new[]
        {
        new KeyboardButton[] { "Bekor qilish\u274c" }
        };

        await _telegram.SendTextMessageAsync(chat.Id, "Ism familiyangizni kirting: ",
        replyMarkup: new ReplyKeyboardMarkup(cancelButton));
        var userName = await handle.NewTextMessage(updateInfo, update);
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
            var testMessage = await handle.NewTextMessage(updateInfo, update);
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
                    await _telegram.SendTextMessageAsync(chat.Id,
                        "Iltimos, misoldagidek kiriting:\nMisol: {testid}*1a2b3c yoki {testid}*abc");
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

        // Convert user answers to a dictionary
        var userAnswerDict = ExtractAnswers(userAnswers);

        // Get the correct answers dictionary from the test
        var correctAnswers = test.Answers;

        // Calculate the percentage and counts
        var (percentage, correctCount, incorrectCount) = CalculateCorrectAnswerPercentage(userAnswerDict, correctAnswers);

        string message = $@"👤 *Foydalanuvchi:* [{EscapeMarkdown(userName)}](tg://user?id={chat.Id})
📝 *Testning nomi:* {EscapeMarkdown(test.Name)}
✍️ *Muallif:* {EscapeMarkdown(test.CreatorUser)}
🔢 *Jami savollar:* {test.Amount}
✅ *Tog'ri javoblar:* {correctCount}
📊 *Foyiz:* {(int)percentage}%";

        await _telegram.SendTextMessageAsync(
            chat.Id,
            message,
            parseMode: ParseMode.MarkdownV2
        );

        var answers = new Answer()
        {
            AnswersDictionary = userAnswerDict,
            TestId = testId,
            UserId = chat.Id,
            UserName = chat.FirstName
        };

            var answer = await answerRepository.AddAsync(answers);

        await SendResultToCreatorUserAsync(chat, updateInfo, userName, test, correctCount, percentage, answer.Id);
    }
    

    private (double percentage, int correctCount, int incorrectCount) CalculateCorrectAnswerPercentage(
        Dictionary<int, string> userAnswers, Dictionary<int, string> correctAnswers)
    {
        int correctCount = 0;

        foreach (var userAnswer in userAnswers)
        {
            if (correctAnswers.TryGetValue(userAnswer.Key, out var correctAnswer) &&
                userAnswer.Value == correctAnswer)
            {
                correctCount++;
            }
        }

        int incorrectCount = correctAnswers.Count - correctCount;
        double percentage = (double)correctCount / correctAnswers.Count * 100;

        return (percentage, correctCount, incorrectCount);
    }

    private Dictionary<int, char> ParseAnswers(string answers)
    {
        var answerDict = new Dictionary<int, char>();

        if (answers.All(char.IsLetter))
        {
            for (int i = 0; i < answers.Length; i++)
            {
                answerDict[i + 1] = answers[i];
            }
        }
        else
        {
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

    private static Dictionary<int, char> CreateDictionaryFromInput(string input)
    {
        var dictionary = new Dictionary<int, char>();
        var i = 0;

        while (i < input.Length)
        {
            var j = i;
            while (j < input.Length && char.IsDigit(input[j]))
            {
                j++;
            }

            if (j < input.Length && j > i && char.IsLetter(input[j]))
            {
                var key = int.Parse(input.Substring(i, j - i));
                var value = input[j];
                dictionary[key] = value;
                i = j + 1;
            }
            else
            {
                break;
            }
        }

        return dictionary;
    }

    private async Task SendResultToCreatorUserAsync(Chat chat, UpdateInfo updateInfo, string userName, Test test,
        int correctCount, double percentage, long answerId)
    {
        //

        string message = $@"*🔔Sizning testingizga javob berildi\!*


🆔 *Test ID* {EscapeMarkdown(test.Id.ToString())}
🆔 *Javobning IDsi* {EscapeMarkdown(answerId.ToString())}
👤 *Foydalanuvchi:* [{EscapeMarkdown(userName)}](tg://user?id={EscapeMarkdown(chat.Id.ToString())})
📝 *Testning nomi:* {EscapeMarkdown(test.Name)}
✍️ *Muallif:* {EscapeMarkdown(test.CreatorUser)}
🔢 *Jami savollar:* {EscapeMarkdown(test.Amount.ToString())}
✅ *Tog'ri javoblar:* {EscapeMarkdown(correctCount.ToString())}
📊 *Foyiz:* {EscapeMarkdown(percentage.ToString("F2"))}\\%";

        var inlineButton = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Batafsil📊", "GetDetails"),
        });

        await _telegram.SendTextMessageAsync(
            chatId: test.CreatorUserId,
            text: message,
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: inlineButton
        );
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
            .Replace("!", "\\!")
            .Replace(",", "\\,");
    }
    private Dictionary<int, string> ExtractAnswers(string input)
    {
        // Remove any whitespace
        input = input.Replace(" ", "");

        // Initialize a dictionary to store the extracted answers
        var answerDictionary = new Dictionary<int, string>();

        // Initialize variables to keep track of the current key and value
        int currentKey = 0;
        string currentValue;

        // Loop through each character in the input string
        foreach (var c in input)
        {
            if (char.IsDigit(c))
            {
                // If the current character is a digit, build the key
                currentKey = currentKey * 10 + int.Parse(c.ToString());
            }
            else if (char.IsLetter(c))
            {
                // If the current character is a letter, it’s the answer for the current key
                currentValue = c.ToString();
                if (currentKey != 0) // Make sure the key is valid
                {
                    answerDictionary[currentKey] = currentValue;
                    currentKey = 0; // Reset key for the next entry
                }
            }
        }

        return answerDictionary;
    }

    private async Task IsSubscribed(UpdateInfo updateInfo, Chat chat, User user)
    {
        while (true)
        {
            var isMember = await CheckMember.CheckMemberAsync(_telegram, chat, configuration);
            if (isMember)
                return;

            var message = await _telegram.SendTextMessageAsync(chat.Id,
                "Botdan foydalanish uchun, pasdagi tugmani bosib kanalga obuna bo'ling, va Tekshirish ni bosing.",
                replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[]
                {
                    new("Obuna bo'lish") { Url = "https://t.me/kelajakkabirqadam_1" },
                    new("Tekshirish✅") { CallbackData = "Subscribed" }
                }));

            var callbackResult = await handle.ButtonClicked(updateInfo, updateInfo.Message);
            if (callbackResult == "Subscribed")
            {
                var result = await CheckMember.CheckMemberAsync(_telegram, chat, configuration);
                if (!result)
                {
                    await _telegram.DeleteMessageAsync(chat.Id, message.MessageId);
                    continue;
                }

                ReplyCallback(updateInfo, "Muvaffaqiyatli\u2705");
                await _telegram.DeleteMessageAsync(chat.Id, message.MessageId);
                return;
            }
        }
    }

    public void ReplyCallback(UpdateInfo update, string text = null, bool showAlert = false, string url = null)
    {
        if (update.Update.Type != UpdateType.CallbackQuery)
            throw new InvalidOperationException("This method can be called only for CallbackQue	ry updates");
        _ = _telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, text, showAlert, url);
    }

}
    