using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[Index(nameof(Name), IsUnique = true, Name = $"IX_{nameof(ProcessDefinition)}")]
public class ProcessDefinition
{
    [Required]
    [StringLength(512)]
    public string Description { get; set; } = null!;

    [Key]
    public Guid Id { get; set; }

    public ICollection<ProcessDefinitionMessage> Messages { get; set; } = new List<ProcessDefinitionMessage>();

    [Required]
    [StringLength(512)]
    public string Name { get; set; } = null!;

    public ICollection<ProcessDefinitionStateItem> StateItems { get; set; } = new List<ProcessDefinitionStateItem>();

    public class Specification
    {
        public Guid? Id { get; private set; }
        public int MaximumRows { get; private set; }
        public string? Name { get; private set; }

        public Specification WithId(Guid id)
        {
            Id = id;

            return this;
        }

        public Specification WithMaximumRows(int maximumRows)
        {
            MaximumRows = maximumRows < 0 ? 0 : maximumRows;
            return this;
        }

        public Specification WithName(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentNullException(nameof(name));

            return this;
        }
    }
}