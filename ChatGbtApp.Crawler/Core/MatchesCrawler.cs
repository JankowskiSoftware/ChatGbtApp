using ChatGbtApp;
using ChatGgtApp.Crawler.Browser;
using ChatGgtApp.Crawler.Extractors.Loopcv;

namespace ChatGgtApp.Crawler.Core;

public class MatchesCrawler(MatchesExtractor matchesExtractor)
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
        "QA",
        "Test",
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
        "Architect",
        "Staff",
        "OPS"
        
    };
    
    public async Task CrawlAsync(string matchesUrl)
    {
        var matches = await matchesExtractor.GetMatchUrlsAsync(matchesUrl);
            
        var filteredMatches = matches
            .Where(_ => ShouldIncludeJobTitle(_.JobTitle))
            .ToArray();


        var outputPath = SolutionDirectory.GetRepoPath(SolutionDirectory.Path_JobUrls);
        await File.WriteAllLinesAsync(outputPath, 
            filteredMatches
                .Select(m => $"{m.JobTitle}, {m.Url}")
        );
        
        // var json = System.Text.Json.JsonSerializer.Serialize(filteredMatches);
        // await File.WriteAllTextAsync(outputPath + ".json", json);
        //
        // var deserializedJobs = System.Text.Json.JsonSerializer.Deserialize<List<Match>>(json);

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