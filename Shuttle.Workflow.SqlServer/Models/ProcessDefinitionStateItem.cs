using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[PrimaryKey(nameof(ProcessDefinitionId), nameof(Name))]
public class ProcessDefinitionStateItem
{
    [Required]
    [StringLength(130)]
    public string Name { get; set; } = null!;

    public Guid ProcessDefinitionId { get; set; }

    public StateItemType Type { get; set; }
}