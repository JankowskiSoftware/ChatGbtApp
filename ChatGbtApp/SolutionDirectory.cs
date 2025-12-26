namespace ChatGbtApp;

public static class SolutionDirectory
{
    public const string Path_JobUrls = "data/job-urls.txt";
    
    
    public static string GetRepoPath()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                dir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any())
                return dir.FullName;

            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Repo root not found (no .git or .sln in parent folders).");
    }

    public static string GetRepoPath(string relativePath)
    {
        return Path.Combine(GetRepoPath(), relativePath); 
    }
}