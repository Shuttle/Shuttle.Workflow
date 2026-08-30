using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.Builder;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public WorkflowBuilder AddWorkflow(Action<WorkflowOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddOptions<WorkflowOptions>().Configure(configureOptions);
            services.AddSingleton<IValidateOptions<WorkflowOptions>, WorkflowOptionsValidator>();
            services.AddSingleton<ISemaphoreOptionsResolver, SemaphoreOptionsResolver>();

            return new(services);
        }
    }
}