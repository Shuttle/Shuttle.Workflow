namespace Shuttle.Workflow;

public interface ISemaphoreOptionsResolver
{
    SemaphoreOptions? Resolve(string key);
}