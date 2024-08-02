using TestBot.Models;
using TestBot.Repositories;
using TestBot.VeiwModels;

namespace TestBot.Services;

public class AdminService(ITestRepository testRepository)
{
    public async Task<string> HandleNewTest(TestCreationModel dto)
    {
        dto.Answers = dto.Answers.ToLower().Trim();
        
        if (dto.ExpirationDate < DateTime.UtcNow.AddHours(5))
            return ("Test yankunlanadigan vaqt hozirgi vaqtdan avval bo'la olmaydi");
        
        if (dto.Answers.Any(char.IsDigit))
        {
            if (CreateDictionaryFromInput(dto.Answers).Count != dto.Amount)
                return ("Javoblar soni testlar soni bilan teng bolishi kerak");
        }
        else
        {
            if(dto.Answers.Length != dto.Amount)
                return ("Javoblar soni testlar soni bilan teng bolishi kerak");
        }

        var newTest = new Test()
        {
            Name = dto.Name,
            Amount = dto.Amount,
            Answers = ExtractAnswers(dto.Answers),
            CreatedAt = DateTime.UtcNow.AddHours(5),
            CreatorUser = dto.CreatorUser,
            CreatorUserId = dto.CreatorUserId,
            IsRewarded = dto.IsRewarded,
            ExpirationDate = dto.ExpirationDate
        };

        await testRepository.AddAsync(newTest);
        await testRepository.SaveAsync();

        return $"Testning ID raqami : {newTest.Id}";
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
        return $@"
    🆔 *ID :* {EscapeMarkdown(test.Id.ToString())}
        📝 *Test nomi :* {EscapeMarkdown(test.Name)}
        👤 *Tuzuvchi :* {EscapeMarkdown(test.CreatorUser)}
        🔢 *Testlar soni:* {test.Amount}
        ✅ *Javoblar :* {EscapeMarkdown(test.Answers)}
        🕒 *Yaratilgan vaqti:* {EscapeMarkdown(test.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan")}
        ⏳ *Yakunlanadigan vaqti :* {(EscapeMarkdown(test.ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"))}
        ";
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

    private string ExtractAnswers(string input)
    {
        // Remove any whitespace
        input = input.Replace(" ", "");

        // Check if the input contains numbers
        bool containsNumbers = input.Any(char.IsDigit);

        if (containsNumbers)
        {
            // If it contains numbers, extract only the letters
            return new string(input.Where(c => char.IsLetter(c)).ToArray());
        }
        else
        {
            // If it doesn't contain numbers, return the input as is
            return input;
        }
    }
}