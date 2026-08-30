using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shuttle.Workflow.SqlServer.Models;

[Table("ReferenceType")]
public class ReferenceType
{
    [Key]
    public Guid Id { get; set; }

    [StringLength(130)]
    public string Name { get; set; } = null!;

    public ICollection<ReferenceItem> ReferenceItems { get; set; } = new List<ReferenceItem>();
}