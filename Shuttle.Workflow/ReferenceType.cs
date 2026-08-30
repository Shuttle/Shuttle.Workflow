namespace Shuttle.Workflow;

public class ReferenceType
{
    public ReferenceType(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        Id = id;
        Name = name;
    }

    public Guid Id { get; }
    public string Name { get; }
}