# Shuttle.Workflow

A workflow/process-orchestration engine: register a `Process` against a `ProcessDefinition`, track its
outstanding messages, and let `Shuttle.Workflow.Server` drive it to completion — deferring, retrying,
flagging it overdue, or abandoning it as needed. Arbitrary key/value `State` (effective-dated, typed) can be
attached to a process or looked up independently. `Semaphore` provides simple named mutual exclusion for
consumers that need it.

`Process`, `State`, and `ProcessDefinition` are event-sourced on `Shuttle.Recall` — see
`Shuttle.Workflow.Application` (the command/write side) and `Shuttle.Workflow.EventProcessing` (the
projection/read side) for that split. `Semaphore` remains a plain EF Core-backed aggregate in
`Shuttle.Workflow.SqlServer`.

## Projects

- **Shuttle.Workflow** — the core domain: the `Process`, `State`, and `ProcessDefinition` aggregates and their
  events, `Semaphore`, and shared abstractions.
- **Shuttle.Workflow.Application** — `Shuttle.Mediator` participants that load/save the event-sourced
  aggregates via `IEventStore`.
- **Shuttle.Workflow.EventProcessing** — projects Process/State/ProcessDefinition events into the SQL Server
  read model and republishes integration notifications.
- **Shuttle.Workflow.SqlServer** — the read model (`WorkflowDbContext`, queries) and the `Semaphore`
  repository.
- **Shuttle.Workflow.Builder** — the `AddWorkflow(...)` service-registration entry point.
- **Shuttle.Workflow.Messages** — ESB message contracts (server triggers, commands, integration
  notifications).
- **Shuttle.Workflow.WebApi** — the public HTTP API.
- **Shuttle.Workflow.WebApi.Contracts** — the HTTP request/response DTOs.
- **Shuttle.Workflow.RestClient** — a typed client over the WebApi, for other .NET services.
- **Shuttle.Workflow.Extensions** — Hopper handler-context helpers for consumers driving a process from their
  own message handlers.
- **Shuttle.Workflow.Server** — the worker host: processes bus triggers, dispatches outbound process
  messages, and runs the event-processing projections out-of-process.

A Vue front-end for this service is part of `Shuttle.Portal`, alongside `Shuttle.Access` and
`Shuttle.Recall`.
