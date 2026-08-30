# Shuttle.Workflow.Messages

```
PM> Install-Package Shuttle.Workflow.Messages
```

ESB message contracts for the Workflow engine, under the `Shuttle.Workflow.Messages.v1` namespace. Reference
this only to send or handle a Workflow bus message. This package deliberately carries no references at all.

The messages fall into three groups:

- **Server triggers** — internal orchestration messages consumed only by `Shuttle.Workflow.Server`:
  `SendProcessMessage` (send the next unsent message in a process), `Wait` (pause a process pending an
  external signal), `ProcessOverdue` and `ReviewProcesses` (periodic sweep of deferred/overdue processes),
  `HealthCheck` (semaphore cleanup). These carry no payload — the state they act on lives in the process's own
  `State`, addressed by correlation id.
- **Commands** — one per `Shuttle.Workflow.Application` participant (`RegisterProcess`, `DeferProcess`,
  `AbandonProcess`, `AddStateItems`, `RegisterProcessDefinition`, and so on). `Shuttle.Workflow.WebApi`
  publishes these instead of calling the participant directly whenever immediate consistency isn't configured;
  `Shuttle.Workflow.Server`'s `v1/MessageHandlers` forward each one to the matching `Shuttle.Mediator`
  participant.
- **Integration notifications** — `ProcessRegistered`, `ProcessCompleted`, `ProcessFailed`, `ProcessAbandoned`,
  published by `Shuttle.Workflow.EventProcessing`'s `ProcessHandler` after the corresponding event is
  projected, for other services to subscribe to.

HTTP payload shapes — `Process`, `State`, `Semaphore`, `RegisterProcess` and friends — live in
`Shuttle.Workflow.WebApi.Contracts` and are reached through `Shuttle.Workflow.RestClient`.
