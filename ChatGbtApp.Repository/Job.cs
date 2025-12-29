using System.ComponentModel.DataAnnotations;

namespace ChatGbtApp.Repository;

public class JobBase
{   [property: Key]
    public required string Url { get; set; }
    public DateTime DateTime { get; set; } = DateTime.UtcNow;
    public string? JobDescription { get; set; }
    public string? Message { get; set; }
}

public class Job : JobBase
{
    public string? Url2 { get; set; }
    public string? JobTitle { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    
    public string? Remote { get; set; }
    public int? IsDistributed { get; set; }
    public string? ContractType { get; set; }
    public string? Seniority { get; set; }   // "low", "medium", "high" (nullable if not parseable)
    public string? Currency { get; set; }
    public string? HourlyMin { get; set; }
    public string? HourlyMax { get; set; }
    public string? SalaryOriginalText { get; set; }
    public string? DeliveryPressureScore { get; set; }
    public string? TechKeywords { get; set; }
    public string? Confidence { get; set; }
    public int Score { get; set; }
    public string? Notes { get; set; }
    
    public bool Rejected { get; set; } = false;
    public bool Marked { get; set; } = false;
    public bool Applied { get; set; } = false;
}
