using System.Text;
using AnswerBot.Repositories;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TestBot.Models;
using TestBot.Repositories;
using TestBot.Services;

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
            using var scope = _serviceScopeFactory.CreateScope();
            var testRepository = scope.ServiceProvider.GetRequiredService<ITestRepository>();
            var answerRepository = scope.ServiceProvider.GetRequiredService<IAnswerRepository>();
            var handleService = scope.ServiceProvider.GetRequiredService<HandleService>();

            var expiredTests = testRepository
                .SelectAll(x => x.ExpirationDate <= DateTime.Now && x.IsRewarded)
                .ToList();
            foreach (var test in expiredTests)
            {
                var answers = answerRepository.SelectAll(x => x.TestId == test.Id).ToList();
                var topAnswers = GetTopAnswers(answers, test.Answers);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("🏆 *Sertefikatli testni vaqti tugadi\\!*");
                messageBuilder.AppendLine($"📝 *Testning nomi:* {EscapeMarkdown(test.Name)}");
                messageBuilder.AppendLine();

                string[] places = { "🥇 1\\-chi o'rin:", "🥈 2\\-chi o'rin:", "🥉 3\\-chi o'rin:" };

                for (int i = 0; i < topAnswers.Count; i++)
                {
                    var answer = topAnswers[i];
                    messageBuilder.AppendLine(places[i]);
                    messageBuilder.AppendLine($"👤 Ismi: [{EscapeMarkdown(answer.UserName)}](tg://user?id={answer.UserId})");
                    messageBuilder.AppendLine($"📊 Foiz: {answer.Percentage}%");
                    if (i < topAnswers.Count - 1) messageBuilder.AppendLine();
                }

                await handleService.Telegram.SendTextMessageAsync(
                    test.CreatorUserId.Value,
                    messageBuilder.ToString(),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: stoppingToken
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking for expired tests.");
        }

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
}    private static int CalculatePercentage(string correctAnswers, string userAnswers)
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

    private static List<Answer> GetTopAnswers(List<Answer> answers, string correctAnswersString)
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
    
    public static string EscapeMarkdown(string text)
    {
        return text.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[").Replace("]", "\\]")
            .Replace("(", "\\(").Replace(")", "\\)").Replace("~", "\\~").Replace("`", "\\`")
            .Replace(">", "\\>").Replace("#", "\\#").Replace("+", "\\+").Replace("-", "\\-")
            .Replace("=", "\\=").Replace("|", "\\|").Replace("{", "\\{").Replace("}", "\\}")
            .Replace(".", "\\.").Replace("!", "\\!");
    }

}
