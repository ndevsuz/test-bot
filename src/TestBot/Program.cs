using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Services;

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
					new KeyboardButton[] { "Testlarni ko'rish(buni qoshmimiz yuz dollru)" },
					new KeyboardButton[] {"Orqaga🔙"}
				};
				await Telegram.SendTextMessageAsync(chat.Id, "Hush kelibsiz admin! :",
					replyMarkup: new ReplyKeyboardMarkup(buttons) { ResizeKeyboard = true });

				var adminResponse = await NewTextMessage(update);
        
				switch (adminResponse)
				{
					case "Yangi test":
						var result = await _adminService.HandleNewTest();
						await Telegram.SendTextMessageAsync(chat.Id, result.ToString());
						break;
					case "Testlarni ko'rish":
						await Telegram.SendTextMessageAsync(chat.Id, "Showing existing tests...");
						break;
					default:
						await Telegram.SendTextMessageAsync(chat.Id, "Unknown command. Please use the buttons provided.", replyMarkup: new ReplyKeyboardRemove());
						break;
				}

			}
		}
	}
}