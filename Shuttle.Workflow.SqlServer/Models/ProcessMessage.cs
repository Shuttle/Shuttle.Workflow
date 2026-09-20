using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[Index(nameof(ProcessId), nameof(TypeName), nameof(SequenceNumber), IsUnique = true, Name = $"IX_{nameof(ProcessMessage)}")]
public class ProcessMessage
{
    public DateTimeOffset? DateCompleted { get; set; }
    public DateTimeOffset? DateSent { get; set; }

    [Key]
    public Guid Id { get; set; }

    public TimeSpan? InvokeTimeout { get; set; }
    public int ItemsCompleted { get; set; }
    public int? ItemsTotal { get; set; }
    public Guid ProcessId { get; set; }
    public int SequenceNumber { get; set; }

    [Required]
    [StringLength(512)]
    public string TypeName { get; set; } = null!;
}