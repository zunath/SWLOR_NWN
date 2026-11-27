using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The two process-event subscriptions are named handlers the service holds, so removing them works: after
/// <see cref="IProcessEventSubscriptions.UnsubscribeAll"/> the source has no subscriber and a raised event reaches nobody.
/// </summary>
[TestFixture]
public class ProcessEventSubscriptionsTests
{
    private FakeSource _source = null!;
    private ProcessEventSubscriptions _subscriptions = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new FakeSource();
        _subscriptions = new ProcessEventSubscriptions(_source);
    }

    [Test]
    public void Nothing_is_subscribed_until_asked()
    {
        _source.UnhandledSubscribers.Should().Be(0);
        _source.ExitSubscribers.Should().Be(0);
        _subscriptions.IsSubscribedToUnhandledException.Should().BeFalse();
        _subscriptions.IsSubscribedToProcessExit.Should().BeFalse();
    }

    [Test]
    public void The_exception_object_reaches_the_unhandled_exception_callback()
    {
        object? received = null;
        var failure = new InvalidOperationException("boom");
        _subscriptions.SubscribeUnhandledException(exceptionObject => received = exceptionObject);

        _source.RaiseUnhandled(failure);

        received.Should().BeSameAs(failure);
    }

    [Test]
    public void The_process_exit_callback_runs_when_the_process_exits()
    {
        var exits = 0;
        _subscriptions.SubscribeProcessExit(() => exits++);

        _source.RaiseExit();

        exits.Should().Be(1);
    }

    [Test]
    public void Subscribing_twice_subscribes_once_and_keeps_the_first_callback()
    {
        var first = 0;
        var second = 0;
        _subscriptions.SubscribeProcessExit(() => first++);
        _subscriptions.SubscribeProcessExit(() => second++);

        _source.RaiseExit();

        _source.ExitSubscribers.Should().Be(1);
        first.Should().Be(1);
        second.Should().Be(0);
    }

    [Test]
    public void Unsubscribing_removes_both_handlers_from_the_source()
    {
        var calls = 0;
        _subscriptions.SubscribeUnhandledException(_ => calls++);
        _subscriptions.SubscribeProcessExit(() => calls++);
        _source.UnhandledSubscribers.Should().Be(1);
        _source.ExitSubscribers.Should().Be(1);

        _subscriptions.UnsubscribeAll();

        _source.UnhandledSubscribers.Should().Be(0);
        _source.ExitSubscribers.Should().Be(0);
        _subscriptions.IsSubscribedToUnhandledException.Should().BeFalse();
        _subscriptions.IsSubscribedToProcessExit.Should().BeFalse();
        _source.RaiseUnhandled(new Exception());
        _source.RaiseExit();
        calls.Should().Be(0);
    }

    [Test]
    public void Unsubscribing_when_nothing_is_subscribed_does_nothing()
    {
        var act = () => _subscriptions.UnsubscribeAll();

        act.Should().NotThrow();
    }

    [Test]
    public void It_can_subscribe_again_after_unsubscribing()
    {
        var exits = 0;
        _subscriptions.SubscribeProcessExit(() => exits++);
        _subscriptions.UnsubscribeAll();

        _subscriptions.SubscribeProcessExit(() => exits += 10);
        _source.RaiseExit();

        _source.ExitSubscribers.Should().Be(1);
        exits.Should().Be(10);
    }

    [Test]
    public void A_callback_is_required()
    {
        ((Action)(() => _subscriptions.SubscribeProcessExit(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _subscriptions.SubscribeUnhandledException(null!))).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void The_real_source_adds_and_removes_the_handlers_without_error()
    {
        var subscriptions = new ProcessEventSubscriptions(new AppDomainProcessEventSource());

        subscriptions.SubscribeUnhandledException(_ => { });
        subscriptions.SubscribeProcessExit(() => { });
        subscriptions.IsSubscribedToUnhandledException.Should().BeTrue();
        subscriptions.IsSubscribedToProcessExit.Should().BeTrue();

        subscriptions.UnsubscribeAll();

        subscriptions.IsSubscribedToUnhandledException.Should().BeFalse();
        subscriptions.IsSubscribedToProcessExit.Should().BeFalse();
    }

    private sealed class FakeSource : IProcessEventSource
    {
        private UnhandledExceptionEventHandler? _unhandled;
        private EventHandler? _exit;

        public int UnhandledSubscribers => _unhandled?.GetInvocationList().Length ?? 0;

        public int ExitSubscribers => _exit?.GetInvocationList().Length ?? 0;

        public event UnhandledExceptionEventHandler UnhandledException
        {
            add => _unhandled += value;
            remove => _unhandled -= value;
        }

        public event EventHandler ProcessExit
        {
            add => _exit += value;
            remove => _exit -= value;
        }

        public void RaiseUnhandled(Exception exception) => _unhandled?.Invoke(this, new UnhandledExceptionEventArgs(exception, false));

        public void RaiseExit() => _exit?.Invoke(this, EventArgs.Empty);
    }
}
