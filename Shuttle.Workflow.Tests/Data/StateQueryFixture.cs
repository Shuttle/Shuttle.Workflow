using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.SqlServer.Models;

namespace Shuttle.Workflow.Tests.Data;

public class StateQueryFixture : DataFixture
{
    [Test]
    public async Task Should_be_able_to_query_state_async()
    {
        var dbContext = ServiceProvider!.GetRequiredService<WorkflowDbContext>();
        var stateQuery = ServiceProvider!.GetRequiredService<IStateQuery>();
        var stateId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            dbContext.States.Add(new() { Id = stateId, Key = $"[state]:{stateId}" });

            dbContext.StateItems.AddRange(
                new StateItem { StateId = stateId, Name = "string-name", Type = StateItemType.String, StringValue = "string-value", EffectiveDate = now },
                new StateItem { StateId = stateId, Name = "datetime-name", Type = StateItemType.DateTime, StringValue = now.UtcDateTime.ToString("O"), DateTimeValue = now, EffectiveDate = now },
                new StateItem { StateId = stateId, Name = "decimal-name", Type = StateItemType.Decimal, StringValue = "2.35", DecimalValue = 2.35m, EffectiveDate = now },
                new StateItem { StateId = stateId, Name = "boolean-name", Type = StateItemType.Boolean, StringValue = "True", BooleanValue = true, EffectiveDate = now },
                new StateItem { StateId = stateId, Name = "guid-name", Type = StateItemType.Guid, StringValue = Guid.NewGuid().ToString("D"), GuidValue = Guid.NewGuid(), EffectiveDate = now });

            await dbContext.SaveChangesAsync();

            var stateModel = (await stateQuery.SearchAsync(
                    new SqlServer.Models.State.Specification()
                        .AddId(stateId)))
                .FirstOrDefault();

            Assert.That(stateModel, Is.Not.Null);
            Assert.That(stateModel!.Items.Count, Is.Zero);

            stateModel = (await stateQuery.SearchAsync(
                    new SqlServer.Models.State.Specification()
                        .AddId(stateId)
                        .IncludeItems()))
                .FirstOrDefault();

            Assert.That(stateModel, Is.Not.Null);
            Assert.That(stateModel!.Items.Count, Is.EqualTo(5));
        }
    }
}