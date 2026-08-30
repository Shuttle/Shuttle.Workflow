# Shuttle.Workflow.WebApi.Contracts

HTTP payload contracts for the `Shuttle.Workflow.WebApi` endpoints, under the
`Shuttle.Workflow.WebApi.Contracts.v1` namespace.

Plain mutable POCOs only — no behaviour, no dependencies. Consume them through
`Shuttle.Workflow.RestClient`, which ships the convenience extensions over these types (notably
`StateExtensions`) in this same namespace, so a single `using` picks up both.

ESB message contracts live separately in `Shuttle.Workflow.Messages`.
