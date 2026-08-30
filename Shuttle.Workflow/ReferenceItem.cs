namespace Shuttle.Workflow;

public class ReferenceItem
{
    public ReferenceItem(Guid referenceTypeId, Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        ReferenceTypeId = referenceTypeId;
        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }
    public Guid ReferenceTypeId { get; }
}