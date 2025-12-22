using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatGbtApp.Repository;

public class JobBase
{   [property: Key]
    public required string Url { get; set; }
    public DateTime DateTime { get; set; }
    public string? JobDescription { get; set; }
    public string? Message { get; set; }
}

public class Job
{
    [Key]
    public required string Url { get; set; }
    public DateTime DateTime { get; set; }
    
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    
    public int? MatchScore { get; set; }    // normalized to 0..10 (nullable if not parseable)
    public string? Remote { get; set; }
    public string? Frontend { get; set; }
    public string? DotNetRole { get; set; }
    public string? SeniorityFit { get; set; }   // "low", "medium", "high" (nullable if not parseable)
    public string? Summary { get; set; }
    public string? Recommendation { get; set; }
    
    public IReadOnlyList<string> MissingSkills { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingAtsKeywoards { get; set; } = Array.Empty<string>(); // NOTE: key name matches your prompt typo
    public IReadOnlyList<string> Strengths { get; set; } = Array.Empty<string>();
    
    public string? JobDescription { get; set; }
    public string? Message { get; set; }

    public bool Marked { get; set; } = false;
    public bool Applied { get; set; } = false;
}