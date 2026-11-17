using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The stat reads run on every attack, tick and rebuild for every player, so a call through <c>GameServices</c> that only reads must not
/// allocate. These tests count the bytes the current thread allocates over many calls. The engine here is one that
/// answers from constants and records nothing (<see cref="StatReadEngine"/>), and the unconverted calls answer through
/// a subclass, so what is counted is the accessor and the instance. Reads that already allocated in the old static class
/// are pinned at what they cost: <c>TemporaryStatModifier.GetStatAdjustment</c> builds a closure on entry, <c>GetNPCStats</c>
/// builds a new <c>NPCStats</c>, and <c>GetAccuracy</c> and <c>GetEvasion</c> build log messages.
/// </summary>
[TestFixture]
[NonParallelizable]
public class StatFacadeAllocationTests
{
    private const int Calls = 20000;
    private const uint Creature = 501;

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private IDisposable _engine = null!;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer(services =>
    {
        services.Replace(ServiceDescriptor.Singleton<IStatService, AllocationStatService>());
        services.Replace(ServiceDescriptor.Singleton<ICombatRatingService, AllocationCombatRatingService>());
    });

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp()
    {
        _installed = _container.Install();
        _engine = NWScriptRuntime.Override(StatReadEngine.Create(StatReadEngine.NpcAnswers()));
        Stat.CacheData();
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
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

    // The pins are grouped by the part of the stat split that each member goes to (Readmes/MaintainabilityRefactorStatSplit.md)
    // and still read through Stat: the pull request that moves a part points its group at the part's GameServices property and
    // registers the part's allocation double (part 5, CombatRating, since P5-4c). Part 7's healing reads go through
    // CombatResources since P5-4d; the real resources service reads the stat through the allocation double, so it needs none.

    /// <summary>
    /// Part 1, <c>StatService</c> (stays): the stat adjustment read, the metadata, the equipment action read, the pools, the NPC
    /// skin reads, Detection, Stealth and Lockpicking.
    /// </summary>
    [Test]
    public void TheStatServiceReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Stat.GetStatAdjustment(Creature, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += Stat.GetStatAdjustmentExcludingTemporaryModifiers(Creature, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += Stat.GetStatAdjustment(Creature, StatType.StatusAppliedRequiredCategory)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetStatTypeCategory(StatType.Attack)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetStatTypeAggregation(StatType.Attack)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionStaminaRestore)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetGrantedDeflectionStatType(StatType.MeleeDeflectionStaminaRestore)).Should().Be(0);
        Allocated(() => sink += Stat.AggregateStatAdjustment(StatType.Attack, 1, 2)).Should().Be(0);
        Allocated(() => sink += Stat.IsBeneficialStatAdjustment(StatType.AttackPercentAdjustment, 2) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.ScaleEffect(10, 10)).Should().Be(0);
        Allocated(() => sink += Stat.GetAbilityNameShort(AbilityType.Might).Length).Should().Be(0);
        Allocated(() => sink += Stat.TryGetEquipmentStatAction(SWLOR.NWN.API.NWScript.Enum.Item.ItemPropertyType.Attack, out _) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.GetMaxFP(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetMaxStamina(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.IsStaminaRestorationBlocked(true, false) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.GetCurrentFP(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetCurrentStamina(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetAdjustedRequiredFP(Creature, 10)).Should().Be(0);
        Allocated(() => sink += Stat.GetNPCSkinMaxHP(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateDetectionRating(1, 2, 3, 4, true)).Should().Be(0);
        Allocated(() => sink += Stat.ApplyNPCDetectionCap(100, true)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateStealthRating(1, 2, 3)).Should().Be(0);
        Allocated(() => sink += Stat.GetStealth(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetDetection(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetLockpicking(Creature)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>Part 2, <c>CreatureStatService</c>.</summary>
    [Test]
    public void TheCreatureStatReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += (int)Stat.GetMovementSpeedMultiplier(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.IsWalkModeEnabled(Creature) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.GetEquippedHPBonus(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetBeastNaturalRegenAmount(30)).Should().Be(0);
        Allocated(() => sink += Stat.IsNaturalStaminaRegenerationAvailable(1, 2) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>Part 3, <c>StatRecordService</c>.</summary>
    [Test]
    public void TheRecordReadsAndAdjustmentsAllocateNothing()
    {
        var player = new Player();
        var sink = 0;

        Allocated(() => sink += Stat.GetEquippedCombatReadiness(Creature)).Should().Be(0);
        Allocated(() => Stat.AdjustEvasion(player, 1)).Should().Be(0);
        Allocated(() => Stat.AdjustAttack(player, 1)).Should().Be(0);
        Allocated(() => Stat.AdjustCombatReadiness(player, 1)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>Part 5, <c>CombatRatingService</c> (Combat), with the deflection and Guard chances.</summary>
    [Test]
    public void TheCombatRatingReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += CombatRating.GetAttack(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetAccuracy(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetEvasion(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetEvasion(10, 10, 1, 4, false)).Should().Be(0);
        Allocated(() => sink += CombatRating.CalculateDefense(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetAttackPercentAdjustment(Creature, SkillType.Force)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetDefensePercentAdjustment(Creature, CombatDamageType.Physical)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetGuardChance(Creature)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetMeleeDeflectionChance(Creature)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetRangedDeflectionChance(Creature)).Should().Be(0);
        Allocated(() => sink += CombatRating.GetShieldDeflectionChance(Creature)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>Part 7, the healing adjustments that joined <c>CombatResourcesService</c> (Combat, P5-4d).</summary>
    [Test]
    public void TheHealingAdjustmentsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += CombatResources.CalculateOutgoingAbilityHealingAmount(10, 20)).Should().Be(0);
        Allocated(() => sink += CombatResources.CalculateEffectiveHealingAmount(10, 20, 30)).Should().Be(0);
        Allocated(() => sink += CombatResources.ApplyOutgoingAbilityHealingAdjustment(Creature, 10)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    /// <summary>Parts 1 (<c>GetMaxFP</c>) and 5 (the ratings).</summary>
    [Test]
    public void TheReadsThatBuildAnNpcsStatsAllocateTheSameThroughTheFacadeAsTheInstance()
    {
        var instance = _container.Get<IStatService>();
        var rating = _container.Get<ICombatRatingService>();
        var sink = 0;

        void SameCost(Action throughTheFacade, Action direct)
        {
            // Each pair runs once to warm up: the first calls can fill a cache of the engine or of the runtime.
            Allocated(throughTheFacade);
            Allocated(direct);
            Allocated(throughTheFacade).Should().Be(Allocated(direct));
        }

        SameCost(() => sink += Stat.GetMaxFP(Creature), () => sink += instance.GetMaxFP(Creature));
        SameCost(() => sink += CombatRating.GetDefense(Creature, CombatDamageType.Physical, AbilityType.Vitality),
            () => sink += rating.GetDefense(Creature, CombatDamageType.Physical, AbilityType.Vitality));
        SameCost(() => sink += CombatRating.GetAttack(Creature, AbilityType.Might, SkillType.Force),
            () => sink += rating.GetAttack(Creature, AbilityType.Might, SkillType.Force));
        SameCost(() => sink += CombatRating.GetEvasion(Creature, SkillType.Invalid), () => sink += rating.GetEvasion(Creature, SkillType.Invalid));
        GC.KeepAlive(sink);
    }

    /// <summary>
    /// The real temporary stat read builds its closure on entry, as it did when <c>Stat</c> was static. One object per
    /// call and nothing the conversion added. It goes with <c>Combat</c>.
    /// </summary>
    [Test]
    public void TheRealTemporaryStatReadAllocatesOnlyItsClosure()
    {
        const int Closure = 32;
        using var container = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IStatService, RealTemporaryStatService>()));
        using var installed = container.Install();
        var sink = 0;

        Allocated(() => sink += Stat.GetStatAdjustment(Creature, StatType.Attack)).Should().Be(Calls * Closure);
        GC.KeepAlive(sink);
    }

    /// <summary>
    /// What the hot reads cost through GameServices, against the instance. Not a pass or fail: run it on its own in Release:
    /// <c>dotnet test SWLOR.Game.Server.Tests -c Release --no-build --filter "FullyQualifiedName~StatFacadeAllocationTests.Measure_hot_read_cost" --logger "console;verbosity=detailed"</c>.
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

        var instance = _container.Get<IStatService>();
        var adjustmentDirect = Time(() => instance.GetStatAdjustment(Creature, StatType.Attack));
        var adjustmentFacade = Time(() => Stat.GetStatAdjustment(Creature, StatType.Attack));
        var aggregateDirect = Time(() => StatRules.AggregateStatAdjustment(StatType.Attack, 1, 2));
        var aggregateFacade = Time(() => Stat.AggregateStatAdjustment(StatType.Attack, 1, 2));

        TestContext.Out.WriteLine($"GetStatAdjustment: instance {adjustmentDirect:F2} ns, facade {adjustmentFacade:F2} ns");
        TestContext.Out.WriteLine($"AggregateStatAdjustment: rules {aggregateDirect:F2} ns, facade {aggregateFacade:F2} ns");
        sink.Should().BeGreaterThan(-1);
    }

    /// <summary>The instance with the real temporary stat read. Other fixtures fill the perk tables, so that read stays out.</summary>
    private sealed class RealTemporaryStatService : StatService
    {
        public RealTemporaryStatService(
            IDBService db,
            ICombatRecorderService combatRecorder,
            IPlayerFeedbackService playerFeedback,
            IStatusEffectService statusEffect,
            IMimicryTraitStatsService mimicryTraitStats,
            ICompanionLookup beastMastery,
            IPerkService perk,
            ISkillService skill,
            ITemporaryStatModifierService temporaryStatModifier,
            IResourceRestoredListener resourceRestored)
            : base(db, combatRecorder, playerFeedback, statusEffect, mimicryTraitStats, beastMastery, perk, skill, temporaryStatModifier,
                resourceRestored)
        {
        }

        protected override int GetPerkStatBonus(uint creature, StatType stat) => 0;
    }
}
