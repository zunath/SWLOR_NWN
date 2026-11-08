using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The status effect reads run on every attack and effect tick, so a facade call that only reads a table must not
/// allocate: the facade resolves its instance from a cache and the instance reads its own fields. These tests count the
/// bytes the current thread allocates over many calls. The native attack cycle calls are here too: a swing must not
/// gain an allocation from the conversion.
/// </summary>
[TestFixture]
[NonParallelizable]
public class StatusEffectFacadeAllocationTests
{
    private const int Calls = 20000;
    private const uint Tracked = 501;
    private const uint Untracked = 502;

    private static readonly IReadOnlySet<Type> NoEffects = new HashSet<Type> { typeof(OtherProbeEffect) };

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp()
    {
        _installed = _container.Install();
        var service = _container.Get<IStatusEffectService>();
        service.OnApplyNWNStatusEffect(Tracked);
        service.CacheData();
    }

    [TearDown]
    public void TearDown()
    {
        _container.Get<IStatusEffectService>().RemoveCreature(Tracked, false);
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
    public void TheStatReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += StatusEffect.GetStatAdjustment(Tracked, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatAdjustment(Untracked, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatSources(Tracked, StatType.Attack).Count).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatSources(Untracked, StatType.Attack).Count).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheActiveEffectReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += StatusEffect.HasAnyActiveEffect(Tracked, NoEffects) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasAnyActiveEffect(Untracked, NoEffects) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasAnyActiveEffect(Tracked, StatusEffectSourceType.Stance) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasAnyActiveEffect(Untracked, StatusEffectSourceType.Stance) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasEffect(typeof(ProbeEffect), Untracked) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasEffect<ProbeEffect>(Untracked) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatusEffect<ProbeEffect>(Untracked) == null ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetCreatureStatusEffects(Tracked).StatGroup == null ? 1 : 0).Should().Be(0);
        Allocated(() => StatusEffect.OnGuardedHit(Untracked, Tracked, 5)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>
    /// These reads capture their arguments in a lambda, so the compiler builds its closure on entry, as it did in the
    /// old static class: one object per call and nothing the conversion added.
    /// </summary>
    [Test]
    public void TheReadsWithALambdaAllocateOnlyTheirClosure()
    {
        const int OneCapture = 24;
        const int TwoCaptures = 32;
        var sink = 0;

        Allocated(() => sink += StatusEffect.HasStatusEffect(Untracked, typeof(ProbeEffect)) ? 1 : 0).Should().Be(Calls * OneCapture);
        Allocated(() => sink += StatusEffect.HasStatusEffect<ProbeEffect>(Untracked) ? 1 : 0).Should().Be(Calls * OneCapture);
        Allocated(() => sink += StatusEffect.HasStatusEffect(Untracked, typeof(ProbeEffect), Tracked) ? 1 : 0).Should().Be(Calls * TwoCaptures);
        Allocated(() => sink += StatusEffect.GetStatusEffect(Untracked, typeof(ProbeEffect)) == null ? 1 : 0).Should().Be(Calls * OneCapture);
        Allocated(() => sink += StatusEffect.GetStatusEffect(Untracked, typeof(ProbeEffect), Tracked) == null ? 1 : 0).Should().Be(Calls * TwoCaptures);
        Allocated(() => sink += StatusEffect.HasStatusEffectCategory(Untracked, StatusEffectCategory.Debuff) ? 1 : 0).Should().Be(Calls * OneCapture);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheCatalogAndIconReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += StatusEffect.HasStatusEffectDefinition(typeof(BleedStatusEffect)) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatusEffectName(typeof(BleedStatusEffect)).Length).Should().Be(0);
        Allocated(() => sink += (int)StatusEffect.GetStatusEffectSourceType(typeof(BleedStatusEffect))).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasStatusEffectCategory(typeof(BleedStatusEffect), StatusEffectCategory.Debuff) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += (int)StatusEffect.GetEffectTypeFromIcon(EffectIconType.Stunned)).Should().Be(0);
        Allocated(() => sink += (int)StatusEffect.GetAbilityTypeBuffed(EffectIconType.AbilityIncreaseSTR)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheDurationRulesAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += StatusEffect.ClampHardCrowdControlDurationTicks(StatusEffectCategory.HardCrowdControl, 40, 1f)).Should().Be(0);
        Allocated(() => sink += StatusEffect.ClampConvertedControlDurationTicks(40, 1f, 5f)).Should().Be(0);
        Allocated(() => sink += (int)StatusEffect.CalculateAdjustedStatusDurationSeconds(10f, 50)).Should().Be(0);
        Allocated(() => sink += StatusEffect.ShouldAnnounceNearby(false, false) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheNativeAttackCycleAllocatesNothing()
    {
        Allocated(() =>
        {
            StatusEffect.BeginNativeAttackSwing(Tracked);
            StatusEffect.BeginNativeAttackSwing(Tracked);
            StatusEffect.EndNativeAttackSwing(Tracked);
            StatusEffect.EndNativeAttackSwing(Tracked);
        }).Should().Be(0);

        Allocated(() => StatusEffect.ForgetDestroyedNativeAttackSwing(Untracked)).Should().Be(0);
    }

    /// <summary>
    /// What the hot reads cost through the facade, against the instance. Not a pass or fail: run it on its own in Release:
    /// <c>dotnet test SWLOR.Game.Server.Tests -c Release --no-build --filter "FullyQualifiedName~StatusEffectFacadeAllocationTests.Measure_hot_read_cost" --logger "console;verbosity=detailed"</c>.
    /// </summary>
    [Test, Explicit("Micro-benchmark. Run on its own in Release.")]
    public void Measure_hot_read_cost()
    {
        const int Iterations = 50_000_000;
        long sink = 0;

        double Time(Func<int> body)
        {
            for (var i = 0; i < 2_000_000; i++)
                sink += body();

            var watch = Stopwatch.StartNew();
            for (var i = 0; i < Iterations; i++)
                sink += body();
            watch.Stop();
            return watch.Elapsed.TotalMilliseconds * 1_000_000 / Iterations;
        }

        var instance = _container.Get<IStatusEffectService>();
        var direct = Time(() => instance.GetStatAdjustment(Tracked, StatType.Attack));
        var facade = Time(() => StatusEffect.GetStatAdjustment(Tracked, StatType.Attack));
        var genericDirect = Time(() => instance.HasEffect<ProbeEffect>(Untracked) ? 1 : 0);
        var genericFacade = Time(() => StatusEffect.HasEffect<ProbeEffect>(Untracked) ? 1 : 0);
        var missBefore = GC.GetAllocatedBytesForCurrentThread();
        sink += StatusEffect.GetCreatureStatusEffects(Untracked).StatGroup.Stats.Count;
        var missBytes = GC.GetAllocatedBytesForCurrentThread() - missBefore;

        TestContext.Out.WriteLine($"GetStatAdjustment: instance {direct:F2} ns, facade {facade:F2} ns");
        TestContext.Out.WriteLine($"HasEffect<T>: instance {genericDirect:F2} ns, facade {genericFacade:F2} ns");
        TestContext.Out.WriteLine($"GetCreatureStatusEffects of an untracked creature: {missBytes} bytes per call, as before the conversion");
        sink.Should().BeGreaterThan(-1);
    }

    [Test]
    public void GenericReads_GoThroughTheInterfaceWithoutAnAllocationPerClosedType()
    {
        var sink = 0;

        Allocated(() => sink += StatusEffect.HasEffect<BleedStatusEffect>(Untracked) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.HasEffect<BurnStatusEffect>(Untracked) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += StatusEffect.GetStatusEffect<BurnStatusEffect>(Untracked) == null ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }
}
