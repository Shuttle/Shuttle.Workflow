using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shuttle.Access;
using Shuttle.Access.AspNetCore;
using Shuttle.Access.Query;
using Shuttle.Hopper;
using Shuttle.Mediator;
using Shuttle.Recall;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.WebApi;

namespace Shuttle.Workflow.Tests.Api;

public class FixtureWebApplicationFactory(Action<IWebHostBuilder>? webHostBuilder = null) : WebApplicationFactory<Program>
{
    public Mock<IBus> Bus { get; } = new();
    public Mock<WorkflowDbContext>? DbContext { get; private set; }
    public Mock<IDbContextFactory<WorkflowDbContext>> DbContextFactory { get; } = new();
    public Mock<IMediator> Mediator { get; } = new();
    public Mock<IProcessDefinitionQuery> ProcessDefinitionQuery { get; } = new();
    public Mock<IProcessQuery> ProcessQuery { get; } = new();
    public Mock<ISessionContext> SessionContext { get; } = new();

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("Shuttle.Access", $"token={Guid.NewGuid():D}");

        base.ConfigureClient(client);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        webHostBuilder?.Invoke(builder);

        var accessOptions = new AccessOptions();

        var session = new Session
        {
            IdentityId = Guid.NewGuid(),
            IdentityName = "identity-name",
            Permissions = [new() { Id = Guid.NewGuid(), Name = "*", TenantId = accessOptions.SystemTenantId }],
            DateRegistered = DateTimeOffset.UtcNow,
            ExpiryDate = DateTimeOffset.UtcNow.Add(TimeSpan.FromHours(1))
        };

        SessionContext.Setup(m => m.Session).Returns(session);
        SessionContext.Setup(m => m.IsAuthorized).Returns(true);

        DbContext = new(new DbContextOptions<WorkflowDbContext>());

        DbContextFactory.Setup(m => m.CreateDbContextAsync(CancellationToken.None)).Returns(Task.FromResult(DbContext.Object));

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IStartupFilter>(new AuthorizationStartupFilter());
            services.AddSingleton(DbContextFactory.Object);
            services.AddSingleton(Mediator.Object);
            services.AddSingleton(Bus.Object);
            services.AddSingleton(ProcessQuery.Object);
            services.AddSingleton(ProcessDefinitionQuery.Object);
            services.AddSingleton(SessionContext.Object);

            // Immediate consistency routes every write through the mocked 'IMediator' rather than the Hopper
            // bus, so the fixture never has to stand up a real event store / SQL Server instance.
            services.PostConfigure<RecallOptions>(options => options.EventProcessing.ImmediateConsistency.Enabled = true);
        });
    }
}