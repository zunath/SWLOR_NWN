using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The teardown runs each step once, in a fixed order, and a step that fails does not stop the ones after it. Each step
/// is a test double that writes its name to one shared log, so the order the tests pin is the order that runs.
/// </summary>
[TestFixture]
public class ServerTeardownTests
{
    private static readonly string[] StepOrder =
    {
        "ClearScheduler",
        "ClearNwTaskQueue",
        "StopCombatRecorder",
        "FlushLogs",
        "UnsubscribeProcessEvents",
        "ClearScriptExecutionProvider",
        "ReleaseFrameworkTypeCaches"
    };

    private List<string> _log = null!;
    private StaticStateDouble _staticState = null!;
    private ProcessEventsDouble _processEvents = null!;
    private ICombatRecorderService _recorder = null!;

    [SetUp]
    public void SetUp()
    {
        _log = new List<string>();
        _staticState = new StaticStateDouble(_log);
        _processEvents = new ProcessEventsDouble(_log);
        _recorder = RecorderProxy.Create(_log);
    }

    private ServerTeardown Teardown() => new(_recorder, _processEvents, _staticState);

    [Test]
    public void The_steps_run_in_the_order_of_the_shutdown_they_replace()
    {
        Teardown().Run();

        _log.Should().Equal(StepOrder,
            "the combat recorder files close before the logs flush, exactly as the two app_shutdown handlers run");
    }

    [Test]
    public void The_combat_recorder_stops_before_the_logs_flush()
    {
        Teardown().Run();

        _log.IndexOf("StopCombatRecorder").Should().BeLessThan(_log.IndexOf("FlushLogs"));
    }

    [Test]
    public void Running_it_twice_runs_every_step_twice_without_failing()
    {
        var teardown = Teardown();

        teardown.Run();
        teardown.Run();

        _log.Should().Equal(StepOrder.Concat(StepOrder));
    }

    [Test]
    public void A_failing_step_does_not_stop_the_steps_after_it_and_its_exception_is_rethrown()
    {
        var failure = new InvalidOperationException("recorder failed");
        _recorder = RecorderProxy.Create(_log, failure);

        var act = () => Teardown().Run();

        act.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(failure);
        _log.Should().Equal(StepOrder, "the recorder step threw, and every step after it still ran");
    }

    [Test]
    public void Several_failing_steps_are_rethrown_together()
    {
        _staticState.FailWith = new InvalidOperationException("log flush failed");
        _recorder = RecorderProxy.Create(_log, new InvalidOperationException("recorder failed"));

        var act = () => Teardown().Run();

        act.Should().Throw<AggregateException>().Which.InnerExceptions.Should().HaveCount(2);
        _log.Should().Contain("UnsubscribeProcessEvents").And.Contain("ClearScriptExecutionProvider").And.Contain("ReleaseFrameworkTypeCaches");
    }

    [Test]
    public void A_step_is_required()
    {
        var act = () => new ServerTeardown(null!, _processEvents, _staticState);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class StaticStateDouble : IServerStaticState
    {
        private readonly List<string> _log;

        public StaticStateDouble(List<string> log) => _log = log;

        /// <summary>When set, <see cref="FlushLogs"/> logs its step and then throws this.</summary>
        public Exception? FailWith { get; set; }

        public void ClearScheduler() => _log.Add("ClearScheduler");

        public void ClearNwTaskQueue() => _log.Add("ClearNwTaskQueue");

        public void FlushLogs()
        {
            _log.Add("FlushLogs");
            if (FailWith != null)
                throw FailWith;
        }

        public void ClearScriptExecutionProvider() => _log.Add("ClearScriptExecutionProvider");

        public void ReleaseFrameworkTypeCaches() => _log.Add("ReleaseFrameworkTypeCaches");
    }

    private sealed class ProcessEventsDouble : IProcessEventSubscriptions
    {
        private readonly List<string> _log;

        public ProcessEventsDouble(List<string> log) => _log = log;

        public bool IsSubscribedToUnhandledException => false;

        public bool IsSubscribedToProcessExit => false;

        public void SubscribeUnhandledException(Action<object> onUnhandled)
        {
        }

        public void SubscribeProcessExit(Action onExit)
        {
        }

        public void UnsubscribeAll() => _log.Add("UnsubscribeProcessEvents");
    }

    /// <summary>An <see cref="ICombatRecorderService"/> that logs <c>StopOnShutdown</c> and answers every other member with its default.</summary>
    public class RecorderProxy : DispatchProxy
    {
        private List<string> _log = null!;
        private Exception? _failure;

        public static ICombatRecorderService Create(List<string> log, Exception? failure = null)
        {
            var proxy = Create<ICombatRecorderService, RecorderProxy>();
            var recorder = (RecorderProxy)(object)proxy;
            recorder._log = log;
            recorder._failure = failure;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ICombatRecorderService.StopOnShutdown))
            {
                _log.Add("StopCombatRecorder");
                if (_failure != null)
                    throw _failure;
            }

            return targetMethod is { ReturnType.IsValueType: true } && targetMethod.ReturnType != typeof(void)
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
        }
    }
}
