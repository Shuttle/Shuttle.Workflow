namespace Shuttle.Workflow.SqlServer.Models;

public class Specification<T> where T : class
{
    private readonly List<Guid> _excludedIds = [];
    private readonly List<Guid> _ids = [];
    public IEnumerable<Guid> ExcludedIds => _excludedIds.AsReadOnly();
    public bool HasExcludedIds => _excludedIds.Any();

    public bool HasIds => _ids.Any();
    public IEnumerable<Guid> Ids => _ids.AsReadOnly();
    public int MaximumRows { get; private set; }

    public T AddExcludedId(Guid id)
    {
        if (!_excludedIds.Contains(id))
        {
            _excludedIds.Add(id);
        }

        return GetTypedSpecification();
    }

    public T AddExcludedIds(IEnumerable<Guid> ids)
    {
        foreach (var id in ids)
        {
            AddExcludedId(id);
        }

        return GetTypedSpecification();
    }

    public T AddId(Guid id)
    {
        if (!_ids.Contains(id))
        {
            _ids.Add(id);
        }

        return GetTypedSpecification();
    }

    public T AddIds(IEnumerable<Guid> ids)
    {
        foreach (var id in ids)
        {
            AddId(id);
        }

        return GetTypedSpecification();
    }

    private T GetTypedSpecification()
    {
        return this as T ?? throw new ApplicationException($"Could not cast instance of type '{GetType().FullName}' to type '{typeof(T).FullName}'");
    }

    public T WithMaximumRows(int maximumRows)
    {
        if (MaximumRows == 0 || maximumRows < MaximumRows)
        {
            MaximumRows = maximumRows;
        }

        return GetTypedSpecification();
    }
}