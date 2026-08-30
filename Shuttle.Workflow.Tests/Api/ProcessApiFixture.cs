using System.Net;
using System.Net.Http.Json;
using Moq;
using NUnit.Framework;
using Shuttle.Hopper;
using Shuttle.Workflow.Application;
using Shuttle.Workflow.Messages.v1;
using Shuttle.Workflow.WebApi.Contracts.v1;
using ContinueProcess = Shuttle.Workflow.Application.ContinueProcess;
using RegisterProcess = Shuttle.Workflow.WebApi.Contracts.v1.RegisterProcess;
using SetProcessOverdueAt = Shuttle.Workflow.Application.SetProcessOverdueAt;

namespace Shuttle.Workflow.Tests.Api;

public class ProcessApiFixture
{
    [Test]
    public async Task Should_be_able_to_continue_using_a_continuation_token()
    {
        var factory = new FixtureWebApplicationFactory();
        var client = factory.CreateClient();

        var processId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var token = Guid.NewGuid();

        factory.ProcessQuery
            .Setup(m => m.SearchAsync(It.IsAny<SqlServer.Models.Process.Specification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new()
                {
                    Id = processId,
                    Name = "process@1",
                    Status = Process.StatusNames.Waiting,
                    DateRegistered = DateTimeOffset.UtcNow,
                    ContinuationToken = token,
                    ContinuationMessageId = messageId,
                    ContinuationRegisteredAt = DateTimeOffset.UtcNow,
                    Messages = [new() { Id = messageId, ProcessId = processId, TypeName = "assemblyA.messageA, assemblyA", SequenceNumber = 1 }]
                }
            ]);

        var response = await client.PatchAsync($"/v1/processes/{processId}/continue/{token}", null);

        Assert.That(response.IsSuccessStatusCode, Is.True);

        factory.Mediator.Verify(m => m.SendAsync(It.IsAny<ContinueProcess>(), It.IsAny<CancellationToken>()), Times.Once);
        factory.Bus.Verify(m => m.SendAsync(It.IsAny<SendProcessMessage>(), It.IsAny<Action<TransportMessageBuilder>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_be_able_to_post_a_new_process()
    {
        var factory = new FixtureWebApplicationFactory();
        var client = factory.CreateClient();

        var message = new RegisterProcess
        {
            Name = ".",
            Messages = [new() { TypeName = "assemblyA.messageA, assemblyA", SequenceNumber = 1 }]
        };

        var response = await client.PostAsJsonAsync("/v1/processes/", message);

        Assert.That(response.IsSuccessStatusCode, Is.True);

        factory.Mediator.Verify(m => m.SendAsync(It.IsAny<Application.RegisterProcess>(), It.IsAny<CancellationToken>()), Times.Once);
        factory.Bus.Verify(m => m.SendAsync(It.IsAny<SendProcessMessage>(), It.IsAny<Action<TransportMessageBuilder>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_be_able_to_set_the_overdue_at_of_a_process()
    {
        var factory = new FixtureWebApplicationFactory();
        var client = factory.CreateClient();

        var processId = Guid.NewGuid();

        factory.ProcessQuery
            .Setup(m => m.SearchAsync(It.IsAny<SqlServer.Models.Process.Specification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = processId, Name = "process@1", Status = Process.StatusNames.Waiting, DateRegistered = DateTimeOffset.UtcNow }]);

        var overdueAt = DateTimeOffset.UtcNow.AddHours(1);

        var response = await client.PatchAsJsonAsync($"/v1/processes/{processId}/overdue-at", new OverdueProcess { OverdueAt = overdueAt });

        Assert.That(response.IsSuccessStatusCode, Is.True);

        factory.Mediator.Verify(m => m.SendAsync(It.Is<SetProcessOverdueAt>(command => command.ProcessId == processId && command.OverdueAt == overdueAt), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Should_not_continue_with_an_invalid_continuation_token()
    {
        var factory = new FixtureWebApplicationFactory();
        var client = factory.CreateClient();

        var processId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        factory.ProcessQuery
            .Setup(m => m.SearchAsync(It.IsAny<SqlServer.Models.Process.Specification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new()
                {
                    Id = processId,
                    Name = "process@1",
                    Status = Process.StatusNames.Started,
                    DateRegistered = DateTimeOffset.UtcNow,
                    ContinuationToken = Guid.NewGuid(),
                    ContinuationMessageId = messageId,
                    Messages = [new() { Id = messageId, ProcessId = processId, TypeName = "assemblyA.messageA, assemblyA", SequenceNumber = 1 }]
                }
            ]);

        var response = await client.PatchAsync($"/v1/processes/{processId}/continue/{Guid.NewGuid()}", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));

        factory.Bus.Verify(m => m.SendAsync(It.IsAny<SendProcessMessage>(), It.IsAny<Action<TransportMessageBuilder>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Should_return_a_continuation_token_when_waiting()
    {
        var factory = new FixtureWebApplicationFactory();
        var client = factory.CreateClient();

        var processId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        factory.ProcessQuery
            .Setup(m => m.SearchAsync(It.IsAny<SqlServer.Models.Process.Specification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { Id = processId, Name = "process@1", Status = Process.StatusNames.Started, DateRegistered = DateTimeOffset.UtcNow }]);

        factory.Mediator
            .Setup(m => m.SendAsync(It.IsAny<WaitProcess>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((command, _) => ((WaitProcess)command).WithContinuation(Guid.NewGuid(), messageId, DateTimeOffset.UtcNow))
            .Returns(Task.CompletedTask);

        var response = await client.PatchAsJsonAsync($"/v1/processes/{processId}/wait", new ProcessStatus { Message = "waiting" });

        Assert.That(response.IsSuccessStatusCode, Is.True);

        var continuation = await response.Content.ReadFromJsonAsync<ProcessContinuation>();

        Assert.That(continuation, Is.Not.Null);
        Assert.That(continuation!.Token, Is.Not.EqualTo(Guid.Empty));
        Assert.That(continuation.MessageId, Is.EqualTo(messageId));

        factory.Mediator.Verify(m => m.SendAsync(It.IsAny<WaitProcess>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}