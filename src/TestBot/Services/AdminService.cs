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
            Amount = dto.Amount,
            Answers = dto.Answers,
            CreatedAt = DateTime.UtcNow.AddHours(5),
            CreatorUser = dto.CreatorUser,
            ExpirationDate = dto.ExpirationDate
        };

        await testRepository.AddAsync(newTest);
        await testRepository.SaveAsync();

        return $"Testning ID raqami : {newTest.Id}";
    }

    public async Task<string?> GetById(long id)
    {
        var result = await testRepository.SelectAsync(t => t.Id == id);
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
            ID : {test.Id}
            Tuzuvchi : {test.CreatorUser}
            Testlar soni: {test.Amount}
            Javoblar : {test.Answers}
            Yaratilgan vaqti: {test.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"}
            Yakunlanadigan vaqti : {test.ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"}
            ";
    }
}