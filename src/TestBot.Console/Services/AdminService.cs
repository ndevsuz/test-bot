using System;
using System.Globalization;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using TestBot.Console.Models;
using TestBot.Console.Repositories;

namespace TestBot.Console.Services
{
    public class AdminService
    {
        private readonly ITestRepository _testRepository;
        private readonly ILogger<AdminService> _logger;
        private readonly ITelegramBotClient _bot;

        public AdminService(ITelegramBotClient bot, ITestRepository testRepository, ILogger<AdminService> logger)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _testRepository = testRepository ?? throw new ArgumentNullException(nameof(testRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Message> HandleYangiTest(Message msg)
        {
            long testId = 0;
            try
            {
                // Step 1: Ask for the name of the test
                var nameMessage = await _bot.SendTextMessageAsync(msg.Chat.Id, "Please enter the name of the test:");
                var creator = nameMessage.Text;

                // Step 2: Ask for the number of answers
                var answersCountMessage = await _bot.SendTextMessageAsync(msg.Chat.Id, "Please enter the number of answers (e.g., 4):");
                if (!int.TryParse(answersCountMessage.Text, out int answersCount) || answersCount <= 0)
                {
                    return await _bot.SendTextMessageAsync(msg.Chat.Id,
                        "Invalid number of answers. Please enter a valid positive integer.");
                }

                // Step 3: Ask for the answers in the specific format
                var answersMessage = await _bot.SendTextMessageAsync(msg.Chat.Id,
                    $"Please enter {answersCount} answers in the format 'abcd...':");
                var answers = answersMessage.Text;

                // Validate answers format and length
                if (answers.Length != answersCount)
                {
                    return await _bot.SendTextMessageAsync(msg.Chat.Id,
                        $"Invalid answers length. Expected {answersCount} characters.");
                }

                // Step 4: Ask for the expiration date
                var expirationDateMessage = await _bot.SendTextMessageAsync(msg.Chat.Id,
                    "Please enter the expiration date in the format 'dd/mm/yyyy:HH/mm':");
                var expirationDateInput = expirationDateMessage.Text;

                // Parse expiration date
                if (!DateTime.TryParseExact(expirationDateInput, "dd/MM/yyyy:HH/mm", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime expirationDate))
                {
                    return await _bot.SendTextMessageAsync(msg.Chat.Id,
                        "Invalid date format. Please enter the expiration date in the format 'dd/mm/yyyy:HH/mm'.");
                }

                // Create a new Test object
                var newTest = new Test
                {
                    Amount = answersCount,
                    Answers = answers,
                    CreatorUser = creator,
                    CreatedAt = DateTime.UtcNow.AddHours(5), // Adjusted to your time zone
                    ExpirationDate = expirationDate,
                };

                // Save the test to the database
                var createdTest = await _testRepository.AddAsync(newTest);
                await _testRepository.SaveAsync();

                testId = createdTest.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Yangi test: {Message}", ex.Message);
            }

            return await _bot.SendTextMessageAsync(msg.Chat.Id, $"Test created successfully! Test ID: {testId}");
        }

        private async Task<Message> WaitForTextMessageAsync(long chatId)
        {
            while (true)
            {
                try
                {
                    var updates = await _bot.GetUpdatesAsync();

                    foreach (var update in updates)
                    {
                        var message = update.Message;
                        if (message != null && message.Type == Telegram.Bot.Types.Enums.MessageType.Text &&
                            message.Chat.Id == chatId)
                        {
                            return message;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exceptions gracefully (e.g., logging)
                }

                await Task.Delay(1000); // Wait before checking updates again
            }
        }
    }   
}
