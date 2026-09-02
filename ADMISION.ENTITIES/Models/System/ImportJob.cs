using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.System;

[Table("ImportJobs", Schema = "System")]
public class ImportJob
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";

    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public int FailedRows { get; set; }

    public string? ErrorMessage { get; set; }

    [StringLength(100)]
    public string? TempToken { get; set; }

    [StringLength(100)]
    public string? HangfireJobId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}
