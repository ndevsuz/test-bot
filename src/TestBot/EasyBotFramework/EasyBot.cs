using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TestBot.Services;

namespace TestBot.EasyBotFramework
{
	public class EasyBot	// A fun way to code Telegram Bots, by Wizou
	{
		public readonly TelegramBotClient Telegram;
		protected IConfiguration _configuration;
		private User Me { get; set; }
		protected string BotName => Me.Username;

		private int _lastUpdateId = -1;
		private readonly CancellationTokenSource _cancel = new();
		private readonly Dictionary<long, TaskInfo> _tasks = new();

		protected virtual Task OnPrivateChat(Chat chat, User user, UpdateInfo updateInfo, Update update) => Task.CompletedTask;
		protected virtual Task OnGroupChat(Chat chat, UpdateInfo update) => Task.CompletedTask;
		protected virtual Task OnChannel(Chat channel, UpdateInfo update) => Task.CompletedTask;
		protected virtual Task OnOtherEvents(UpdateInfo update) => Task.CompletedTask;

		protected EasyBot(string botToken, IConfiguration configuration)
		{
			Telegram = new(botToken);
			Me = Task.Run(() => Telegram.GetMeAsync()).Result;
			_configuration = configuration;
		}

		public void Run() => RunAsync().Wait();

		private async Task RunAsync()
		{
			try
			{
				Console.WriteLine($"Press Escape to stop the {BotName} bot");
				while (true)
				{
					try
					{
						var updates = await Telegram.GetUpdatesAsync(_lastUpdateId + 1, timeout: 2);
						foreach (var update in updates)
							HandleUpdate(update);
						/*
						if (Console.KeyAvailable)
							if (Console.ReadKey().Key == ConsoleKey.Escape)
								break;
					*/
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
						continue;
					}
				}

				await _cancel.CancelAsync();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		public async Task<string> CheckWebhook(string url)
		{
			var webhookInfo = await Telegram.GetWebhookInfoAsync();
			string result = $"{BotName} is running";
			if (webhookInfo.Url != url)
			{
				await Telegram.SetWebhookAsync(url);
				result += " and now registered as Webhook";
			}
			return $"{result}\n\nLast webhook error: {webhookInfo.LastErrorDate} {webhookInfo.LastErrorMessage}";
		}

		/// <summary>Use this method in your WebHook controller</summary>
		private void HandleUpdate(Update update)
		{
			//Console.WriteLine($"{update.Message.Chat.Id}    {update.Message.Text}");
			if (update.Id <= _lastUpdateId) return;
			_lastUpdateId = update.Id;
			switch (update.Type)
			{
				case UpdateType.Message: HandleUpdate(update, UpdateKind.NewMessage, update.Message); break;
				case UpdateType.EditedMessage: HandleUpdate(update, UpdateKind.EditedMessage, update.EditedMessage); break;
				case UpdateType.ChannelPost: HandleUpdate(update, UpdateKind.NewMessage, update.ChannelPost); break;
				case UpdateType.EditedChannelPost: HandleUpdate(update, UpdateKind.EditedMessage, update.EditedChannelPost); break;
				case UpdateType.CallbackQuery: HandleUpdate(update, UpdateKind.CallbackQuery, update.CallbackQuery.Message); break;
				case UpdateType.MyChatMember: HandleUpdate(update, UpdateKind.OtherUpdate, chat: update.MyChatMember.Chat); break;
				case UpdateType.ChatMember: HandleUpdate(update, UpdateKind.OtherUpdate, chat: update.ChatMember?.Chat); break;
				default: HandleUpdate(update, UpdateKind.OtherUpdate); break;
			}
		}

		private void HandleUpdate(Update update, UpdateKind updateKind, Message message = null, Chat chat = null)
		{
			TaskInfo taskInfo;
			chat ??= message?.Chat;
			long chatId = chat?.Id ?? 0;
			lock (_tasks)
				if (!_tasks.TryGetValue(chatId, out taskInfo))
					_tasks[chatId] = taskInfo = new TaskInfo();
			var updateInfo = new UpdateInfo(taskInfo, _configuration) { UpdateKind = updateKind, Update = update, Message = message };
			if (update.Type is UpdateType.CallbackQuery)
				updateInfo.CallbackData = update.CallbackQuery.Data;
			lock (taskInfo)
				if (taskInfo.Task != null)
				{
					taskInfo.Updates.Enqueue(updateInfo);
					taskInfo.Semaphore.Release();
					return;
				}
			RunTask(taskInfo, updateInfo, chat, update);
		}

		private void RunTask(TaskInfo taskInfo, UpdateInfo updateInfo, Chat chat, Update update)
		{
			Func<Task> taskStarter = (chat?.Type) switch
			{
				ChatType.Private => () => OnPrivateChat(chat, updateInfo.Message?.From, updateInfo, update),
				ChatType.Group or ChatType.Supergroup => () => OnGroupChat(chat, updateInfo),
				ChatType.Channel => () => OnChannel(chat, updateInfo),
				_ => () => OnOtherEvents(updateInfo),
			};
			taskInfo.Task = Task.Run(taskStarter).ContinueWith(async t =>
			{
				lock (taskInfo)
					if (taskInfo.Semaphore.CurrentCount == 0)
					{
						taskInfo.Task = null;
						return;
					}
				var newUpdate = await ((IGetNext)updateInfo).NextUpdate(_cancel.Token);
				RunTask(taskInfo, newUpdate, chat, update);
			});
		}
		
		public void ReplyCallback(UpdateInfo update, string text = null, bool showAlert = false, string url = null)
		{
			if (update.Update.Type != UpdateType.CallbackQuery)
				throw new InvalidOperationException("This method can be called only for CallbackQue	ry updates");
			_ = Telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, text, showAlert, url);
		}
	}

	public class LeftTheChatException() : Exception("The chat was left");
}
