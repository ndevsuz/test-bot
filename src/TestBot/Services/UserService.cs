using Telegram.Bot.Types;
using TestBot.Repositories;

namespace TestBot.Services;

public class UserService(IUserRepository userRepository)
{
    public async Task AddUser(Chat chat)
    {
        try
        {
            var user = await userRepository.SelectAsync(u => u.UserId == chat.Id);

            if (user != null)
                return;

            var userModel = new Models.User
            {
                UserId = chat.Id,
                Name = $"{chat.FirstName ?? string.Empty} {chat.LastName ?? string.Empty}",
                Username = chat.Username ?? string.Empty,
                CreatedOn = DateTime.UtcNow // Ensure this is in UTC
            };

            await userRepository.AddAsync(userModel);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            // ignored
        }
    }
}