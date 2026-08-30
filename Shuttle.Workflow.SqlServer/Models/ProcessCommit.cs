using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[PrimaryKey(nameof(ProcessId), nameof(Key))]
public class ProcessCommit
{
    public DateTimeOffset DateCommitted { get; set; }

    [Required]
    [StringLength(1024)]
    public string Key { get; set; } = null!;

    public Guid ProcessId { get; set; }
}