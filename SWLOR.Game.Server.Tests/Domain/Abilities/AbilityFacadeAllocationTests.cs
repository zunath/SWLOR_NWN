using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The ability reads run on every attack and activation, so a static call that only reads a table must not allocate: the
/// accessor <c>GameServices.Ability</c> (the facade until P2-ZF) resolves its instance from the same cache the facade read and
/// the instance reads its own fields. These tests count the bytes the current thread allocates over many calls, with no
/// impact active and with one active. The queued-attack and activation reads of <c>UsePerkFeat</c> and the quick cast map
/// are here too, because they moved with the state.
/// </summary>
[TestFixture]
[NonParallelizable]
public class AbilityFacadeAllocationTests
{
    private const int Calls = 20000;
    private const uint Tracked = 701;
    private const uint Untracked = 702;
    private const FeatType Registered = FeatType.Flash1;

    private static readonly AbilityDetail Force = new()
    {
        Name = "Probe",
        SkillType = SkillType.Force,
        IsSingleTargetAbility = true,
        Requirements = new List<IAbilityActivationRequirement>()
    };

    private AbilityBench _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;

    /// <summary>
    /// The container's ability service is the bench's instance, whose calls into unconverted code answer from its own
    /// tables without allocating, so a count here is the accessor and the instance and nothing behind them.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _world = new AbilityBench();
        _container = new FacadeContainer(services => services.Replace(ServiceDescriptor.Singleton<IAbilityService>(_world.Service)));
        _installed = _container.Install();
        _world.Service.Register(Registered, Force);
        _world.Service.BeginAbilityImpact(Tracked, Force);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
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
    public void The_catalog_reads_allocate_nothing()
    {
        var sink = 0;

        Allocated(() => sink += Ability.IsFeatRegistered(Registered) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Ability.IsFeatRegistered(FeatType.Invalid) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Ability.GetAbilityDetail(Registered).AbilityLevel).Should().Be(0);
        Allocated(() => sink += Ability.GetAllAbilityDetails().Count).Should().Be(0);
        Allocated(() => sink += Ability.GetLastActivationDenialReason().Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void The_impact_reads_allocate_nothing_with_or_without_an_active_impact()
    {
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += Ability.GetAbilityImpactSequence(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.GetActiveAbilityImpactSummary(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.GetLastCompletedAbilityImpactSummary(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.GetWeaponTraitAccuracy(creature)).Should().Be(0);
            Allocated(() => sink += (int)Ability.GetSuccessfulImpactVisualEffect(creature)).Should().Be(0);
            Allocated(() => sink += Ability.IsAreaImpact(creature, Force) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.IsSingleTargetImpact(creature, Force) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.TryTriggerAreaAbilityPulse(creature) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.ApplyCapturedAbilityDamageBonus(creature, 5)).Should().Be(0);
            Allocated(() => sink += Ability.ApplyCombatReadinessToActivatedAbilityMagnitude(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)Ability.GetActiveForceAffinityMagnitudeMultiplier(creature)).Should().Be(0);
            Allocated(() => sink += (int)Ability.GetActiveForceAffinityRestorativeMagnitudeMultiplier(creature)).Should().Be(0);
            Allocated(() => sink += Ability.ApplyActiveForceAffinityMagnitude(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)Ability.ApplyActiveForceAffinityMagnitude(creature, 5f)).Should().Be(0);
            Allocated(() => sink += Ability.ApplyActiveForceAffinityRestorativeMagnitude(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)Ability.GetWeaponAbilityDamageType(creature, SkillType.Force, CombatDamageType.Fire)).Should().Be(0);
            Allocated(() => Ability.AddActiveAbilityDefenseIgnorePercentAdjustment(creature, 0)).Should().Be(0);
        }

        GC.KeepAlive(sink);
    }

    [Test]
    public void The_stat_source_read_allocates_nothing()
    {
        _world.Service.StatSources = new[] { new StatAdjustmentSource("source", new Dictionary<StatType, int>()) };
        var sink = 0;

        Allocated(() => sink += Ability.GetAbilityImpactStatSources(Tracked, StatType.Attack).Count).Should().Be(0);
        Allocated(() => sink += Ability.GetAbilityImpactStatSources(Untracked, StatType.Attack).Count).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void The_pure_rules_allocate_nothing()
    {
        var sink = 0;

        Allocated(() => sink += Ability.IsForcePower(false, SkillType.Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Ability.IsForcePower(Untracked, Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Ability.CalculateHostileAbilityEnmity(5, 0, 1)).Should().Be(0);
        Allocated(() => sink += Ability.IsAreaImpact(null!, Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += Ability.ApplyCombatReadinessMagnitude(Untracked, 0)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    [Test]
    public void The_activation_and_queued_attack_reads_allocate_nothing()
    {
        var active = new AbilityActiveActivation { ActivationId = "id", Ability = Force };
        var traits = new WeaponAbilityTraitActivation(new PairedRangedAbilityTraitsSnapshot(Guid.Empty, true, true, 1, 3), 5) { IsCommitted = true };
        var reservation = new AbilityQueuedWeaponAttack { AttackIndex = 1, Weapon = 800 };
        Ability.SetActiveAbilityActivation(Tracked, active);
        Ability.SetQueuedWeaponTraits(Tracked, traits);
        Ability.TryAddQueuedWeaponAttack(Tracked, reservation);
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += Ability.TryGetActiveAbilityActivation(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.TryGetQueuedWeaponTraits(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += Ability.TryGetQueuedWeaponAttack(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += UsePerkFeat.GetActiveWeaponTraits(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += UsePerkFeat.GetQueuedWeaponTraitAccuracy(creature)).Should().Be(0);
            Allocated(() => UsePerkFeat.BeginWeaponAttackRoll(creature)).Should().Be(0);
        }

        Ability.RemoveActiveAbilityActivation(Tracked);
        Ability.RemoveQueuedWeaponTraits(Tracked);
        Ability.RemoveQueuedWeaponAttack(Tracked);
        GC.KeepAlive(sink);
    }

    [Test]
    public void The_quick_cast_map_reads_allocate_nothing()
    {
        var (alias, original) = QuickCastFeatMap.Originals.First();
        var sink = 0;

        Allocated(() => sink += QuickCastFeatMap.IsAlias(alias) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += (int)QuickCastFeatMap.GetOriginal(alias)).Should().Be(0);
        Allocated(() => sink += QuickCastFeatMap.TryGetAlias(original, out _) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += QuickCastFeatMap.Originals.Count).Should().Be(0);
        GC.KeepAlive(sink);
    }
}
