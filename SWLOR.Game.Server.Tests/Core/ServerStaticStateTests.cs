using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Async;
using SWLOR.NWN.API;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>The real steps of the teardown that act on process-wide state: the scheduler, the task queue and the script execution provider.</summary>
[NonParallelizable]
public class ServerStaticStateTests
{
    private readonly ServerStaticState _state = new();

    [Test]
    public void Clearing_the_scheduler_cancels_one_shot_and_repeating_work()
    {
        var oneShot = 0;
        var repeating = 0;
        Scheduler.Process();
        Scheduler.Schedule(() => oneShot++, TimeSpan.Zero);
        Scheduler.ScheduleRepeating(() => repeating++, TimeSpan.FromMilliseconds(1));

        _state.ClearScheduler();
        Thread.Sleep(20);
        Scheduler.Process();
        Scheduler.Process();

        oneShot.Should().Be(0);
        repeating.Should().Be(0);
    }

    [Test]
    public void Work_scheduled_after_a_clear_still_runs()
    {
        _state.ClearScheduler();
        var calls = 0;
        Scheduler.Process();
        Scheduler.Schedule(() => calls++, TimeSpan.Zero);

        Scheduler.Process();
        Scheduler.Process();

        calls.Should().Be(1);
    }

    [Test]
    public void Clearing_the_task_queue_drops_work_posted_for_the_next_frame()
    {
        var context = NwTask.MainThreadSynchronizationContext;
        var ran = 0;
        context.Update();
        context.Post(_ => ran++, null);

        _state.ClearNwTaskQueue();
        context.Update();

        ran.Should().Be(0);
    }

    [Test]
    public void Clearing_the_script_execution_provider_leaves_none()
    {
        var previous = ScriptExecutionProvider.Current;
        try
        {
            ScriptExecutionProvider.SetProvider(new ScriptExecutionProviderImpl());

            _state.ClearScriptExecutionProvider();

            ScriptExecutionProvider.Current.Should().BeNull();
        }
        finally
        {
            ScriptExecutionProvider.SetProvider(previous);
        }
    }

}
