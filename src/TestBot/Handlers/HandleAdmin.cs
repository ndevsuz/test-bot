using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Helpers;
using TestBot.Interfaces;
using TestBot.Services;
using TestBot.VeiwModels;

namespace TestBot.Handlers
{
    public class HandleAdmin(
        HandleNextUpdate handle,
        Lazy<IHandler> handler,
        IOptions<BotConfiguration> botConfiguration,
        IConfiguration configuration,
        AdminService adminService)
    {
        private readonly ITelegramBotClient _telegram = new TelegramBotClient(botConfiguration.Value.BotToken);

        public async Task Handle(Chat chat, User user, UpdateInfo updateInfo, Update update)
        {
            try
            {
                while (true)
                {
                    var buttons = new KeyboardButton[][]
                    {
                        ["Yangi test\ud83c\udd95"],
                        ["Testlarni ko'rish\ud83d\udc40"],
                        ["Testni o'chirish\ud83d\uddd1"],
                        ["Paneldan chiqish\ud83d\udeaa"]
                    };

                    await _telegram.SendTextMessageAsync(chat.Id, "Panel menu\ud83d\udee0\ufe0f :",
                        replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

                    var adminResponse = await handle.NewTextMessage(updateInfo, update);

                    switch (adminResponse)
                    {
                        case "Yangi test\ud83c\udd95":
                            await HandleNewTest(chat, updateInfo, update);
                            break;
                        case "Testlarni ko'rish\ud83d\udc40":
                            await HandleViewTests(chat, updateInfo, update);
                            break;
                        case "Testni o'chirish\ud83d\uddd1":
                            await HandleDeleteTest(chat, updateInfo, update);
                            break;
                        case "Paneldan chiqish\ud83d\udeaa":
                            await handler.Value.HandleUserTask(chat, user, updateInfo, update);
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task HandleNewTest(Chat chat, UpdateInfo updateInfo, Update update)
        {
            var dto = new TestCreationModel();

            var buttons = new[]
            {
                new KeyboardButton[] { "Sertefikatli" },
                new KeyboardButton[] { "Sertefikatsiz" },
                new KeyboardButton[] { "Bekor qilish" },
            };

            var cancelButton = new[]
            {
                new KeyboardButton[] { "Bekor qilish" }
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Testni turini tanlang: ",
                replyMarkup: new ReplyKeyboardMarkup(buttons));
            var testTypeResult = await handle.NewTextMessage(updateInfo, update);
            switch (testTypeResult)
            {
                case "Bekor qilish":
                    await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                    return;
                case "Sertefikatli":
                    dto.IsRewarded = true;
                    break;
                case "Sertefikatsiz":
                    dto.IsRewarded = false;
                    break;
                default:
                    dto.IsRewarded = false;
                    break;
            }

            await _telegram.SendTextMessageAsync(chat.Id, "Testning nomini kiriting: ",
                replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testName = await handle.NewTextMessage(updateInfo, update);
            if (testName == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }

            dto.Name = testName;

            await _telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting");
            var creatorUser = await handle.NewTextMessage(updateInfo, update);
            if (creatorUser == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }

            dto.CreatorUser = creatorUser;
            dto.CreatorUserId = chat.Id;

            await _telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
            var amountResult = await handle.NewTextMessage(updateInfo, update);
            if (amountResult == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }

            if (!int.TryParse(amountResult, out var amount))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy son kiriting.");
                return;
            }

            dto.Amount = amount;
            await _telegram.SendTextMessageAsync(chat.Id,
                "\u2705Testning javoblarini kiriting\\.\n\n\u270d\ufe0fMisol uchun: \n>abcdabcdabcd\\.\\.\\.  yoki\n>1a2b3c4d5a6b7c\\.\\.\\.",
                parseMode: ParseMode.MarkdownV2);
            var answers = await handle.NewTextMessage(updateInfo, update);
            if (answers == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }

            dto.Answers = answers;

            await _telegram.SendTextMessageAsync(chat.Id, "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
            var expirationDateResult = await handle.NewTextMessage(updateInfo, update);
            if (expirationDateResult == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }

            if (!DateTime.TryParse(expirationDateResult, out var expirationDate))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy sana kiriting.");
                return;
            }

            dto.ExpirationDate = expirationDate.AddHours(5).ToUniversalTime();
            try
            {
                var result = await adminService.HandleNewTest(dto);
                await _telegram.SendTextMessageAsync(chat.Id, result);
            }
            catch (Exception ex)
            {
                await _telegram.SendTextMessageAsync(chat.Id, $"{ex.Message} {ex.InnerException}");
            }
        }

        private async Task HandleViewTests(Chat chat, UpdateInfo updateInfo, Update update)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] { "Bekor qilish\u274c" }
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, testning ID raqamini yozing.",
                replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(updateInfo, update);

            if (testIdMessage is "Bekor qilish\u274c")
                return;

            if (!long.TryParse(testIdMessage, out var testId))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy ID raqamini yozing.");
                return;
            }

            var test = await adminService.GetById(testId, chat.Id);
            if (test == null)
            {
                await _telegram.SendTextMessageAsync(chat.Id,
                    "Bunday ID raqamli test topilmadi, Yoki u test sizga tegishlik emas!");
                return;
            }

            await _telegram.SendTextMessageAsync(chat.Id, test,
                replyMarkup: new ReplyKeyboardRemove(), parseMode: ParseMode.MarkdownV2);
        }

        private async Task HandleDeleteTest(Chat chat, UpdateInfo updateInfo, Update update)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] { "Bekor qilish\u274c" }
            };

            await _telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting",
                replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(updateInfo, update);

            if (testIdMessage is "Bekor qilish\u274c")
                return;

            if (!long.TryParse(testIdMessage, out var testId))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy ID raqamini yozing.");
                return;
            }

            var isDeleted = await adminService.DeleteTest(testId);
            if (isDeleted)
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Test o'chirildi",
                    replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton[][]
                        { new KeyboardButton[] { "Boshqa testni o'chirish" } }) { ResizeKeyboard = true });
            }
            else
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bunday ID bilan test mavjud emas",
                    replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton[][]
                        { new KeyboardButton[] { "Boshqa testni o'chirish" } }) { ResizeKeyboard = true });
            }
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
}
