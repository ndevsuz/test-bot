    using System.ComponentModel.DataAnnotations.Schema;
    using Newtonsoft.Json;
    using TestBot.Migrations;

    namespace TestBot.Models;
    public class Answer
    {
        public long Id { get; set; }
        public string Answers { get; set; }  // Store as JSON
        public int? Percentage { get; set; }
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public long TestId { get; set; }
        public Test Test { get; set; }

        [NotMapped]
        public Dictionary<int, string> AnswersDictionary
        {
            get => JsonConvert.DeserializeObject<Dictionary<int, string>>(Answers);
            set => Answers = JsonConvert.SerializeObject(value);
        }
    }
