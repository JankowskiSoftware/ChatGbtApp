namespace ChatGbtApp.Tests;

public class SolutionDirectoryTests : IDisposable
{
    private readonly string _tempRootDir;
    private readonly string _originalDirectory;

    public SolutionDirectoryTests()
    {
        _originalDirectory = Directory.GetCurrentDirectory();
        _tempRootDir = Path.Combine(Path.GetTempPath(), $"SolutionDirectoryTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRootDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_originalDirectory);
        try { Directory.Delete(_tempRootDir, recursive: true); } catch { }
    }

    [Fact]
    public void FindRepoRoot_WithGitFolder_ReturnsCorrectPath()
    {
        CreateGitMarker();
        var subDir = CreateSubDir("subdir", "nested");
        
        var result = FindFromDirectory(subDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithSlnFile_ReturnsCorrectPath()
    {
        CreateSlnFile("MySolution.sln");
        var subDir = CreateSubDir("projects", "myproject");
        
        var result = FindFromDirectory(subDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithBothGitAndSln_ReturnsCorrectPath()
    {
        CreateGitMarker();
        CreateSlnFile("Solution.sln");
        var deepDir = CreateSubDir("a", "b", "c", "d");
        
        var result = FindFromDirectory(deepDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_FromRootDirectory_ReturnsRoot()
    {
        CreateGitMarker();
        
        var result = FindFromDirectory(_tempRootDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithMultipleSlnFiles_ReturnsCorrectPath()
    {
        CreateSlnFile("First.sln");
        CreateSlnFile("Second.sln");
        var subDir = CreateSubDir("src");
        
        var result = FindFromDirectory(subDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithGitInParentAndSlnInCurrent_ReturnsSlnDirectory()
    {
        CreateGitMarker();
        var subDir = CreateSubDir("subfolder");
        File.WriteAllText(Path.Combine(subDir, "Sub.sln"), "");
        
        var result = FindFromDirectory(subDir);
        
        Assert.Equal(subDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithNoGitOrSln_ThrowsDirectoryNotFoundException()
    {
        var deepDir = CreateSubDir("project", "src", "components");
        Directory.SetCurrentDirectory(deepDir);

        var exception = Assert.Throws<DirectoryNotFoundException>(() => SolutionDirectory.GetRepoPath());
        
        Assert.Contains("Repo root not found", exception.Message);
        Assert.Contains(".git", exception.Message);
        Assert.Contains(".sln", exception.Message);
    }

    [Fact]
    public void FindRepoRoot_AtDriveRoot_ThrowsWhenNoRepoMarker()
    {
        var testDir = CreateSubDir("isolated");
        Directory.SetCurrentDirectory(testDir);

        var exception = Assert.Throws<DirectoryNotFoundException>(() => SolutionDirectory.GetRepoPath());
        
        Assert.Contains("Repo root not found", exception.Message);
    }

    [Fact]
    public void FindRepoRoot_WithDeeplyNestedStructure_FindsRoot()
    {
        CreateGitMarker();
        var deepPath = _tempRootDir;
        for (int i = 0; i < 20; i++)
            deepPath = Path.Combine(deepPath, $"level{i}");
        Directory.CreateDirectory(deepPath);
        
        var result = FindFromDirectory(deepPath);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithEmptyGitFolder_StillFindsRoot()
    {
        CreateGitMarker();
        var workDir = CreateSubDir("workspace");
        
        var result = FindFromDirectory(workDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithSlnFileNameVariations_FindsRoot()
    {
        CreateSlnFile("My.Complex-Name_123.sln");
        var subDir = CreateSubDir("test");
        
        var result = FindFromDirectory(subDir);
        
        Assert.Equal(_tempRootDir, result);
    }

    [Fact]
    public void FindRepoRoot_WithGitFile_DoesNotMatch()
    {
        File.WriteAllText(Path.Combine(_tempRootDir, ".git"), "gitdir: /some/path");
        var subDir = CreateSubDir("src");
        Directory.SetCurrentDirectory(subDir);

        var exception = Assert.Throws<DirectoryNotFoundException>(() => SolutionDirectory.GetRepoPath());
        
        Assert.Contains("Repo root not found", exception.Message);
    }

    private void CreateGitMarker() => Directory.CreateDirectory(Path.Combine(_tempRootDir, ".git"));
    
    private void CreateSlnFile(string name) => File.WriteAllText(Path.Combine(_tempRootDir, name), "");
    
    private string CreateSubDir(params string[] paths)
    {
        var fullPath = Path.Combine(_tempRootDir, Path.Combine(paths));
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }
    
    private string FindFromDirectory(string dir)
    {
        Directory.SetCurrentDirectory(dir);
        return SolutionDirectory.GetRepoPath();
    }
}
