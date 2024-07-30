using AnswerBot.Repositories;
using Telegram.Bot;
using TestBot.Models;
using TestBot.Repositories;
using TestBot.Services;

public class TestExpirationService : BackgroundService
{
    private readonly ILogger<TestExpirationService> _logger;
    private readonly ITestRepository _testRepository;
    private readonly IAnswerRepository _answerRepository;
    private readonly HandleService _handleService; 

    public TestExpirationService(
        ILogger<TestExpirationService> logger,
        ITestRepository testRepository,
        HandleService handleService, IAnswerRepository answerRepository)
    {
        _logger = logger;
        _testRepository = testRepository;
        _handleService = handleService;
        _answerRepository = answerRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredTests = _testRepository.SelectAll(x => x.ExpirationDate > DateTime.Now).ToList();
                foreach (var test in expiredTests)
                {
                    if (test.IsRewarded is true && test.CreatorUserId.HasValue)
                    {
                        var answers = _answerRepository.SelectAll(x => x.TestId == test.Id).ToList();
                        answers = GetTopAnswers(answers, test.Answers);

                        foreach (var answer in answers)
                        {   
                            
                            await _handleService.Telegram.SendTextMessageAsync(
                                answer.UserId,
                                $"Testni onasini emdirib tashabsiz natijez : {answer.Percentage}"
                            );
                            
                            await _handleService.Telegram.SendTextMessageAsync(
                                test.CreatorUserId.Value,
                                $"mano user sizi teztizi tikib qoyibdi {answer.UserId} uni natijasi {answer.Percentage}"
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
    
    private static int CalculatePercentage(string correctAnswers, string userAnswers)
    {
        if (correctAnswers.Length != userAnswers.Length)
        {
            throw new ArgumentException("The length of correct answers and user answers must be the same.");
        }

        int correctCount = 0;

        for (int i = 0; i < correctAnswers.Length; i++)
        {
            if (correctAnswers[i] == userAnswers[i])
            {
                correctCount++;
            }
        }

        return (int)((double)correctCount / correctAnswers.Length * 100);
    }

    public static List<Answer> GetTopAnswers(List<Answer> answers, string correctAnswersString)
    {
        var result = answers.Select(answer => new
            {
                Answer = answer,
                Percentage = CalculatePercentage(correctAnswersString, answer.Answers)
            })
            .ToList();

        var perfectAnswers = result.Where(r => r.Percentage == 100).Select(r => r.Answer).ToList();

        if (perfectAnswers.Count() > 2)
        {
            return perfectAnswers;
        }

        return result.OrderByDescending(r => r.Percentage)
            .Take(3)
            .Select(r => r.Answer)
            .ToList();
    }
}
