using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using TestBot;
using TestBot.Contexts;
using TestBot.EasyBotFramework;
using TestBot.Handlers;
using TestBot.Helpers;
using TestBot.Interfaces;
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
		services.AddScoped<IUserRepository, UserRepository>();
		services.AddTransient<IHandler, Handler>();
		services.AddTransient<AdminService>();
		services.AddTransient<UserService>();
		services.AddSingleton(new CancellationTokenSource());
		services.AddScoped<HandleNextUpdate>();
		services.AddScoped<HandleAdmin>();
		services.AddScoped<HandleUser>();

		services.AddTransient<Lazy<IHandler>>(sp => new Lazy<IHandler>(() => sp.GetRequiredService<IHandler>()));
		services.AddScoped<HandleService>();
		
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