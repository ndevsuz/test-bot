namespace TestBot.Models;

public class User
{
    public long UserId { get; set; }
    public string? Name { get; set; }
    public string? Username { get; set; }
    public DateTime CreatedOn { get; set; }
}