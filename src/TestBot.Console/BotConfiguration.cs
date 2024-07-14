namespace TestBot.Console;

public class BotConfiguration
{
    public string BotToken { get; init; } = default!;
    public List<long> AdminIds { get; init; } = new();
}
