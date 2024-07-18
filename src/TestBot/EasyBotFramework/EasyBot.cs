using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TestBot.Services;

namespace TestBot.EasyBotFramework
{
	public class EasyBot	// A fun way to code Telegram Bots, by Wizou
	{
		protected readonly TelegramBotClient Telegram;
		protected IConfiguration _configuration;
		protected readonly AdminService _adminService;
		private User Me { get; set; }
		protected string BotName => Me.Username;

		private int _lastUpdateId = -1;
		private readonly CancellationTokenSource _cancel = new();
		private readonly Dictionary<long, TaskInfo> _tasks = new();

		protected virtual Task OnPrivateChat(Chat chat, User user, UpdateInfo update) => Task.CompletedTask;
		protected virtual Task OnGroupChat(Chat chat, UpdateInfo update) => Task.CompletedTask;
		protected virtual Task OnChannel(Chat channel, UpdateInfo update) => Task.CompletedTask;
		protected virtual Task OnOtherEvents(UpdateInfo update) => Task.CompletedTask;

		protected EasyBot(string botToken, IConfiguration configuration, AdminService adminService)
		{
			Telegram = new(botToken);
			Me = Task.Run(() => Telegram.GetMeAsync()).Result;
			_configuration = configuration;
			_adminService = adminService;
		}

		public void Run() => RunAsync().Wait();

		private async Task RunAsync()
		{
			System.Console.WriteLine("Press Escape to stop the bot");
			while (true)
			{
				var updates = await Telegram.GetUpdatesAsync(_lastUpdateId + 1, timeout: 2);
				foreach (var update in updates)
					HandleUpdate(update);
				if (Console.KeyAvailable)
					if (Console.ReadKey().Key == ConsoleKey.Escape)
						break;
			}
			await _cancel.CancelAsync();
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
			RunTask(taskInfo, updateInfo, chat);
		}

		private void RunTask(TaskInfo taskInfo, UpdateInfo updateInfo, Chat chat)
		{
			Func<Task> taskStarter = (chat?.Type) switch
			{
				ChatType.Private => () => OnPrivateChat(chat, updateInfo.Message?.From, updateInfo),
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
				RunTask(taskInfo, newUpdate, chat);
			});
		}

		public async Task<UpdateKind> NextEvent(UpdateInfo update, CancellationToken ct = default)
		{
			using var bothCT = CancellationTokenSource.CreateLinkedTokenSource(ct, _cancel.Token);
			var newUpdate = await ((IGetNext)update).NextUpdate(bothCT.Token);
			update.Message = newUpdate.Message;
			update.CallbackData = newUpdate.CallbackData;
			update.Update = newUpdate.Update;
			return update.UpdateKind = newUpdate.UpdateKind;
		}

		public async Task<string> ButtonClicked(UpdateInfo update, Message msg = null, CancellationToken ct = default)
		{
			while (true)
			{
				switch (await NextEvent(update, ct))
				{
					case UpdateKind.CallbackQuery:
						if (msg != null && update.Message.MessageId != msg.MessageId)
							_ = Telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, null, cancellationToken: ct);
						else
							return update.CallbackData;
						continue;
					case UpdateKind.OtherUpdate
						when update.Update.MyChatMember is ChatMemberUpdated
						{ NewChatMember.Status: ChatMemberStatus.Left or ChatMemberStatus.Kicked }:
						throw new LeftTheChatException(); // abort the calling method
				}
			}
		}

		public async Task<MsgCategory> NewMessage(UpdateInfo update, CancellationToken ct = default)
		{
			while (true)
			{
				switch (await NextEvent(update, ct))
				{
					case UpdateKind.NewMessage
						when update.MsgCategory is MsgCategory.Text or MsgCategory.MediaOrDoc or MsgCategory.StickerOrDice:
							return update.MsgCategory; // NewMessage only returns for messages from these 3 categories
					case UpdateKind.CallbackQuery:
						_ = Telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, null, cancellationToken: ct);
						continue;
					case UpdateKind.OtherUpdate
						when update.Update.MyChatMember is ChatMemberUpdated
						{ NewChatMember.Status: ChatMemberStatus.Left or ChatMemberStatus.Kicked }:
							throw new LeftTheChatException(); // abort the calling method
				}
			}
		}

		public async Task<string> NewTextMessage(UpdateInfo update, CancellationToken ct = default)
		{
			while (await NewMessage(update, ct) != MsgCategory.Text) { }
			return update.Message.Text;
		}

		public void ReplyCallback(UpdateInfo update, string text = null, bool showAlert = false, string url = null)
		{
			if (update.Update.Type != UpdateType.CallbackQuery)
				throw new InvalidOperationException("This method can be called only for CallbackQuery updates");
			_ = Telegram.AnswerCallbackQueryAsync(update.Update.CallbackQuery.Id, text, showAlert, url);
		}
	}

	public class LeftTheChatException : Exception
	{
		public LeftTheChatException() : base("The chat was left") { }
	}
}
