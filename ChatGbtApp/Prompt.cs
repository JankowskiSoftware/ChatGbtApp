namespace ChatGbtApp;

public class Prompt
{
    public string GetPrompt(string jobDescription)
    {
        var solutionRoot = SolutionDirectory.FindRepoRoot();
        var promptTemplate = File.ReadAllText(Path.Combine(solutionRoot, "data/prompt.txt"));
        var cv = File.ReadAllText(Path.Combine(solutionRoot, "data/cv.txt"));

        return promptTemplate
            .Replace("{{CV}}", cv)
            .Replace("{{JOB DESCROPTION}}", jobDescription);
    }
}