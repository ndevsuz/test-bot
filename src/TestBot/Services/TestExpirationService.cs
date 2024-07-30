using Telegram.Bot;
using TestBot.Repositories;
using TestBot.Services;

public class TestExpirationService : BackgroundService
{
    private readonly ILogger<TestExpirationService> _logger;
    private readonly ITestRepository _testRepository; 
    private readonly HandleService _handleService; 

    public TestExpirationService(
        ILogger<TestExpirationService> logger,
        ITestRepository testRepository,
        HandleService handleService)
    {
        _logger = logger;
        _testRepository = testRepository;
        _handleService = handleService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredTests = _testRepository.SelectAll(x => x.ExpirationDate > DateTime.Now);
                foreach (var test in expiredTests)
                {
                    if (test.CreatorUserId.HasValue)
                    {
                        await _handleService.Telegram.SendTextMessageAsync(
                            test.CreatorUserId.Value,
                            "Your test is expired"
                        );
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
