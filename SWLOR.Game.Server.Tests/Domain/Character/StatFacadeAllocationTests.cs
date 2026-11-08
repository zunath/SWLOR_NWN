using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The stat reads run on every attack, tick and rebuild for every player, so a facade call that only reads must not
/// allocate. These tests count the bytes the current thread allocates over many calls. The engine here is one that
/// answers from constants and records nothing (<see cref="StatReadEngine"/>), and the unconverted calls answer through
/// a subclass, so what is counted is the facade and the instance. Reads that already allocated in the old static class
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

    [Test]
    public void TheStatAdjustmentReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Stat.GetStatAdjustment(Creature, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += Stat.GetStatAdjustmentExcludingTemporaryModifiers(Creature, StatType.Attack)).Should().Be(0);
        Allocated(() => sink += Stat.GetStatAdjustment(Creature, StatType.StatusAppliedRequiredCategory)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheMetadataReadsAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += (int)Stat.GetStatTypeCategory(StatType.Attack)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetStatTypeAggregation(StatType.Attack)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetStatTypeDeflectionSource(StatType.MeleeDeflectionStaminaRestore)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetGrantedDeflectionStatType(StatType.MeleeDeflectionStaminaRestore)).Should().Be(0);
        Allocated(() => sink += Stat.AggregateStatAdjustment(StatType.Attack, 1, 2)).Should().Be(0);
        Allocated(() => sink += Stat.IsBeneficialStatAdjustment(StatType.AttackPercentAdjustment, 2) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheFormulasAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Stat.GetMaxFP(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetMaxStamina(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetAttack(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetAccuracy(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetEvasion(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.GetEvasion(10, 10, 1, 4, false)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateDefense(10, 10, 1)).Should().Be(0);
        Allocated(() => sink += Stat.ScaleEffect(10, 10)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateDetectionRating(1, 2, 3, 4, true)).Should().Be(0);
        Allocated(() => sink += Stat.ApplyNPCDetectionCap(100, true)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateStealthRating(1, 2, 3)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateOutgoingAbilityHealingAmount(10, 20)).Should().Be(0);
        Allocated(() => sink += Stat.CalculateEffectiveHealingAmount(10, 20, 30)).Should().Be(0);
        Allocated(() => sink += Stat.GetBeastNaturalRegenAmount(30)).Should().Be(0);
        Allocated(() => sink += Stat.IsNaturalStaminaRegenerationAvailable(1, 2) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.IsStaminaRestorationBlocked(true, false) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.GetAbilityNameShort(AbilityType.Might).Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheCreatureReadsThatTouchTheEngineAllocateNothing()
    {
        var sink = 0;

        Allocated(() => sink += Stat.GetCurrentFP(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetCurrentStamina(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetAdjustedRequiredFP(Creature, 10)).Should().Be(0);
        Allocated(() => sink += Stat.GetAttackPercentAdjustment(Creature, SkillType.Force)).Should().Be(0);
        Allocated(() => sink += Stat.GetDefensePercentAdjustment(Creature, CombatDamageType.Physical)).Should().Be(0);
        Allocated(() => sink += Stat.GetGuardChance(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetMeleeDeflectionChance(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetRangedDeflectionChance(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetShieldDeflectionChance(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetStealth(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetLockpicking(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetDetection(Creature)).Should().Be(0);
        Allocated(() => sink += (int)Stat.GetMovementSpeedMultiplier(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.IsWalkModeEnabled(Creature) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Stat.GetEquippedHPBonus(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetEquippedCombatReadiness(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.GetNPCSkinMaxHP(Creature)).Should().Be(0);
        Allocated(() => sink += Stat.ApplyOutgoingAbilityHealingAdjustment(Creature, 10)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheRecordAdjustmentsAndTheEquipmentActionReadAllocateNothing()
    {
        var player = new Player();
        var sink = 0;

        Allocated(() => Stat.AdjustEvasion(player, 1)).Should().Be(0);
        Allocated(() => Stat.AdjustAttack(player, 1)).Should().Be(0);
        Allocated(() => Stat.AdjustCombatReadiness(player, 1)).Should().Be(0);
        Allocated(() => sink += Stat.TryGetEquipmentStatAction(SWLOR.NWN.API.NWScript.Enum.Item.ItemPropertyType.Attack, out _) ? 1 : 0).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void TheReadsThatBuildAnNpcsStatsAllocateTheSameThroughTheFacadeAsTheInstance()
    {
        var instance = _container.Get<IStatService>();
        var sink = 0;

        void SameCost(Action throughTheFacade, Action direct)
        {
            // Each pair runs once to warm up: the first calls can fill a cache of the engine or of the runtime.
            Allocated(throughTheFacade);
            Allocated(direct);
            Allocated(throughTheFacade).Should().Be(Allocated(direct));
        }

        SameCost(() => sink += Stat.GetMaxFP(Creature), () => sink += instance.GetMaxFP(Creature));
        SameCost(() => sink += Stat.GetDefense(Creature, CombatDamageType.Physical, AbilityType.Vitality),
            () => sink += instance.GetDefense(Creature, CombatDamageType.Physical, AbilityType.Vitality));
        SameCost(() => sink += Stat.GetAttack(Creature, AbilityType.Might, SkillType.Force),
            () => sink += instance.GetAttack(Creature, AbilityType.Might, SkillType.Force));
        SameCost(() => sink += Stat.GetEvasion(Creature, SkillType.Invalid), () => sink += instance.GetEvasion(Creature, SkillType.Invalid));
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
    /// What the hot reads cost through the facade, against the instance. Not a pass or fail: run it on its own in Release:
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
            IMimicryService mimicry,
            ITwinPistolEquipmentService twinPistolEquipment,
            IPowerGloveEquipmentService powerGloveEquipment,
            IItemService item,
            IRecastService recast,
            IBeastMasteryService beastMastery,
            IEnmityService enmity,
            IResistanceService resistance,
            IPvpCombatService pvpCombat,
            IPerkService perk,
            ISkillService skill,
            ITemporaryStatModifierService temporaryStatModifier)
            : base(db, combatRecorder, playerFeedback, statusEffect, mimicry, twinPistolEquipment, powerGloveEquipment, item, recast, beastMastery, enmity, resistance, pvpCombat,
                perk, skill, temporaryStatModifier)
        {
        }

        protected override int GetPerkStatBonus(uint creature, StatType stat) => 0;
    }
}
