using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Workflow.Builder;
using Shuttle.Workflow.SqlServer;

namespace Shuttle.Workflow.Tests.Data;

[TestFixture]
[Category("Data")]
public class DataFixture
{
    [SetUp]
    public void DataAccessSetup()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DataFixture>()
            .Build();

        services
            .AddWorkflow(_ =>
            {
            })
            .UseSqlServer(options =>
            {
                configuration.GetSection(WorkflowSqlServerOptions.SectionName).Bind(options);

                options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
            });

        ServiceProvider = services.BuildServiceProvider();
    }

    protected IServiceProvider? ServiceProvider;
}