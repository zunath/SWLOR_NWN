using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The closure manager of the host entry: OBJECT_SELF stays the game's, as on the native entry, while each closure is
/// scheduled with the host's handler and runs back through the native entry's closure body.
/// </summary>
[TestFixture]
public class HostedClosureManagerTests
{
    private const uint Invalid = 0x7F000000;

    private FacadeContainer _container;
    private IDisposable _installed;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    // ManagedEntryDepth reads the combat recorder's flag through GameServices on every managed entry.
    [SetUp]
    public void Install() => _installed = _container.Install();

    [TearDown]
    public void Uninstall() => _installed.Dispose();

    [Test]
    public void Each_closure_goes_to_the_host_with_the_object_and_delay_the_game_gave()
    {
        var host = new RecordingHostClosures();
        global::NWN.Core.ICoreFunctionHandler manager = new HostedClosureManager(host);

        manager.ClosureAssignCommand(11, () => { });
        manager.ClosureDelayCommand(12, 2.5f, () => { });
        manager.ClosureActionDoCommand(13, () => { });

        host.Scheduled.Select(closure => (closure.Kind, closure.Object, closure.Duration))
            .Should().Equal(("AssignCommand", 11u, 0f), ("DelayCommand", 12u, 2.5f), ("ActionDoCommand", 13u, 0f));
    }

    [Test]
    public void NWN_Core_reads_the_games_OBJECT_SELF_not_the_hosts()
    {
        var host = new RecordingHostClosures { ObjectSelf = 99 };
        var manager = new HostedClosureManager(host);

        manager.ObjectSelf = 5;

        ((global::NWN.Core.ICoreFunctionHandler)manager).ObjectSelf.Should().Be(5u);
        host.ObjectSelf.Should().Be(99u, "the host's own OBJECT_SELF is only read when a closure starts");
    }

    [Test]
    public void A_closure_runs_as_the_object_the_engine_called_it_back_for_and_restores_OBJECT_SELF()
    {
        var host = new RecordingHostClosures();
        var manager = new HostedClosureManager(host);
        var seen = 0u;
        ((global::NWN.Core.ICoreFunctionHandler)manager).ClosureDelayCommand(21, 1f, () => seen = manager.ObjectSelf);
        manager.ObjectSelf = Invalid;

        // The host makes the callback's object its own OBJECT_SELF before it runs the closure.
        host.ObjectSelf = 21;
        host.Scheduled.Single().Closure();

        seen.Should().Be(21u);
        manager.ObjectSelf.Should().Be(Invalid);
    }

    [Test]
    public void A_closure_that_throws_is_logged_and_contained_and_OBJECT_SELF_is_restored()
    {
        var host = new RecordingHostClosures();
        var manager = new HostedClosureManager(host);
        ((global::NWN.Core.ICoreFunctionHandler)manager).ClosureAssignCommand(31, () => throw new InvalidOperationException("closure body"));
        manager.ObjectSelf = 7;
        host.ObjectSelf = 31;

        var run = () => host.Scheduled.Single().Closure();

        run.Should().NotThrow("the native entry logs a closure's exception and carries on");
        manager.ObjectSelf.Should().Be(7u);
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [Test]
    public void The_manager_needs_the_hosts_handler()
    {
        var create = () => new HostedClosureManager(null);

        create.Should().Throw<ArgumentNullException>();
    }
}
