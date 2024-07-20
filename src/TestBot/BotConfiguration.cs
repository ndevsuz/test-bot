namespace TestBot;

public class BotConfiguration
{
    public string BotToken { get; init; } = default!;
    public List<long> AdminIds { get; set; }
}
