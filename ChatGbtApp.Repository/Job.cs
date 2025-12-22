using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatGbtApp.Repository;

public class Job
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public DateTime DateTime { get; set; }

    public string? Title { get; set; }

    public string? Company { get; set; }

    public int? Score { get; set; }

    public string? FileLocation { get; set; }

    public string? Hash { get; set; }

    public string? JobDescription { get; set; }
}