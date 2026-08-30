using Microsoft.EntityFrameworkCore;
using Shuttle.Workflow.SqlServer.Models;
using Shuttle.Workflow.SqlServer.ValueConverters;

namespace Shuttle.Workflow.SqlServer;

public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<ProcessCommit> ProcessCommits { get; set; } = null!;
    public DbSet<ProcessDefinitionMessage> ProcessDefinitionMessages { get; set; } = null!;
    public DbSet<Models.ProcessDefinition> ProcessDefinitions { get; set; } = null!;
    public DbSet<ProcessDefinitionStateItem> ProcessDefinitionStateItems { get; set; } = null!;
    public DbSet<Models.Process> Processes { get; set; } = null!;
    public DbSet<ProcessMessage> ProcessMessages { get; set; } = null!;
    public DbSet<Models.ReferenceItem> ReferenceItems { get; set; } = null!;
    public DbSet<Models.ReferenceType> ReferenceTypes { get; set; } = null!;
    public DbSet<Models.Semaphore> Semaphores { get; set; } = null!;
    public DbSet<StateItem> StateItems { get; set; } = null!;
    public DbSet<Models.State> States { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<TimeSpan?>()
            .HaveConversion<NullableTimeSpanStringValueConverter>();

        configurationBuilder
            .Properties<TimeSpan>()
            .HaveConversion<TimeSpanStringValueConverter>();

        // No 'DateTime' conversion is registered: every date is a 'DateTimeOffset' mapped to 'datetimeoffset',
        // which carries its own offset, so there is no 'DateTimeKind' to re-assert on materialisation.
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workflow");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.DisplayName());
        }

        modelBuilder.Entity<Models.Process>()
            .HasIndex(p => new { p.Key, p.DateCompleted })
            .HasDatabaseName("IX_Process_Key_DateCompleted")
            .HasFilter("[Key] IS NOT NULL");

        modelBuilder.Entity<Models.Process>()
            .HasIndex(p => new { p.Key, p.Status })
            .HasDatabaseName("IX_Process_Key_Status")
            .HasFilter("[Key] IS NOT NULL");

        modelBuilder.Entity<Models.Process>()
            .HasMany(e => e.Messages);

        modelBuilder.Entity<Models.ProcessDefinition>()
            .HasMany(e => e.StateItems);

        modelBuilder.Entity<Models.ProcessDefinition>()
            .HasMany(e => e.Messages);

        modelBuilder.Entity<Models.ReferenceType>()
            .HasMany(e => e.ReferenceItems);

        modelBuilder.Entity<Models.State>()
            .HasMany(e => e.Items);

        modelBuilder.Entity<StateItem>()
            .Property(e => e.EffectiveDate)
            .HasDefaultValue(DateTimeOffset.UnixEpoch);

        modelBuilder.Entity<StateItem>()
            .Property(e => e.EffectiveDateEnd)
            .HasDefaultValue(DateTimeOffset.MaxValue);

        modelBuilder.Entity<StateItem>()
            .Property(e => e.DateRegistered)
            // 'GETUTCDATE()' returns a 'datetime' and cannot default a 'datetimeoffset' column; this keeps the
            // stored offset at zero rather than picking up the server's.
            .HasDefaultValueSql("TODATETIMEOFFSET(SYSUTCDATETIME(), 0)");

        modelBuilder
            .Entity<StateItem>()
            .HasOne(o => o.State)
            .WithMany(m => m.Items)
            .HasForeignKey(fk => fk.StateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}