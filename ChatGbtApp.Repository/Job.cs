using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatGbtApp.Repository;

public class Job
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public DateTime DateTime { get; set; }

    [Required] [StringLength(255)] public string Title { get; set; } = string.Empty;

    [Required] [StringLength(255)] public string Company { get; set; } = string.Empty;

    [Required] [StringLength(255)] public string JobUrl { get; set; } = string.Empty;

    public int? Score { get; set; }

    [Column(TypeName = "nvarchar(max)")] public string JobDescription { get; set; } = string.Empty;
}