using Asp.Versioning;
using Scalar.AspNetCore;
using Serilog;
using Shuttle.Access.AspNetCore;
using Shuttle.Hopper;
using Shuttle.Hopper.AzureStorageQueues;
using Shuttle.Recall;
using Shuttle.Recall.SqlServer.EventProcessing;
using Shuttle.Recall.SqlServer.Storage;
using Shuttle.Workflow.Application;
using Shuttle.Workflow.Builder;
using Shuttle.Workflow.EventProcessing.v1.EventHandlers;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi.Converters;
using Shuttle.Workflow.WebApi.Endpoints;

namespace Shuttle.Workflow.WebApi;

public class Program
{
    public static async Task Main(string[] args)
    {
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

        var webApplicationBuilder = WebApplication.CreateBuilder(args);
        var configuration = webApplicationBuilder.Configuration;

        configuration
            .AddUserSecrets<Program>()
            .AddJsonFile(appsettingsPath)
            .AddEnvironmentVariables();

        webApplicationBuilder.Host.UseSerilog((context, loggerConfiguration) =>
            loggerConfiguration.ReadFrom.Configuration(context.Configuration));

        var apiVersion1 = new ApiVersion(1, 0);

        var services = webApplicationBuilder.Services
            .AddHopper(options =>
            {
                configuration.GetSection(HopperOptions.SectionName).Bind(options);
            })
            .UseAzureStorageQueues(builder =>
            {
                builder.Configure("azure", options =>
                {
                    configuration.GetSection(AzureStorageQueueOptions.SectionName).Bind(options);

                    if (string.IsNullOrEmpty(options.StorageAccount))
                    {
                        options.ConnectionString = configuration.GetConnectionString("azure") ?? throw new ApplicationException("Could not find a connection string with name 'azure'.");
                    }
                });
            })
            .Services
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
            .AddScoped<MessageDispatcher>()
            .AddAccessAuthorization(options =>
            {
                configuration.GetSection(AccessAuthorizationOptions.SectionName).Bind(options);
            })
            .Services;

        var immediateConsistencyEnabled = configuration.GetValue<bool>($"{RecallOptions.SectionName}:EventProcessing:ImmediateConsistency:Enabled");

        var recallBuilder = services
            .AddRecall(options =>
            {
                configuration.GetSection(RecallOptions.SectionName).Bind(options);

                if (immediateConsistencyEnabled)
                {
                    options.EventProcessing.AutoStart = false;
                }
            })
            .UseSqlServerEventStorage(options =>
            {
                configuration.GetSection(SqlServerEventProcessingOptions.SectionName).Bind(options);

                options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
                options.Schema = "workflow";
            })
            .UseSqlServerEventProcessing()
            .AddProjection<ProcessHandler>(ProjectionNames.Process)
            .AddProjection<StateHandler>(ProjectionNames.State)
            .AddProjection<ProcessDefinitionHandler>(ProjectionNames.ProcessDefinition);

        if (immediateConsistencyEnabled)
        {
            recallBuilder = recallBuilder.RegisterPrimitiveEventSequencing();
        }

        webApplicationBuilder.Services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = apiVersion1;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi(options =>
            {
                options.Document.AddSchemaTransformer((schema, _, _) =>
                {
                    schema.Title = schema.Title?.Replace("+", "_");
                    return Task.CompletedTask;
                });
            });

        webApplicationBuilder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new AssumeUtcDateTimeOffsetJsonConverter());
        });

        webApplicationBuilder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        var app = webApplicationBuilder.Build();

        app.UseCors();
        app.UseAccessAuthorization();

        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Workflow API")
                .WithTheme(ScalarTheme.DeepSpace)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        var versionSet = app.NewApiVersionSet()
            .HasApiVersion(apiVersion1)
            .ReportApiVersions()
            .Build();

        await app
            .MapProcessDefinitionEndpoints(versionSet)
            .MapProcessEndpoints(versionSet)
            .MapSemaphoreEndpoints(versionSet)
            .MapServerEndpoints(versionSet)
            .MapStateEndpoints(versionSet)
            .RunAsync();
    }
}