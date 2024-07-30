using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
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

        public async Task Handle(Chat chat, User user, UpdateInfo update)
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
                    };

                    await _telegram.SendTextMessageAsync(chat.Id, "Panel menu\ud83d\udee0\ufe0f :",
                        replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

                    var adminResponse = await handle.NewTextMessage(update);

                    switch (adminResponse)
                    {
                        case "Yangi test\ud83c\udd95":
                            await HandleNewTest(chat, update);
                            break;
                        case "Testlarni ko'rish\ud83d\udc40":   
                            await HandleViewTests(chat, update);
                            break;
                        case "Testni o'chirish\ud83d\uddd1":
                            await HandleDeleteTest(chat, update);
                            break;
                        case "Paneldan chiqish":
                            await handler.Value.HandleAdminTask(chat, user, update);
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

        private async Task HandleNewTest(Chat chat, UpdateInfo update)
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
            var testTypeResult = await handle.NewTextMessage(update);
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
            }

            await _telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting",
                replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var creatorUser = await handle.NewTextMessage(update);
            if (creatorUser == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }
            dto.CreatorUser = creatorUser;
            dto.CreatorUserId = chat.Id;

            await _telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
            var amountResult = await handle.NewTextMessage(update);
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
            await _telegram.SendTextMessageAsync(chat.Id, "Javoblarni kiriting (abcdab yoki 1a2b3c4d5a6b) korinishida");
            var answers = await handle.NewTextMessage(update);
            if (answers == "Bekor qilish")
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Bekor qilindi.");
                return;
            }
            dto.Answers = answers;

            await _telegram.SendTextMessageAsync(chat.Id, "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
            var expirationDateResult = await handle.NewTextMessage(update);
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
            dto.ExpirationDate = expirationDate.ToUniversalTime();
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

        private async Task HandleViewTests(Chat chat, UpdateInfo update)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] {"Bekor qilish\u274c"}
            };

            await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, testning ID raqamini yozing.", replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(update);

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
                replyMarkup: new ReplyKeyboardMarkup(new[] { new KeyboardButton("Boshqa testni ko'rish") }) { ResizeKeyboard = true });
        }

        private async Task HandleDeleteTest(Chat chat, UpdateInfo update)
        {
            var cancelButton = new[]
            {
                new KeyboardButton[] {"Bekor qilish\u274c"}
            };

            await _telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting", replyMarkup: new ReplyKeyboardMarkup(cancelButton));
            var testIdMessage = await handle.NewTextMessage(update);
            
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
