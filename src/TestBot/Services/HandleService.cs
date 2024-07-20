using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using TestBot.EasyBotFramework;
using TestBot.Handlers;

namespace TestBot.Services;

public class HandleService(
	HandleAdmin handleAdmin,
	HandleUser handleUser,
	IConfiguration configuration,
	IOptions<BotConfiguration> botConfiguration)
	: EasyBot(botConfiguration.Value.BotToken, configuration)
{
	protected override async Task OnPrivateChat(Chat chat, User user, UpdateInfo update)
    {
	    Console.WriteLine(update.UserType);
	    if (update.UpdateKind != UpdateKind.NewMessage || update.MsgCategory != MsgCategory.Text)
		    return;
	    
	    if (update.Message.Text == "/admin" && update.UserType == UserType.Admin)
	    {
		    await handleAdmin.Handle(chat, user, update);
	    }
	    else if (update.Message.Text == "/start")
	    {
		    await handleUser.Handle(chat, user, update);
	    }
    }
    public Task HandleRequest()
    {
	    this.Run();
	    return Task.CompletedTask;
    }

}