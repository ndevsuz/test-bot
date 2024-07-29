namespace TestBot.Models;

public class Test
{
    public long Id { get; set; }
    public int Amount { get; set; }
    public string? Answers { get; set; }
    public string? CreatorUser { get; set; }
    public long? CreatorUserId { get; set; }
    public bool IsRewarded { get; set; } 
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpirationDate { get; set; }
}