namespace ChatGbtApp;

public static class SolutionDirectory
{
    public static string FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                dir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).Any())
                return dir.FullName;

            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Repo root not found (no .git or .sln in parent folders).");
    }
}