# Shuttle.Workflow.RestClient

```
PM> Install-Package Shuttle.Workflow.RestClient
```

## Configuration

```c#
var configuration = 
    new ConfigurationBuilder()
        .AddJsonFile("appsettings.json").Build();

serviceCollection.AddWorkflowClient(options =>
{
    options.BaseAddress = new Uri("https://host/api");

    // or bind from configuration
    configuration
        .GetSection(WorkflowClientOptions.SectionName)
        .Bind(options);

    // Every outgoing request passes through 'WorkflowHttpMessageHandler', which invokes this delegate before
    // sending it -- set whatever authorization header the target Shuttle.Workflow.WebApi expects here.
    options.ConfigureHttpRequestAsync = (request, serviceProvider) =>
    {
        var tokenProvider = serviceProvider.GetRequiredService<ITokenProvider>();

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.GetToken());

        return Task.CompletedTask;
    };
})
```

The default JSON settings structure is as follows:

```json
{
  "Shuttle": {
    "Workflow": {
      "Client": {
        "BaseAddress": "__WorkflowBaseAddress__"
      }
    }
  }
}
```

`ConfigureHttpRequestAsync` can't be bound from configuration — it's a delegate, set in code as shown above. It
runs once per outgoing request, with the request message and the calling `IServiceProvider`, so it can pull
whatever credential/token service the host already has registered rather than depending on any particular
authentication package.
