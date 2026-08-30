using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Shuttle.Workflow.SqlServer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<DesignTimeDbContextFactory>()
            .Build();

        var workflowSqlServerOptions = configuration.GetSection(WorkflowSqlServerOptions.SectionName).Get<WorkflowSqlServerOptions>();

        if (workflowSqlServerOptions == null)
        {
            throw new ArgumentException($"Could not find a configuration section for '{WorkflowSqlServerOptions.SectionName}'.");
        }

        var connectionString = workflowSqlServerOptions.ConnectionString;

        optionsBuilder.UseSqlServer(connectionString, builder =>
        {
            builder.MigrationsHistoryTable("__EFMigrationsHistory", "workflow");
            builder.CommandTimeout(0);
        });

        return new(optionsBuilder.Options);
    }
}