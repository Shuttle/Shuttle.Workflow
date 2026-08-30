using System.Reflection;
using Microsoft.Extensions.Options;

namespace Shuttle.Workflow.RestClient;

public class WorkflowHttpMessageHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string _userAgent;
    private readonly WorkflowClientOptions _workflowClientOptions;

    public WorkflowHttpMessageHandler(IOptions<WorkflowClientOptions> workflowClientOptions, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(workflowClientOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _workflowClientOptions = workflowClientOptions.Value;
        _serviceProvider = serviceProvider;

        var version = Assembly.GetExecutingAssembly().GetName().Version;

        _userAgent = $"Shuttle.Workflow{(version != null ? $"/{version.Major}.{version.Minor}.{version.Build}" : string.Empty)}";
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Headers.Add("User-Agent", _userAgent);

        await (_workflowClientOptions.ConfigureHttpRequestAsync?.Invoke(request, _serviceProvider) ?? Task.CompletedTask);

        return await base.SendAsync(request, cancellationToken);
    }
}