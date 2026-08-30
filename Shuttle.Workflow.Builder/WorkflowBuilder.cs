using Microsoft.Extensions.DependencyInjection;

namespace Shuttle.Workflow.Builder;

public class WorkflowBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}