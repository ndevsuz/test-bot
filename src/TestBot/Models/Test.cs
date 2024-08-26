using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace TestBot.Models
{
    public class Test
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }

        [NotMapped]
        public Dictionary<int, string>? Answers 
        { 
            get => string.IsNullOrEmpty(AnswersJson) 
                ? null 
                : JsonSerializer.Deserialize<Dictionary<int, string>>(AnswersJson); 
            set => AnswersJson = value == null 
                ? null 
                : JsonSerializer.Serialize(value); 
        }
        
        public string? AnswersJson { get; set; }

        public string? CreatorUser { get; set; }
        public long? CreatorUserId { get; set; }
        public bool IsRewarded { get; set; } 
        public DateTime? CreatedAt { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}