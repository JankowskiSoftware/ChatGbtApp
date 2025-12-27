namespace ChatGbtApp;

public class Prompt
{
    public string GetPrompt(string promptName, string jobDescription)
    {
        var promptPath = SolutionDirectory.GetRepoPath($"data/{promptName}.txt");
        var promptTemplate = File.ReadAllText(promptPath);

        return promptTemplate
            .Replace("{{JOB DESCROPTION}}", jobDescription);
    }
}