using TestBot.Repositories;

namespace TestBot.Services;

public class UserService
{
    private readonly ITestRepository _testRepository;

    public UserService(ITestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    public async Task<string> HandleCheckTest(long testId, string answers)
    {
        var test = await _testRepository.SelectAsync(x => x.Id == testId);

        if (test is null || test.ExpirationDate >= DateTime.UtcNow.AddHours(5))
            return "Bunday ID bilan test mavjud emas yoki test allaqachon yakunlangan";
        
        List<char> ans = new List<char>();
        if (answers.Any(char.IsDigit))
            ans.AddRange(CreateDictionaryFromInput(answers).Values.ToList());
        else
            ans.AddRange(answers.ToCharArray());
        
        List<char> trueAns = new List<char>();
        if (test.Answers.Any(char.IsDigit))
            trueAns.AddRange(CreateDictionaryFromInput(test.Answers).Values.ToList());
        else
            trueAns.AddRange(test.Answers.ToCharArray());

        if (trueAns.Count != ans.Count)
            return $@"
Jami testlar soni {test.Amount} ta
Siz {ans.Count} ta javob yubordingiz

Iltimos javoblarni toliq yuboring";
            
        int correctAns = 0;

        for(int i = 0;i < answers.Length;i++)
        {
            if (trueAns[i] == ans[i])
                correctAns++;
        }

        string result = @$"
Jami testlar soni {test.Amount} ta
Togri berilgan javoblar soni {correctAns} ta

Test natijasi {test.Amount/100*correctAns}%
";
        
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
}