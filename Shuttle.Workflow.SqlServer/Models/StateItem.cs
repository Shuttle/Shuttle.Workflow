using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shuttle.Workflow.SqlServer.Models;

[PrimaryKey(nameof(StateId), nameof(Name), nameof(EffectiveDate))]
public class StateItem
{
    public bool? BooleanValue { get; set; }

    public DateTimeOffset DateRegistered { get; set; }

    public DateTimeOffset? DateTimeValue { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? DecimalValue { get; set; }

    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    ///     Exclusive end of the interval over which this item applies.  <see cref="DateTimeOffset.MaxValue" />
    ///     means "still in effect"; an earlier value means the item expired, regardless of whether a replacement followed it.
    /// </summary>
    public DateTimeOffset EffectiveDateEnd { get; set; } = DateTimeOffset.MaxValue;

    public Guid? GuidValue { get; set; }

    [Required]
    [StringLength(130)]
    public string Name { get; set; } = null!;

    [ForeignKey(nameof(StateId))]
    public State State { get; set; } = null!;

    public Guid StateId { get; set; }

    [MaxLength(int.MaxValue)]
    public string? StringValue { get; set; } = null!;

    public StateItemType Type { get; set; }
}