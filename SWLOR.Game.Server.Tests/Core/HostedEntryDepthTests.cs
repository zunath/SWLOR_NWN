using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// While a combat recording runs, every callback of the host entry counts as a managed entry, as the native entry's
/// callbacks do (<see cref="ManagedEntryDepth"/>): the depth is one inside the callback and back to zero after it, also
/// when the callback fails.
/// </summary>
[TestFixture]
public class HostedEntryDepthTests
{
    private FacadeContainer _container;
    private IDisposable _installed;

    [OneTimeSetUp]
    public void BuildContainer() =>
        _container = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton(RecordingFlagCombatRecorder.Create())));

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void Install() => _installed = _container.Install();

    [TearDown]
    public void Uninstall() => _installed.Dispose();

    [TestCase(false)]
    [TestCase(true)]
    public void A_closure_counts_as_a_managed_entry(bool fails)
    {
        var host = new RecordingHostClosures();
        var manager = new HostedClosureManager(host);
        var depth = -1;
        ((global::NWN.Core.ICoreFunctionHandler)manager).ClosureDelayCommand(1, 0f, () =>
        {
            depth = ManagedEntryDepth.Current;
            if (fails)
                throw new InvalidOperationException("closure");
        });

        host.Scheduled.Single().Closure();

        depth.Should().Be(1);
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void A_script_counts_as_a_managed_entry(bool fails)
    {
        var depth = -1;
        var dispatcher = new HostedScriptDispatcher((_, _) =>
        {
            depth = ManagedEntryDepth.Current;
            return fails ? throw new InvalidOperationException("script") : 0;
        });

        dispatcher.TryHandle("mod_load", 0, out _);

        depth.Should().Be(1);
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void A_frame_counts_as_a_managed_entry(bool fails)
    {
        var depth = -1;
        var loop = new HostedMainLoop(new RecordingHostCallbacks(), _ =>
        {
            depth = ManagedEntryDepth.Current;
            if (fails)
                throw new InvalidOperationException("frame");
        });

        loop.Tick();

        depth.Should().Be(1);
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void A_signal_counts_as_a_managed_entry(bool fails)
    {
        var depth = -1;
        var handler = new HostedSignalHandler(_ =>
        {
            depth = ManagedEntryDepth.Current;
            if (fails)
                throw new InvalidOperationException("signal");
        });

        handler.OnSignal("ON_MODULE_LOAD_FINISH");

        depth.Should().Be(1);
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [Test]
    public void The_recorder_double_reports_a_running_recording()
    {
        GameServices.CombatRecorder.IsRecording.Should().BeTrue();
        GameServices.CombatRecorder.Should().BeAssignableTo<ICombatRecorderService>();
    }
}
