    using TestBot.Migrations;

    namespace TestBot.Models;

    public class Answer
    {
        public long Id { get; set; }
        public string Answers { get; set; }
        public int? Percentage { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public long TestId { get; set; }
        public Test Test { get; set; }
    }