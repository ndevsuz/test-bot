using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Repositories;
using TestBot.Services;
using TestBot.VeiwModels;

namespace TestBot
{
	public class TestBot : EasyBot
	{
		
		private static void Main(string[] args)
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.Build();
			var adminService = new AdminService();
			var bot = new TestBot(args[0], configuration, adminService);
			bot.Run();
		}

		public TestBot(string botToken, IConfiguration configuration, AdminService adminService) : base(botToken, configuration, adminService)
		{
		}

		protected override async Task OnPrivateChat(Chat chat, User user, UpdateInfo update)
		{
			Console.WriteLine(update.UserType);
			if (update.UpdateKind != UpdateKind.NewMessage || update.MsgCategory != MsgCategory.Text)
				return;
			if (update.Message.Text == "/admin" && update.UserType == UserType.Admin)
			{
				var buttons = new KeyboardButton[][]
				{
					new KeyboardButton[] { "Yangi test" },
					new KeyboardButton[] { "Testlarni ko'rish" },
					new KeyboardButton[] { "Testni o'chirish" },
					new KeyboardButton[] {"Orqaga🔙"}
				};
				await Telegram.SendTextMessageAsync(chat.Id, "Hush kelibsiz admin! :",
					replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

				var adminResponse = await NewTextMessage(update);
        
				switch (adminResponse)
				{
					case "Yangi test":
						var dto = new TestCreationModel();
						
						await Telegram.SendTextMessageAsync(chat.Id, "Ismingiz va familiyangizni kiriting",
							replyMarkup: new ReplyKeyboardRemove());
						dto.CreatorUser = await NewTextMessage(update);

						await Telegram.SendTextMessageAsync(chat.Id, "Testlar sonini kiriting");
						dto.Amount = int.Parse(await NewTextMessage(update));
						
						await Telegram.SendTextMessageAsync(chat.Id, "Javoblarni kiriting (abcdab yoki 1a2b3c4d5a6b) korinishida");
						dto.Answers = await NewTextMessage(update);
						
						await Telegram.SendTextMessageAsync(chat.Id, "Test qachon tugashini kiriting (yil/oy/kun soat:minut)");
						dto.ExpirationDate = DateTime.Parse(await NewTextMessage(update));
						
						var result = await _adminService.HandleNewTest(dto);
						await Telegram.SendTextMessageAsync(chat.Id, $"Testning ID raqami : {result.ToString()}");
						break;
					
					case "Testlarni ko'rish":
						await Telegram.SendTextMessageAsync(chat.Id, "Testlar");
						var tests = await _adminService.GetAllTests();
						foreach (var test in tests)
						{
							await Telegram.SendTextMessageAsync(chat.Id, test,
								replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
						}
						break;
					
					case "Testni o'chirish":
						await Telegram.SendTextMessageAsync(chat.Id, "O'chirish uchun test ID sini kiriting");
						var testId = long.Parse(await NewTextMessage(update));
						var isDeleted = await _adminService.DeleteTest(testId);
						if(isDeleted)
							await Telegram.SendTextMessageAsync(chat.Id, "Test o'chirildi",
								replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
						else
							await Telegram.SendTextMessageAsync(chat.Id, "Bunday ID bilan test mavjud emas",
								replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });
						break;
					
					default:
						await Telegram.SendTextMessageAsync(chat.Id, "Unknown command. Please use the buttons provided.", replyMarkup: new ReplyKeyboardRemove());
						break;
				}

			}
		}
	}
}