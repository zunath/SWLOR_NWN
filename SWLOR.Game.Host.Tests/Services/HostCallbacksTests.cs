using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.Host.Services;

namespace SWLOR.Game.Host.Tests.Services;

[TestFixture]
public class HostCallbacksTests
{
    private sealed class RecordingHandler : IHostSignalHandler
    {
        public List<string> Signals { get; } = [];

        public void OnSignal(string signal) => Signals.Add(signal);
    }

    private sealed class Dispatcher : Nwn.Hosting.Contracts.Generations.IScriptDispatcher
    {
        public bool TryHandle(string scriptName, uint objectSelf, out bool conditionalResult)
        {
            conditionalResult = false;
            return true;
        }
    }

    [Test]
    public void An_activation_dispatcher_is_held_until_it_is_unbound_and_another_cannot_unbind_it()
    {
        var callbacks = new HostCallbacks();
        var activating = new Dispatcher();

        callbacks.ActivationScripts.Should().BeNull();
        callbacks.BindActivationScripts(activating);
        callbacks.UnbindActivationScripts(new Dispatcher());
        callbacks.ActivationScripts.Should().BeSameAs(activating);
        callbacks.UnbindActivationScripts(activating);
        callbacks.ActivationScripts.Should().BeNull();

        var bindNothing = () => callbacks.BindActivationScripts(null!);
        bindNothing.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Signals_go_to_the_bound_handler_until_it_is_unbound()
    {
        var callbacks = new HostCallbacks();
        var handler = new RecordingHandler();

        callbacks.DeliverSignal("before");
        callbacks.BindSignals(handler);
        callbacks.DeliverSignal("ON_MODULE_LOAD_FINISH");
        callbacks.UnbindSignals(handler);
        callbacks.DeliverSignal("after");

        handler.Signals.Should().Equal("ON_MODULE_LOAD_FINISH");
        callbacks.HasSignalHandler.Should().BeFalse();
    }

    [Test]
    public void Unbinding_another_handler_keeps_the_bound_one()
    {
        var callbacks = new HostCallbacks();
        var bound = new RecordingHandler();
        var retired = new RecordingHandler();
        callbacks.BindSignals(bound);

        callbacks.UnbindSignals(retired);
        callbacks.DeliverSignal("signal");

        bound.Signals.Should().Equal("signal");
        retired.Signals.Should().BeEmpty();
    }

    [Test]
    public void A_later_handler_replaces_the_bound_one()
    {
        var callbacks = new HostCallbacks();
        var first = new RecordingHandler();
        var second = new RecordingHandler();

        callbacks.BindSignals(first);
        callbacks.BindSignals(second);
        callbacks.DeliverSignal("signal");

        first.Signals.Should().BeEmpty();
        second.Signals.Should().Equal("signal");
    }

    [Test]
    public void The_frame_is_the_one_being_delivered()
    {
        var callbacks = new HostCallbacks();

        callbacks.Frame.Should().Be(0);
        callbacks.BeginFrame(12345);

        ((IHostCallbacks)callbacks).Frame.Should().Be(12345);
    }

    [Test]
    public void Binding_needs_a_handler()
    {
        var callbacks = new HostCallbacks();

        var bind = () => callbacks.BindSignals(null!);
        var unbind = () => callbacks.UnbindSignals(null!);

        bind.Should().Throw<ArgumentNullException>();
        unbind.Should().Throw<ArgumentNullException>();
    }
}
