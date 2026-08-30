using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shuttle.Workflow.SqlServer;
using Shuttle.Workflow.SqlServer.Models;

namespace Shuttle.Workflow.Tests.Data;

public class StateQueryItemMatchFixture : DataFixture
{
    // 'ProcessEmployeesParticipant' relies on this shape to recover leavers whose offboarding was never
    // started: an item match of '!EXISTS' has to mean "not in effect as at 'ItemEffectiveDate'", not
    // "no row was ever written".
    [Test]
    public async Task Should_not_match_an_item_that_has_been_expired()
    {
        var dbContext = ServiceProvider!.GetRequiredService<WorkflowDbContext>();
        var stateQuery = ServiceProvider!.GetRequiredService<IStateQuery>();
        var now = DateTimeOffset.UtcNow;
        var offboardingStarted = now.AddDays(-30);
        var reactivated = now.AddDays(-20);
        var missedLeaverId = Guid.NewGuid();
        var offboardedId = Guid.NewGuid();

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            dbContext.States.Add(new() { Id = missedLeaverId, Key = $"[peoplehr.employee]:{missedLeaverId}" });
            dbContext.States.Add(new() { Id = offboardedId, Key = $"[peoplehr.employee]:{offboardedId}" });

            dbContext.StateItems.AddRange(
                new StateItem { StateId = missedLeaverId, Name = "EmployeeStatus", Type = StateItemType.String, StringValue = "3", EffectiveDate = offboardingStarted },
                new StateItem { StateId = missedLeaverId, Name = "OffboardingStartDate", Type = StateItemType.DateTime, StringValue = offboardingStarted.UtcDateTime.ToString("O"), DateTimeValue = offboardingStarted, EffectiveDate = offboardingStarted, EffectiveDateEnd = reactivated },
                new StateItem { StateId = offboardedId, Name = "EmployeeStatus", Type = StateItemType.String, StringValue = "3", EffectiveDate = offboardingStarted },
                new StateItem { StateId = offboardedId, Name = "OffboardingStartDate", Type = StateItemType.DateTime, StringValue = offboardingStarted.UtcDateTime.ToString("O"), DateTimeValue = offboardingStarted, EffectiveDate = offboardingStarted });

            await dbContext.SaveChangesAsync();

            var specification = new SqlServer.Models.State.Specification()
                .AddId(missedLeaverId)
                .AddId(offboardedId)
                .WithItemEffectiveDate(now);

            specification
                .WithItemMatch(new("EmployeeStatus", "==", "3", "String"))
                .WithItemMatch(new("OffboardingStartDate", "!EXISTS", null, "DateTime"));

            var ids = (await stateQuery.SearchAsync(specification)).Select(item => item.Id).ToList();

            Assert.That(ids, Does.Contain(missedLeaverId), "the expired 'OffboardingStartDate' should not have matched");
            Assert.That(ids, Does.Not.Contain(offboardedId), "the open-ended 'OffboardingStartDate' should have matched");
        }
    }

    [Test]
    public async Task Should_only_return_the_items_in_effect_as_at_the_item_effective_date()
    {
        var dbContext = ServiceProvider!.GetRequiredService<WorkflowDbContext>();
        var stateQuery = ServiceProvider!.GetRequiredService<IStateQuery>();
        var now = DateTimeOffset.UtcNow;
        var stateId = Guid.NewGuid();

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            dbContext.States.Add(new() { Id = stateId, Key = $"[peoplehr.employee]:{stateId}" });

            dbContext.StateItems.AddRange(
                new StateItem { StateId = stateId, Name = "EmployeeStatus", Type = StateItemType.String, StringValue = "1", EffectiveDate = now.AddDays(-30), EffectiveDateEnd = now.AddDays(-10) },
                new StateItem { StateId = stateId, Name = "EmployeeStatus", Type = StateItemType.String, StringValue = "2", EffectiveDate = now.AddDays(-10), EffectiveDateEnd = now.AddDays(-1) },
                new StateItem { StateId = stateId, Name = "EmployeeStatus", Type = StateItemType.String, StringValue = "3", EffectiveDate = now.AddDays(-1) });

            await dbContext.SaveChangesAsync();

            var withoutEffectiveDate = (await stateQuery.SearchAsync(
                    new SqlServer.Models.State.Specification()
                        .AddId(stateId)
                        .IncludeItems()))
                .Single();

            // No 'ItemEffectiveDate' short-circuits the interval predicate, so the whole history comes back.
            // This is why the on/offboarding guards cannot scan 'state.Items' directly.
            Assert.That(withoutEffectiveDate.Items.Count, Is.EqualTo(3));

            var asAtNow = (await stateQuery.SearchAsync(
                    new SqlServer.Models.State.Specification()
                        .AddId(stateId)
                        .WithItemEffectiveDate(now)
                        .IncludeItems()))
                .Single();

            Assert.That(asAtNow.Items.Count, Is.EqualTo(1));
            Assert.That(asAtNow.Items.Single().StringValue, Is.EqualTo("3"));
        }
    }
}