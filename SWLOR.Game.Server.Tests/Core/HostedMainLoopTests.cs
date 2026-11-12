using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>The host entry's frame: the native entry's main-loop body for the frame number the host delivers.</summary>
[TestFixture]
public class HostedMainLoopTests
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
    public void A_tick_runs_the_main_loop_for_the_hosts_frame_and_records_it_as_the_server_frame()
    {
        var before = ServerFrame.Current;
        var host = new RecordingHostCallbacks { Frame = 777 };
        var frames = new List<ulong>();
        var serverFrameDuring = 0ul;
        var loop = new HostedMainLoop(host, frame =>
        {
            frames.Add(frame);
            serverFrameDuring = ServerFrame.Current;
        });

        try
        {
            loop.Tick();
            host.Frame = 778;
            loop.Tick();

            frames.Should().Equal(777ul, 778ul);
            serverFrameDuring.Should().Be(778ul);
            ServerFrame.Current.Should().Be(778ul);
        }
        finally
        {
            // ServerFrame is process-wide; other tests read its default.
            new HostedMainLoop(new RecordingHostCallbacks { Frame = before }, _ => { }).Tick();
        }
    }

    [Test]
    public void A_main_loop_that_throws_is_logged_and_contained()
    {
        var loop = new HostedMainLoop(new RecordingHostCallbacks(), _ => throw new InvalidOperationException("frame"));

        var tick = () => loop.Tick();

        tick.Should().NotThrow();
        ManagedEntryDepth.Current.Should().Be(0);
    }

    [Test]
    public void The_main_loop_needs_the_host_and_a_body()
    {
        var withoutHost = () => new HostedMainLoop(null);
        var withoutBody = () => new HostedMainLoop(new RecordingHostCallbacks(), null);

        withoutHost.Should().Throw<ArgumentNullException>();
        withoutBody.Should().Throw<ArgumentNullException>();
    }
}
