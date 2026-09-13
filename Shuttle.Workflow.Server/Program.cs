using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shuttle.Hopper;
using Shuttle.Hopper.AzureStorageQueues;
using Shuttle.Pigeon.RestClient;
using Shuttle.Recall;
using Shuttle.Recall.SqlServer.EventProcessing;
using Shuttle.Recall.SqlServer.Storage;
using Shuttle.Workflow.Application;
using Shuttle.Workflow.Builder;
using Shuttle.Workflow.EventProcessing.v1.EventHandlers;
using Shuttle.Workflow.SqlServer;

namespace Shuttle.Workflow.Server;

internal class Program
{
    private static async Task Main()
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        var configurationFolder = Environment.GetEnvironmentVariable("CONFIGURATION_FOLDER");

        if (string.IsNullOrEmpty(configurationFolder))
        {
            throw new ApplicationException("Environment variable `CONFIGURATION_FOLDER` has not been set.");
        }

        var appsettingsPath = Path.Combine(configurationFolder, "appsettings.json");

        if (!File.Exists(appsettingsPath))
        {
            throw new ApplicationException($"File '{appsettingsPath}' cannot be accessed/found.");
        }

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddJsonFile(appsettingsPath)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        await Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IConfiguration>(configuration);

                services
                    .AddSingleton<IConfiguration>(configuration)
                    .AddLogging(builder =>
                    {
                        builder.AddSerilog();
                    })
                    .AddWorkflow(options =>
                    {
                        configuration.GetSection(WorkflowOptions.SectionName).Bind(options);
                    })
                    .RegisterParticipants()
                    .UseSqlServer(options =>
                    {
                        configuration.GetSection(WorkflowSqlServerOptions.SectionName).Bind(options);

                        options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
                    })
                    .Services
                    .AddRecall(options =>
                    {
                        configuration.GetSection(RecallOptions.SectionName).Bind(options);
                    })
                    .UseSqlServerEventStorage(options =>
                    {
                        configuration.GetSection(SqlServerStorageOptions.SectionName).Bind(options);

                        options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
                        options.Schema = "workflow";
                    })
                    .RegisterPrimitiveEventSequencing()
                    .UseSqlServerEventProcessing(options =>
                    {
                        configuration.GetSection(SqlServerEventProcessingOptions.SectionName).Bind(options);
                    })
                    .AddProjection<ProcessHandler>(ProjectionNames.Process)
                    .AddProjection<StateHandler>(ProjectionNames.State)
                    .AddProjection<ProcessDefinitionHandler>(ProjectionNames.ProcessDefinition)
                    .Services
                    .AddHopper(options =>
                    {
                        configuration.GetSection(HopperOptions.SectionName).Bind(options);
                    })
                    .AddMessageHandlersFrom(typeof(Program).Assembly)
                    .UseAzureStorageQueues(builder =>
                    {
                        builder.Configure("azure", options =>
                        {
                            configuration.GetSection($"{AzureStorageQueueOptions.SectionName}").Bind(options);

                            if (string.IsNullOrWhiteSpace(options.StorageAccount))
                            {
                                options.ConnectionString = configuration.GetConnectionString("azure") ?? throw new ApplicationException("Missing connection string 'azure'.");
                            }
                        });
                    });

                var pigeonClientOptions = configuration.GetSection(PigeonClientOptions.SectionName).Get<PigeonClientOptions>();

                if (pigeonClientOptions?.BaseAddress != null)
                {
                    services.AddPigeonClient(options =>
                    {
                        configuration.GetSection(PigeonClientOptions.SectionName).Bind(options);
                    });
                }
            })
            .Build()
            .RunAsync();
    }
}