namespace TestBot.VeiwModels;

public class TestCreationModel
{
    public int Amount { get; set; }
    public string? Answers { get; set; }
    public string? CreatorUser { get; set; }
    public long? CreatorUserId { get; set; }
    public bool IsRewarded { get; set; }
    public DateTime? ExpirationDate { get; set; }
}