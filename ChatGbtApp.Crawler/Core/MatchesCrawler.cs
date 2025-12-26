using ChatGbtApp;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Extractors.Loopcv;

namespace ChatGgtApp.Crawler.Core;

public class MatchesCrawler(Chromium chromium)
{
    private readonly List<string> EXCLUDED_JOB_TITLES = new()
    {
        "Java",
        "Python",
        "C++",
        "Fullstack",
        "Full Stack",
        "Full-Stack",
        "Frontend",
        "Front-end",
        "DevOps",
        "Lead",
        "Principal",
        "Data Engineer",
        "GCP",
        "iOS",
        "SAP",
        "QA Engineer",
        "QA Software Engineer,",
        "Test Engineer",
        "Manager",
        "React",
        "ABAP",
        "Flutter",
        "Android",
        "Quality Assurance",
        "Angular",
        "German",
        "Embedded",
        "Kubernetes",
        "Scala",
        "Drupal",
        "Middleware",
        "Ruby",
    };
    
    public async Task CrawlAsync(string matchesUrl)
    {
        var matches = await new MatchesExtractor().GetMatchUrlsAsync(matchesUrl);
            
        var filteredMatches = matches
            .Where(_ => ShouldIncludeJobTitle(_.JobTitle))
            .ToArray();

        var outputPath = SolutionDirectory.GetRepoPath(SolutionDirectory.Path_JobUrls);
        
        await File.WriteAllLinesAsync(outputPath, 
            filteredMatches
                .Select(m => $"{m.JobTitle}, {m.Url}")
            );

        Console.WriteLine($"Found filtered {filteredMatches.Count()} job titles.");
        foreach (var match in filteredMatches)
        {
            Console.WriteLine(match.JobTitle);
        }
    }


    private bool ShouldIncludeJobTitle(string jobTitle)
    {
        foreach (var excluded in EXCLUDED_JOB_TITLES)
        {
            if (jobTitle.Contains(excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        
        return true;
    }
}