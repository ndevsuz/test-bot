using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Helpers;
using TestBot.Repositories;

namespace TestBot.Services;

public class HandleService(
	HandleAdmin handleAdmin,
	HandleUser handleUser,
	UserService userService,
	IConfiguration configuration,
	IOptions<BotConfiguration> botConfiguration)
	: EasyBot(botConfiguration.Value.BotToken, configuration)
{
	protected override async Task OnPrivateChat(Chat chat, User user, UpdateInfo update)
    {
	    Console.WriteLine(update.UserType);
	    if (update.UpdateKind != UpdateKind.NewMessage || update.MsgCategory != MsgCategory.Text)
		    return;
	    
	    switch (update.Message.Text)
	    {
		    case "/admin" when update.UserType == UserType.Admin:
			    await handleAdmin.Handle(chat, user, update);
			    break;
		    case "/start":
		    {
			    _ = Task.Run(async () => await userService.AddUser(chat));
			    var result = await CheckMember.CheckMemberAsync(Telegram ,chat, configuration);
			    if (result)
				    await handleUser.Handle(chat, user, update);
			    else
			    {
				    var checkMsg = await Telegram.SendTextMessageAsync(chat.Id,
					    "Assalamu alaykum! Botdan foydalanish uchun, pasdagi tugmani bosib kanalga obuna bo'ling, va qaytadan /start ni bosing.", replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[]
					    {
						    new("Obuna bo'lish") { Url = "https://t.me/+t6rTFC-i3OpjOThi"}
					    }));
				    
			    }
			    break;	
		    }
	    }
    }
    public Task HandleRequest()
    {
	    this.Run();
	    return Task.CompletedTask;
    }

}