using Telegram.Bot;
using TestBot.Repositories;
using TestBot.Services;

namespace TestBot.Services;
public class TestExpirationService(
    ILogger<TestExpirationService> logger,
    ITestRepository testRepository,
    HandleService handleService)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredTests = testRepository.SelectAll(x => x.ExpirationDate > DateTime.Now);
                foreach (var test in expiredTests)
                {
                    if (test.CreatorUserId.HasValue)
                    {
                        await handleService.Telegram.SendTextMessageAsync(
                            test.CreatorUserId.Value,
                            "Your test is expired"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking for expired tests.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
