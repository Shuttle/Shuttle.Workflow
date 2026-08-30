# Shuttle.Workflow.SqlServer

```
PM> Install-Package Shuttle.Workflow.SqlServer
```

This project owns the SQL Server **read model** only — the `WorkflowDbContext` schema, and the
`IProcessQuery`/`IStateQuery`/`IProcessDefinitionQuery`/`ISemaphoreQuery` implementations that answer
reads. `Process`, `State`, and `ProcessDefinition` are event-sourced (see `Shuttle.Workflow.Application` and
`Shuttle.Workflow.EventProcessing`); `Semaphore` is the one aggregate still written here directly, via
`ISemaphoreRepository`.

## Configuration

```c#
var configuration = 
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json").Build();

serviceCollection
    .AddWorkflow(options => { configuration.GetSection(WorkflowOptions.SectionName).Bind(options); })
    .UseSqlServer(options =>
    {
        configuration.GetSection(WorkflowSqlServerOptions.SectionName).Bind(options);

        options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
    });
```

`AddWorkflow` comes from `Shuttle.Workflow.Builder`. `UseSqlServer` registers the read-model `WorkflowDbContext`
and the query/`Semaphore` services above — it is **not** sufficient on its own to process commands. A host that
registers and processes Process/State/ProcessDefinition commands (`Shuttle.Workflow.WebApi` and
`Shuttle.Workflow.Server` both do) also needs the `Shuttle.Recall` event store wired up alongside it:

```c#
serviceCollection
    .AddRecall(options => { configuration.GetSection(RecallOptions.SectionName).Bind(options); })
    .UseSqlServerEventStorage(options =>
    {
        configuration.GetSection(SqlServerStorageOptions.SectionName).Bind(options);

        options.ConnectionString = configuration.GetConnectionString("Workflow") ?? throw new ApplicationException("Missing connection string 'Workflow'.");
        options.Schema = "workflow";
    })
    .RegisterPrimitiveEventSequencing()
    .UseSqlServerEventProcessing(options => { configuration.GetSection(SqlServerEventProcessingOptions.SectionName).Bind(options); })
    .AddProjection<ProcessHandler>(ProjectionNames.Process)
    .AddProjection<StateHandler>(ProjectionNames.State)
    .AddProjection<ProcessDefinitionHandler>(ProjectionNames.ProcessDefinition);
```

The event store and the read model share the same `Workflow` connection string, under separate schemas
(`workflow` for the event store, the EF Core default schema for the read model) — see
`Shuttle.Workflow.EventProcessing` for the handlers that keep the read model in sync with the event stream.

The `WorkflowDbContext` makes use of a connection string named `Workflow` in the configuration file:

```json
{
  "ConnectionStrings": {
    "Workflow": "Server=.;Database=Workflow;User Id={user-id};Password={password};TrustServerCertificate=true"
  }
}
```
