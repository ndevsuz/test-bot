using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TestBot.Repositories;
using TestBot.Services;

namespace TestBot.Services;

public class TestExpirationService : BackgroundService
{
    private readonly ILogger<TestExpirationService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TestExpirationService(
        ILogger<TestExpirationService> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var testRepository = scope.ServiceProvider.GetRequiredService<ITestRepository>();
                    var handleService = scope.ServiceProvider.GetRequiredService<HandleService>();

                    var expiredTests = testRepository.SelectAll(x => x.ExpirationDate <= DateTime.Now);
                    foreach (var test in expiredTests)
                    {
                        if (test.CreatorUserId.HasValue)
                        {
                            await handleService.Telegram.SendTextMessageAsync(
                                test.CreatorUserId.Value,
                                "Your test has expired",
                                cancellationToken: stoppingToken
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking for expired tests.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}