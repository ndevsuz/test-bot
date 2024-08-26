using System.Text;
using System.Text.RegularExpressions;
using TestBot.Models;
using TestBot.Repositories;
using TestBot.VeiwModels;

namespace TestBot.Services;

public class AdminService(ITestRepository testRepository)
{
    public async Task<string> HandleNewTest(TestCreationModel dto)
    {
        dto.Answers = dto.Answers.ToLower().Trim();
        
        var newTest = new Test()
        {
            Name = dto.Name,
            Amount = ExtractAnswers(dto.Answers).Count,
            Answers = ExtractAnswers(dto.Answers),
            CreatedAt = DateTime.UtcNow.AddHours(5),
            CreatorUser = dto.CreatorUser,
            CreatorUserId = dto.CreatorUserId,
            IsRewarded = dto.IsRewarded,
            ExpirationDate = dto.ExpirationDate
        };

        await testRepository.AddAsync(newTest);
        await testRepository.SaveAsync();

        string rewardStatus = newTest.IsRewarded ? "Ha" : "Yo'q";

        string message = $@"✅ Test muvaffaqiyatli yaratildi\!

📋 *Test ma'lumotlari:*
🆔 *Test ID:* {EscapeMarkdown(newTest.Id.ToString())}
📝 *Nomi:* {EscapeMarkdown(newTest.Name)}
🔢 *Savollar soni:* {EscapeMarkdown(newTest.Amount.ToString())}
👤 *Yaratuvchi:* {EscapeMarkdown(newTest.CreatorUser)}
🕒 *Yaratilgan vaqt:* {EscapeMarkdown(newTest.CreatedAt.ToString())}
🏆 *Sertefikatli:* {EscapeMarkdown(rewardStatus)}

Test ishlashga tayyor\!

Omad\!";

        return message;
    }

    public async Task<string?> GetById(long id, long userId)
    {
        var result = await testRepository.SelectAsync(t => t.Id == id && t.CreatorUserId == userId);
        if (result == null)
            return null;
        return await Task.FromResult(ConvertTestsToStrings(result));
    }
    
    public async Task<Test?> GetTestById(long id)
    {
        var result = await testRepository.SelectAsync(t => t.Id == id);
        return result ?? null;
    }

    /*
    public Task<List<string>> GetAllTests()
    {
        var result = testRepository.SelectAll().ToList();
        return Task.FromResult(ConvertTestsToStrings(result));
    }
    */

    public async Task<bool> DeleteTest(long id)
    {
        var result = await testRepository.DeleteAsync(x => x.Id == id);
        await testRepository.SaveAsync();

        return result;
    }

    private static Dictionary<int, char> CreateDictionaryFromInput(string input)
    {
        var dictionary = new Dictionary<int, char>();
        var i = 0;

        while (i < input.Length)
        {
            var j = i;
            while (j < input.Length && char.IsDigit(input[j]))
            {
                j++;
            }
            
            if (j < input.Length && j > i && char.IsLetter(input[j]))
            {
                var key = int.Parse(input.Substring(i, j - i));
                var value = input[j];
                dictionary[key] = value;
                i = j + 1;
            }
            else
            {
                break;
            }
        }

        return dictionary;
    }

    private static string ConvertTestsToStrings(Test test)
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"🆔 *ID :* {EscapeMarkdown(test.Id.ToString())}");
        sb.AppendLine($@"📝 *Test nomi :* {EscapeMarkdown(test.Name)}");
        sb.AppendLine($@"👤 *Tuzuvchi :* [{EscapeMarkdown(test.CreatorUser)}](tg://user?id={test.CreatorUserId})");
        sb.AppendLine($@"🔢 *Testlar soni:* {test.Amount}");

        if (test.Answers != null && test.Answers.Count > 0)
        {
            sb.AppendLine("✅ *Javoblar :*");
            foreach (var answer in test.Answers)
            {
                sb.AppendLine($@"{EscapeMarkdown(answer.Key.ToString())}\. {EscapeMarkdown(answer.Value)}");
            }
        }
        else
        {
            sb.AppendLine("✅ *Javoblar :* Belgilanmagan");
        }

        sb.AppendLine($@"🕒 *Yaratilgan vaqti:* {EscapeMarkdown(test.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan")}");
        sb.AppendLine($@"⏳ *Yakunlanadigan vaqti :* {(EscapeMarkdown(test.ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"))}");

        return sb.ToString();
    }
    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string[] specialCharacters = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        foreach (var character in specialCharacters)
        {
            text = text.Replace(character, "\\" + character);
        }
        return text;
    }

    private Dictionary<int, string> ExtractAnswers(string answers)
    {
        var answerDict = new Dictionary<int, string>();

        // Check if the string contains any numbers (keys)
        bool isKeyed = answers.Any(char.IsDigit);

        if (isKeyed)
        {
            // If the answers are keyed (e.g., "1a2b3c"), split by numbers and populate the dictionary
            int key = 0;
            foreach (var part in Regex.Split(answers, @"(?<=\D)(?=\d)|(?<=\d)(?=\D)"))
            {
                if (int.TryParse(part, out int number))
                {
                    key = number;
                }
                else
                {
                    answerDict[key] = part;
                }
            }
        }
        else
        {
            // If the answers are not keyed (e.g., "abcdabcd"), use the position as the key
            for (int i = 0; i < answers.Length; i++)
            {
                answerDict[i + 1] = answers[i].ToString();
            }
        }

        return answerDict;
    }
}