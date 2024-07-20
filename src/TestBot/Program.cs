using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TestBot;
using TestBot.Contexts;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Repositories;
using TestBot.Services;


IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((context, services) =>
	{
		services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));

		services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
			.AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
			{
				BotConfiguration? botConfiguration = sp.GetService<IOptions<BotConfiguration>>()?.Value;
				ArgumentNullException.ThrowIfNull(botConfiguration);
				TelegramBotClientOptions options = new(botConfiguration.BotToken);
				return new TelegramBotClient(options, httpClient);
			});
		services.AddSingleton(context.Configuration);
		services.AddScoped<ITestRepository, TestRepository>();
		services.AddScoped<AdminService>();
		services.AddScoped<HandleService>();
		services.AddSingleton(new CancellationTokenSource());
		services.AddScoped<HandleNextMessage>();
		services.AddScoped<HandleAdmin>();
		services.AddScoped<HandleUser>();
		services.AddDbContext<AppDbContext>(options =>
			options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));	

	})
	.Build();

using (var scope = host.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var handleService = services.GetRequiredService<HandleService>();
	await handleService.HandleRequest();
}
	
await host.RunAsync();