namespace Shuttle.Workflow.Messages.v1;

public class ExpireStateItems
{
    public List<Expiry> Expiries { get; set; } = [];
    public Guid StateId { get; set; }

    public class Expiry
    {
        public DateTimeOffset EffectiveDateEnd { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}