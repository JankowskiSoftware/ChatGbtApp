using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatGbtApp.Repository;

public class JobBase
{   [property: Key]
    public required string Url { get; set; }
    public DateTime DateTime { get; set; }
    public string? Hash { get; set; }
    public string? JobDescription { get; set; }
    public string? Message { get; set; }
}

public class Job
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime DateTime { get; set; }
    public string? Hash { get; set; }
    public string? JobDescription { get; set; }
    public string? Message { get; set; }
    
    
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public int? MatchScore { get; set; }    // normalized to 0..10 (nullable if not parseable)
    public string? SeniorityFit { get; set; }   // "low", "medium", "high" (nullable if not parseable)
    public IReadOnlyList<string> MissingSkills { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingAtsKeywoards { get; set; } = Array.Empty<string>(); // NOTE: key name matches your prompt typo
    public IReadOnlyList<string> Strengths { get; set; } = Array.Empty<string>();
    public string? Recommendation { get; set; }
}