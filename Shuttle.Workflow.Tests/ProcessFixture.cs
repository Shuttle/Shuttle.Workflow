using NUnit.Framework;

namespace Shuttle.Workflow.Tests;

[TestFixture]
public class ProcessFixture
{
    private static Process Registered(string? key = null)
    {
        var process = new Process();

        process.Register("process@1", key, "process v1");

        return process;
    }

    [Test]
    public void Should_be_able_to_create_a_valid_process()
    {
        var process = Registered();

        var message1 = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1);
        var message2 = new Process.Message(Guid.NewGuid(), "assemblyA.messageB, assemblyA", 2);
        var message3 = new Process.Message(Guid.NewGuid(), "assemblyB.messageA, assemblyB", 3);

        Assert.That(() => process.AddMessage(message2), Throws.InvalidOperationException);

        Assert.That(() => process.AddMessage(message1), Throws.Nothing);
        Assert.That(() => process.AddMessage(message2), Throws.Nothing);
        Assert.That(() => process.AddMessage(message3), Throws.Nothing);

        Assert.That(process.GetMessages().Count, Is.EqualTo(3));
    }

    [Test]
    public void Should_be_able_to_complete_a_message()
    {
        var process = Registered();

        var message1 = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1);

        process.AddMessage(message1);
        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageB, assemblyA", 2));
        process.AddMessage(new(Guid.NewGuid(), "assemblyB.messageA, assemblyB", 3));

        Assert.That(() => process.CompleteMessage(message1.Id), Throws.Nothing);
        Assert.That(process.GetMessage(message1.Id).HasCompleted, Is.True);
    }

    [Test]
    public void Should_be_able_to_set_a_continuation_token_referencing_the_next_message()
    {
        var process = Registered();
        var message1 = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1);

        process.AddMessage(message1);
        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageB, assemblyA", 2));

        var continuationSet = process.WithContinuation();

        Assert.That(continuationSet.Token, Is.Not.EqualTo(Guid.Empty));
        Assert.That(process.ContinuationToken, Is.EqualTo(continuationSet.Token));
        Assert.That(process.ContinuationMessageId, Is.EqualTo(message1.Id));
        Assert.That(process.ContinuationRegisteredAt, Is.Not.Null);
    }

    [Test]
    public void Should_clear_the_continuation_when_a_message_is_completed()
    {
        var process = Registered();
        var message1 = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1);

        process.AddMessage(message1);
        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageB, assemblyA", 2));

        process.WithContinuation();

        Assert.That(process.ContinuationToken, Is.Not.Null);

        process.CompleteMessage(message1.Id);

        Assert.That(process.ContinuationToken, Is.Null);
        Assert.That(process.ContinuationMessageId, Is.Null);
        Assert.That(process.ContinuationRegisteredAt, Is.Null);
    }

    [Test]
    public void Should_clear_the_continuation_when_the_process_continues()
    {
        var process = Registered();

        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1));

        process.WithContinuation();

        Assert.That(process.ContinuationToken, Is.Not.Null);

        process.Continue();

        Assert.That(process.ContinuationToken, Is.Null);
        Assert.That(process.ContinuationMessageId, Is.Null);
        Assert.That(process.ContinuationRegisteredAt, Is.Null);
    }

    [Test]
    public void Should_be_overdue_when_the_overdue_at_time_has_passed()
    {
        var process = Registered();

        process.WithOverdueAt(DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.That(process.IsOverdue(), Is.True);
    }

    [Test]
    public void Should_not_be_overdue_before_the_overdue_at_time()
    {
        var process = Registered();

        process.WithOverdueAt(DateTimeOffset.UtcNow.AddMinutes(10));

        Assert.That(process.IsOverdue(), Is.False);
    }

    [Test]
    public void Should_not_be_overdue_when_no_deadline_applies()
    {
        var process = Registered();

        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1));

        Assert.That(process.GetEffectiveOverdueAt(), Is.Null);
        Assert.That(process.IsOverdue(), Is.False);
    }

    [Test]
    public void Should_use_the_earlier_of_overdue_at_and_the_current_message_invoke_timeout()
    {
        var process = Registered();
        var message = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1, TimeSpan.FromMilliseconds(20));

        process.AddMessage(message);
        process.SendMessage(message.Id);

        process.WithOverdueAt(DateTimeOffset.UtcNow.AddHours(1));

        Thread.Sleep(50);

        var sentMessage = process.GetMessage(message.Id);

        Assert.That(process.IsOverdue(), Is.True);
        Assert.That(process.GetEffectiveOverdueAt(), Is.EqualTo(sentMessage.DateSent!.Value + sentMessage.InvokeTimeout!.Value));
    }

    [Test]
    public void Should_not_let_a_completed_steps_invoke_timeout_clamp_a_later_steps_deadline()
    {
        var process = Registered();

        var message1 = new Process.Message(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1, TimeSpan.FromSeconds(30));
        var message2 = new Process.Message(Guid.NewGuid(), "assemblyA.messageB, assemblyA", 2, TimeSpan.FromHours(1));

        process.AddMessage(message1);
        process.AddMessage(message2);

        process.SendMessage(message1.Id);
        process.CompleteMessage(message1.Id);
        process.SendMessage(message2.Id);

        var sentMessage2 = process.GetMessage(message2.Id);

        Assert.That(process.IsOverdue(), Is.False);
        Assert.That(process.GetEffectiveOverdueAt(), Is.EqualTo(sentMessage2.DateSent!.Value + sentMessage2.InvokeTimeout!.Value));
    }

    [Test]
    public void Should_be_able_to_abandon_an_overdue_process_that_is_waiting()
    {
        var process = Registered();

        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1));
        process.Wait("waiting");
        process.WithOverdueAt(DateTimeOffset.UtcNow.AddMinutes(-1));

        Assert.That(process.IsOverdue(), Is.True);

        process.Abandon("overdue while waiting");

        Assert.That(process.HasStatus(Process.StatusNames.Abandoned), Is.True);
        Assert.That(process.HasCompleted, Is.True);
    }

    [Test]
    public void Should_clear_the_deferral_when_the_process_continues()
    {
        var process = Registered();

        process.AddMessage(new(Guid.NewGuid(), "assemblyA.messageA, assemblyA", 1));

        process.Defer(DateTimeOffset.UtcNow.AddHours(1));

        Assert.That(process.DeferredTill, Is.Not.Null);
        Assert.That(process.IsDeferred(), Is.True);

        process.Continue();

        Assert.That(process.DeferredTill, Is.Null);
        Assert.That(process.IsDeferred(), Is.False);
    }

    [Test]
    public void Should_not_raise_a_duplicate_commit_for_the_same_key()
    {
        var process = Registered();

        var dateCommitted = DateTimeOffset.UtcNow;

        Assert.That(process.Commit("commit-key", dateCommitted), Is.Not.Null);
        Assert.That(process.Commit("commit-key", dateCommitted), Is.Null);
        Assert.That(process.Commit("another-key", dateCommitted), Is.Not.Null);
    }
}