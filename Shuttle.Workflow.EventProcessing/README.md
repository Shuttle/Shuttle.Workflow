# Shuttle.Workflow.EventProcessing

The projection layer for the event-sourced `Process`, `State`, and `ProcessDefinition` aggregates.

One handler class per aggregate lives under `v1/EventHandlers/` — `ProcessHandler`, `StateHandler`,
`ProcessDefinitionHandler` — each implementing `Shuttle.Recall`'s `IEventHandler<TEvent>` for every event the
aggregate can raise. A handler projects its events into the `Shuttle.Workflow.SqlServer` read model
(`WorkflowDbContext`) and, for the Process lifecycle milestones (`Registered`, `Completed`, `Failed`,
`Abandoned`), republishes an integration message from `Shuttle.Workflow.Messages` on the Hopper bus.

This project is registered as a set of named projections, not a NuGet-consumable library:

```c#
serviceCollection
    .AddRecall(...)
    .UseSqlServerEventStorage(...)
    .UseSqlServerEventProcessing(...)
    .AddProjection<ProcessHandler>(ProjectionNames.Process)
    .AddProjection<StateHandler>(ProjectionNames.State)
    .AddProjection<ProcessDefinitionHandler>(ProjectionNames.ProcessDefinition);
```

See `Shuttle.Workflow.SqlServer`'s README for the full write/read wiring, and `Shuttle.Workflow.Application` for
the command-side participants that raise the events these handlers project.
