using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Services;
using TestBot.VeiwModels;

namespace TestBot.Handlers
{
    public class HandleAdmin(
        HandleNextUpdate handle,
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
                        new KeyboardButton[] { "Yangi test" },
                        new KeyboardButton[] { "Testlarni ko'rish" },
                        new KeyboardButton[] { "Testni o'chirish" },
                    };

                    await _telegram.SendTextMessageAsync(chat.Id, "Hush kelibsiz admin!",
                        replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

                    var adminResponse = await handle.NewTextMessage(update);

                    switch (adminResponse)
                    {
                        case "Yangi test":
                            await HandleNewTest(chat, update);
                            break;
                        case "Testlarni ko'rish":
                            await HandleViewTests(chat, update);
                            break;
                        case "Testni o'chirish":
                            await HandleDeleteTest(chat, update);
                            break;
                        default:
                            await _telegram.SendTextMessageAsync(chat.Id, "Foydalanuvchi rejimiga otish uchun yana bir marotaba bosing.",
                                replyMarkup: new ReplyKeyboardRemove());
                            break;
                    }
                    return;
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

            await _telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting",
                replyMarkup: new ReplyKeyboardRemove());
            dto.CreatorUser = await handle.NewTextMessage(update);

            await _telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
            if (!int.TryParse(await handle.NewTextMessage(update), out var amount))
            {
                await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, haqiqiy son kiriting.");
                return;
            }
            dto.Amount = amount;

            await _telegram.SendTextMessageAsync(chat.Id, "Javoblarni kiriting (abcdab yoki 1a2b3c4d5a6b) korinishida");
            dto.Answers = await handle.NewTextMessage(update);

            await _telegram.SendTextMessageAsync(chat.Id, "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
            if (!DateTime.TryParse(await handle.NewTextMessage(update), out var expirationDate))
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
            await _telegram.SendTextMessageAsync(chat.Id, "Iltimos, testning ID raqamini yozing.", replyMarkup: new ReplyKeyboardRemove());
            var testIdMessage = await handle.NewTextMessage(update);

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
            await _telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting");
            var testIdMessage = await handle.NewTextMessage(update);

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
