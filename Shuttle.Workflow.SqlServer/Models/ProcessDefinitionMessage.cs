using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[Index(nameof(ProcessDefinitionId), nameof(TypeName), nameof(SequenceNumber), IsUnique = true, Name = $"IX_{nameof(ProcessDefinition)}")]
public class ProcessDefinitionMessage
{
    [Key]
    public Guid Id { get; set; }

    public TimeSpan? InvokeTimeout { get; set; }
    public Guid ProcessDefinitionId { get; set; }
    public int SequenceNumber { get; set; }

    [Required]
    [StringLength(512)]
    public string TypeName { get; set; } = null!;
}