using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Interfaces;
using TestBot.Services;
using TestBot.VeiwModels;

namespace TestBot.Handlers
{
    public class HandleAdmin(
        HandleNextUpdate handle,
        Lazy<IHandler> handler,
        IOptions<BotConfiguration> botConfiguration,
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

                    var adminResponse = await handle.NewTextMessage(updateInfo);

                    switch (adminResponse)
                    {
                        case "Yangi test\ud83c\udd95":
                            await HandleNewTest(chat, updateInfo);
                            break;
                        case "Testlarni ko'rish\ud83d\udc40":   
                            await HandleViewTests(chat, updateInfo);
                            break;
                        case "Testni o'chirish\ud83d\uddd1":
                            await HandleDeleteTest(chat, updateInfo);
                            break;
                        case "Paneldan chiqish\ud83d\udeaa":
                            await handler.Value.HandleUserTask(chat, user, updateInfo, update);
                            return;
                        default:
                            await _telegram.SendTextMessageAsync(chat.Id, "Foydalanuvchi rejimiga otish uchun yana bir marotaba bosing.",
                                replyMarkup: new ReplyKeyboardRemove());
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task HandleNewTest(Chat chat, UpdateInfo updateInfo)
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
                new KeyboardButton[] {"Bekor qilish"}
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Testni turini tanlang: ",
                replyMarkup: new ReplyKeyboardMarkup(buttons));
            var testTypeResult = await handle.NewTextMessage(updateInfo);
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
            var testName = await handle.NewTextMessage(updateInfo);
            if (testName == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }
            dto.Name = testName;

            await _telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting");
            var creatorUser = await handle.NewTextMessage(updateInfo);
            if (creatorUser == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }
            dto.CreatorUser = creatorUser;
            dto.CreatorUserId = chat.Id;

            await _telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
            var amountResult = await handle.NewTextMessage(updateInfo);
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
            await _telegram.SendTextMessageAsync(chat.Id, "\u2705Testning javoblarini kiriting\\.\n\n\u270d\ufe0fMisol uchun: \n>abcdabcdabcd\\.\\.\\.  yoki\n>1a2b3c4d5a6b7c\\.\\.\\.", parseMode:ParseMode.MarkdownV2);
            var answers = await handle.NewTextMessage(updateInfo);
            if (answers == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }
            dto.Answers = answers;

            await _telegram.SendTextMessageAsync(chat.Id, "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
            var expirationDateResult = await handle.NewTextMessage(updateInfo);
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

        private async Task HandleViewTests(Chat chat, UpdateInfo updateInfo)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] {"Bekor qilish\u274c"}
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, testning ID raqamini yozing.", replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(updateInfo);

            if (testIdMessage is "Bekor qilish\u274c")
                return;

            if (!long.TryParse(testIdMessage, out var testId))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy ID raqamini yozing.");
                return;
            }
            var test = await adminService.GetById(testId);
            if (test == null)
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bunday ID raqamli test topilmadi.");
                return;
            }

            await _telegram.SendTextMessageAsync(chat.Id, test,
                replyMarkup: new ReplyKeyboardRemove(), parseMode: ParseMode.MarkdownV2);
        }

        private async Task HandleDeleteTest(Chat chat, UpdateInfo updateInfo)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] {"Bekor qilish\u274c"}
            };

            await _telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting", replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(updateInfo);
            
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
                    replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton[][] { new KeyboardButton[] { "Boshqa testni o'chirish" } }) { ResizeKeyboard = true });
            }
            else
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bunday ID bilan test mavjud emas",
                    replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton[][] { new KeyboardButton[] { "Boshqa testni o'chirish" } }) { ResizeKeyboard = true });
            }
        }
    }
}
