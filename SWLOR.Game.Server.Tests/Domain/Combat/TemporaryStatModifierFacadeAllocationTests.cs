using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// <c>TemporaryStatModifier.GetStatAdjustment</c> runs on every <c>Stat.GetStatAdjustment</c>. The static path
/// (<c>GameServices.TemporaryStatModifier</c>) must add no allocation to a read: it resolves the instance
/// from a per-service slot and calls it. The read itself allocates the closure of its two lambdas (32 bytes, as on master); these tests
/// pin that figure and that the static path costs nothing on top of the instance.
/// </summary>
[TestFixture]
[NonParallelizable]
public class TemporaryStatModifierFacadeAllocationTests
{
    private const int Calls = 20000;
    private const int ClosureBytes = 32;
    private const uint Tracked = 8101;
    private const uint Untracked = 8102;

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp()
    {
        _installed = _container.Install();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _container.Get<ITemporaryStatModifierService>().Add(Tracked, StatType.Attack, 5, 100000f, "g");
    }

    [TearDown]
    public void TearDown()
    {
        _container.Get<ITemporaryStatModifierService>().Clear(Tracked);
        _engineScope.Dispose();
        _installed.Dispose();
    }

    private static long Allocated(Action call)
    {
        for (var i = 0; i < 100; i++)
            call();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            call();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Test]
    public void TheFacadeAddsNoAllocationToTheHotRead()
    {
        var instance = _container.Get<ITemporaryStatModifierService>();
        var sink = 0;

        Allocated(() => sink += TemporaryStatModifier.GetStatAdjustment(Untracked, StatType.Attack))
            .Should().Be(Allocated(() => sink += instance.GetStatAdjustment(Untracked, StatType.Attack)))
            .And.Be(Calls * ClosureBytes);
        Allocated(() => sink += TemporaryStatModifier.GetStatAdjustment(Tracked, StatType.Attack))
            .Should().Be(Allocated(() => sink += instance.GetStatAdjustment(Tracked, StatType.Attack)));
        Allocated(() => sink += TemporaryStatModifier.GetStatAdjustment(Tracked, StatType.Attack, "g"))
            .Should().Be(Allocated(() => sink += instance.GetStatAdjustment(Tracked, StatType.Attack, "g")));
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheSourceReadsAllocateNothingWhenThereIsNothingToReport()
    {
        var sink = 0;

        Allocated(() => sink += TemporaryStatModifier.GetStatSources(Untracked, StatType.Attack).Count).Should().Be(0);
        Allocated(() => sink += TemporaryStatModifier.GetStatSources(Tracked, StatType.Defense).Count).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheNoOpWritesAddNothingToTheInstance()
    {
        var instance = _container.Get<ITemporaryStatModifierService>();

        Allocated(() => TemporaryStatModifier.Add(NWScript.OBJECT_INVALID, StatType.Attack, 5, 30f)).Should().Be(0);
        // Refresh builds its lambda closure on entry (40 bytes with the compiler cached delegate); the code is unchanged.
        Allocated(() => TemporaryStatModifier.Refresh(Untracked, StatType.Attack, 0f))
            .Should().Be(Allocated(() => instance.Refresh(Untracked, StatType.Attack, 0f))).And.Be(Calls * 40);
        Allocated(() => TemporaryStatModifier.Clear(Untracked)).Should().Be(0);
    }
}
