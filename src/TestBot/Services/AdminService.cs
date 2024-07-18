using TestBot.Models;
using TestBot.Repositories;
using TestBot.VeiwModels;

namespace TestBot.Services;

public class AdminService
{
    private readonly ITestRepository _testRepository;
    public AdminService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<long> HandleNewTest(TestCreationModel dto)
    {
        dto.Answers = dto.Answers.ToLower().Trim();
        
        if (dto.ExpirationDate > DateTime.UtcNow.AddHours(5))
            throw new Exception("Test yankunlanadigan vaqt hozirgi vaqtdan avval bo'la olmaydi");
        
        if (dto.Answers.Any(char.IsDigit))
        {
            if (CreateDictionaryFromInput(dto.Answers).Count != dto.Amount)
                throw new Exception("Javoblar soni testlar soni bilan teng bolishi kerak");
        }
        else
        {
            if(dto.Answers.Length != dto.Amount)
                throw new Exception("Javoblar soni testlar soni bilan teng bolishi kerak");
        }

        var newTest = new Test()
        {
            Amount = dto.Amount,
            Answers = dto.Answers,
            CreatedAt = DateTime.UtcNow.AddHours(5),
            CreatorUser = dto.CreatorUser,
            ExpirationDate = dto.ExpirationDate
        };

        await _testRepository.AddAsync(newTest);
        await _testRepository.SaveAsync();
        
        return newTest.Id;
    }

    public async Task<List<string>> GetAllTests()
    {
        var result = _testRepository.SelectAll().ToList();
        return ConvertTestsToStrings(result);
    }

    public async Task<bool> DeleteTest(long id)
    {
        var result = await _testRepository.DeleteAsync(x => x.Id == id);
        await _testRepository.SaveAsync();

        return result;
    }
    
    public static Dictionary<int, char> CreateDictionaryFromInput(string input)
    {
        var dictionary = new Dictionary<int, char>();
        int i = 0;

        while (i < input.Length)
        {
            int j = i;
            while (j < input.Length && char.IsDigit(input[j]))
            {
                j++;
            }
            
            if (j < input.Length && j > i && char.IsLetter(input[j]))
            {
                int key = int.Parse(input.Substring(i, j - i));
                char value = input[j];
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
    
    public static List<string> ConvertTestsToStrings(List<Test> tests)
    {
        List<string> formattedStrings = new();

        foreach (var test in tests)
        {
            string formattedString = $@"
ID : {test.Id}
Tuzuvchi : {test.CreatorUser}
Testlar soni: {test.Amount}
Javoblar : {test.Answers}
Yaratilgan vaqti: {test.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"}
Yakunlanadigan vaqti : {test.ExpirationDate?.ToString("dd/MM/yyyy HH:mm") ?? "Belgilanmagan"}
";
            formattedStrings.Add(formattedString);
        }

        return formattedStrings;
    }
}