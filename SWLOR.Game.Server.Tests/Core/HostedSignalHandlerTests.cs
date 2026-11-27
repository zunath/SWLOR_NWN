using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>The host entry's signal callback: the native entry's signal body and table.</summary>
[TestFixture]
public class HostedSignalHandlerTests
{
    private FacadeContainer _container;
    private IDisposable _installed;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void Install() => _installed = _container.Install();

    [TearDown]
    public void Uninstall() => _installed.Dispose();

    [Test]
    public void A_signal_goes_to_the_signal_table()
    {
        var signals = new List<string>();
        var handler = new HostedSignalHandler(signals.Add);

        handler.OnSignal("ON_MODULE_LOAD_FINISH");

        signals.Should().Equal("ON_MODULE_LOAD_FINISH");
    }

    [Test]
    public void A_signal_that_fails_is_logged_and_contained()
    {
        var handler = new HostedSignalHandler(_ => throw new InvalidOperationException("signal"));

        var signal = () => handler.OnSignal("ON_MODULE_LOAD_FINISH");

        signal.Should().NotThrow();
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [Test]
    public void The_native_table_ignores_the_signals_it_does_not_act_on()
    {
        var handler = new HostedSignalHandler();

        var signal = () => handler.OnSignal("ON_NWNX_LOADED");

        signal.Should().NotThrow();
    }

    [Test]
    public void The_handler_needs_a_table()
    {
        var create = () => new HostedSignalHandler(null);

        create.Should().Throw<ArgumentNullException>();
    }
}
