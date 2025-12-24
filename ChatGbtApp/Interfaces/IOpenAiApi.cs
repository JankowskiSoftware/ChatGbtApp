namespace ChatGbtApp.Interfaces;

public interface IOpenAiApi
{
    Task<string> AskAsync(string input, string model = "gpt-4.1-mini");
}
