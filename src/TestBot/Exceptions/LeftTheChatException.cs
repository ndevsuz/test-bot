namespace TestBot.Exceptions;

public class LeftTheChatException : Exception
{
    public LeftTheChatException() : base("The chat was left") { }
}
