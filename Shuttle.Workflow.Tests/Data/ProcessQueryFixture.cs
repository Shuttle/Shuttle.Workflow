using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.SqlServer.Models;

namespace Shuttle.Workflow.Tests.Data;

public class ProcessQueryFixture : DataFixture
{
    [Test]
    public async Task Should_be_able_to_query_process_async()
    {
        var dbContext = ServiceProvider!.GetRequiredService<WorkflowDbContext>();
        var processQuery = ServiceProvider!.GetRequiredService<IProcessQuery>();
        var processId = Guid.NewGuid();

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            dbContext.Processes.Add(new()
            {
                Id = processId,
                Name = "process@1",
                Key = "process-key:maybe.perhaps",
                Description = "process v1",
                DateRegistered = DateTimeOffset.UtcNow,
                Status = Process.StatusNames.Registered
            });

            dbContext.ProcessMessages.AddRange(
                new ProcessMessage { Id = Guid.NewGuid(), ProcessId = processId, TypeName = "assemblyA.messageA, assemblyA", SequenceNumber = 1 },
                new ProcessMessage { Id = Guid.NewGuid(), ProcessId = processId, TypeName = "assemblyA.messageB, assemblyA", SequenceNumber = 2 },
                new ProcessMessage { Id = Guid.NewGuid(), ProcessId = processId, TypeName = "assemblyB.messageA, assemblyB", SequenceNumber = 3 });

            await dbContext.SaveChangesAsync();

            var processModel = (await processQuery.SearchAsync(
                    new SqlServer.Models.Process.Specification()
                        .AddId(processId)))
                .FirstOrDefault();

            Assert.That(processModel, Is.Not.Null);
            Assert.That(processModel!.Messages.Count, Is.Zero);

            processModel = (await processQuery.SearchAsync(
                    new SqlServer.Models.Process.Specification()
                        .AddId(processId)
                        .IncludeMessages()))
                .FirstOrDefault();

            Assert.That(processModel, Is.Not.Null);
            Assert.That(processModel!.Messages.Count, Is.EqualTo(3));
        }
    }
}