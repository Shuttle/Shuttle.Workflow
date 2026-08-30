using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.RestClient;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddWorkflowClient(Action<WorkflowClientOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.AddOptions<WorkflowClientOptions>().Configure(configureOptions);

            services.AddSingleton<IValidateOptions<WorkflowClientOptions>, WorkflowClientOptionsValidator>();

            services.AddHttpClient<IWorkflowClient, WorkflowClient>("WorkflowClient", (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<WorkflowClientOptions>>().Value;

                    client.BaseAddress = options.BaseAddress;
                })
                .AddHttpMessageHandler<WorkflowHttpMessageHandler>();

            services.AddSingleton<IStateSpecificationFactory, StateSpecificationFactory>();
            services.AddSingleton<IStateConditionalCollectionFactory, StateConditionalCollectionFactory>();
            services.AddSingleton<IStateConditionalCollectionService, StateConditionalCollectionService>();

            return services;
        }
    }
}