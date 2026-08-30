using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shuttle.Workflow.Builder;

namespace Shuttle.Workflow.SqlServer;

public static class WorkflowBuilderExtensions
{
    extension(WorkflowBuilder workflowBuilder)
    {
        public WorkflowBuilder UseSqlServer(Action<WorkflowSqlServerOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(workflowBuilder);

            var services = workflowBuilder.Services;

            services.AddOptions<WorkflowSqlServerOptions>().Configure(configureOptions);
            services.AddSingleton<IValidateOptions<WorkflowSqlServerOptions>, WorkflowSqlServerOptionsValidator>();

            services.AddScoped<IProcessDefinitionQuery, ProcessDefinitionQuery>();
            services.AddScoped<IProcessQuery, ProcessQuery>();
            services.AddScoped<ISemaphoreRepository, SemaphoreRepository>();
            services.AddScoped<ISemaphoreQuery, SemaphoreQuery>();
            services.AddScoped<IStateQuery, StateQuery>();

            services.AddDbContext<WorkflowDbContext>((serviceProvider, options) =>
            {
                var workflowSqlServerOptions = serviceProvider.GetRequiredService<IOptions<WorkflowSqlServerOptions>>().Value;

                options.UseSqlServer(workflowSqlServerOptions.ConnectionString, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout((int)workflowSqlServerOptions.CommandTimeout.TotalSeconds);
                });
            });

            return workflowBuilder;
        }
    }
}