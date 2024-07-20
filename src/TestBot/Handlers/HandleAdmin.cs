using System.Net;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Services;
using TestBot.VeiwModels;

namespace TestBot.Handlers;

public class HandleAdmin(HandleNextMessage handle, IOptions<BotConfiguration> botConfiguration, AdminService adminService)
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
				    new KeyboardButton[] { "Orqaga 🔙" }
			    };
			    await _telegram.SendTextMessageAsync(chat.Id, "Hush kelibsiz admin! :",
				    replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

			    var adminResponse = await handle.NewTextMessage(update);

			    switch (adminResponse)
			    {
				    case "Yangi test":
					    var dto = new TestCreationModel();

					    await _telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting",
						    replyMarkup: new ReplyKeyboardRemove());
					    dto.CreatorUser = await handle.NewTextMessage(update);

					    await _telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
					    dto.Amount = int.Parse(await handle.NewTextMessage(update));

					    await _telegram.SendTextMessageAsync(chat.Id,
						    "Javoblarni kiriting (abcdab yoki 1a2b3c4d5a6b) korinishida");
					    dto.Answers = await handle.NewTextMessage(update);

					    await _telegram.SendTextMessageAsync(chat.Id,
						    "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
					    dto.ExpirationDate = DateTime.Parse(await handle.NewTextMessage(update)).ToUniversalTime();

					    try
					    {
						    var result = await adminService.HandleNewTest(dto);
							await _telegram.SendTextMessageAsync(chat.Id, result);
					    }
					    catch(Exception ex)
					    {
						    await _telegram.SendTextMessageAsync(chat.Id, $"{ex.Message + ex.InnerException}");
					    }
					    break;

				    case "Testlarni ko'rish":
					    await _telegram.SendTextMessageAsync(chat.Id, "Testlar");
					    var tests = await adminService.GetAllTests();
					    foreach (var test in tests)
					    {
						    await _telegram.SendTextMessageAsync(chat.Id, test,
							    replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
					    }

					    break;

				    case "Testni o'chirish":
					    await _telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting");
					    var testId = long.Parse(await handle.NewTextMessage(update));
					    var isDeleted = await adminService.DeleteTest(testId);
					    if (isDeleted)
						    await _telegram.SendTextMessageAsync(chat.Id, "Test o'chirildi",
							    replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
					    else
						    await _telegram.SendTextMessageAsync(chat.Id, "Bunday ID bilan test mavjud emas",
							    replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
					    break;
				    
				    default:
					    await _telegram.SendTextMessageAsync(chat.Id, "Unknown command. Please use the buttons provided.",
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
}