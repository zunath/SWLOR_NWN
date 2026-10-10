using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.Async;

namespace SWLOR.Game.Server.Tests.Core;

[NonParallelizable]
public class NwTaskWaitTests
{
    [Test]
    public void WaitOnMainContextPollsOncePerFrameInsteadOfSpinning()
    {
        var previous = SynchronizationContext.Current;
        var context = NwTask.MainThreadSynchronizationContext;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var polls = 0;
            var task = NwTask.WaitUntil(() => ++polls >= 3);
            task.IsCompleted.Should().BeFalse();
            polls.Should().Be(0);

            context.Update();
            polls.Should().Be(1);
            task.IsCompleted.Should().BeFalse();
            context.Update();
            polls.Should().Be(2);
            context.Update();
            task.IsCompletedSuccessfully.Should().BeTrue();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void CancellationEndsAnUnmetWaitOnTheNextFrame()
    {
        using var cancellation = new CancellationTokenSource();
        var task = NwTask.WaitUntil(() => false, cancellation.Token);
        cancellation.Cancel();
        NwTask.MainThreadSynchronizationContext.Update();

        task.IsCompletedSuccessfully.Should().BeTrue();
    }
}
