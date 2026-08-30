using System.ComponentModel.DataAnnotations;

namespace Shuttle.Workflow.SqlServer.Models;

public class ReferenceItem
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(130)]
    public string Name { get; set; } = null!;

    public Guid ReferenceTypeId { get; set; }
}