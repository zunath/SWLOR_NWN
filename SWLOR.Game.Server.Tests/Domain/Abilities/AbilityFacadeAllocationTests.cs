using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The ability reads run on every attack and activation, so a static call that only reads a table must not allocate: the
/// accessor <c>GameServices.Ability</c> resolves its instance from a per-service slot and
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
    /// The container's ability service and its parts (catalog, impact state store, magnitude, activation, aura, immunity, activation state,
    /// the damage parts, the lifecycle, the per-target parts and the visual part) are the bench's, whose calls into unconverted code answer from its own
    /// tables without allocating, so a count here is the accessor and the instance and nothing behind them.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        _world = new AbilityBench();
        _container = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IAbilityService>(_world.Service));
            services.Replace(ServiceDescriptor.Singleton<IAbilityCatalogService>(_world.Catalog));
            services.Replace(ServiceDescriptor.Singleton<IAbilityImpactState>(_world.ImpactState));
            services.Replace(ServiceDescriptor.Singleton<ICombatAbilityMagnitudeService>(_world.Magnitude));
            services.Replace(ServiceDescriptor.Singleton<IAbilityAuraService>(_world.Aura));
            services.Replace(ServiceDescriptor.Singleton<IAbilityImmunityService>(_world.Immunity));
            services.Replace(ServiceDescriptor.Singleton<IAbilityActivationStateService>(_world.ActivationState));
            services.Replace(ServiceDescriptor.Singleton<IAbilityActivationService>(_world.Activation));
            services.Replace(ServiceDescriptor.Singleton<IAbilityImpactMessageService>(_world.ImpactMessage));
            services.Replace(ServiceDescriptor.Singleton<IAbilityDarkForceService>(_world.DarkForce));
            services.Replace(ServiceDescriptor.Singleton<IAbilityDamageDeliveryService>(_world.DamageDelivery));
            services.Replace(ServiceDescriptor.Singleton<IAbilityStatusApplicationService>(_world.StatusApplication));
            services.Replace(ServiceDescriptor.Singleton<IAbilityDamageInputService>(_world.DamageInput));
            services.Replace(ServiceDescriptor.Singleton<IAbilityDamageStageService>(_world.DamageStage));
            services.Replace(ServiceDescriptor.Singleton<IAbilityImpactLifecycleService>(_world.Lifecycle));
            services.Replace(ServiceDescriptor.Singleton<IAbilityHitRollService>(_world.HitRoll));
            services.Replace(ServiceDescriptor.Singleton<IAbilityTargetImpactService>(_world.TargetImpact));
            services.Replace(ServiceDescriptor.Singleton<IAbilityHostileImpactService>(_world.HostileImpact));
            services.Replace(ServiceDescriptor.Singleton<IAbilityImpactVisualService>(_world.ImpactVisual));
        });
        _installed = _container.Install();
        _world.Catalog.Register(Registered, Force);
        _world.Lifecycle.BeginAbilityImpact(Tracked, Force);
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

    // The reads of IAbilityCatalog (GameServices.AbilityCatalog after P5-3b) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_catalog_reads_allocate_nothing()
    {
        var sink = 0;

        Allocated(() => sink += AbilityCatalog.IsFeatRegistered(Registered) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += AbilityCatalog.IsFeatRegistered(FeatType.Invalid) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += AbilityCatalog.GetAbilityDetail(Registered).AbilityLevel).Should().Be(0);
        Allocated(() => sink += AbilityCatalog.GetAllAbilityDetails().Count).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The reads of IAbilityActivationService (GameServices.AbilityActivation since P5-3d) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_last_denial_read_allocates_nothing()
    {
        var sink = 0;

        Allocated(() => sink += AbilityActivation.GetLastActivationDenialReason().Length).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The reads of IAbilityImpactState (GameServices.AbilityImpactState after P5-3b) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_impact_state_reads_allocate_nothing_with_or_without_an_active_impact()
    {
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += AbilityImpactState.GetAbilityImpactSequence(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.GetActiveAbilityImpactSummary(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.GetLastCompletedAbilityImpactSummary(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.GetWeaponTraitAccuracy(creature)).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.IsAreaImpact(creature, Force) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.IsSingleTargetImpact(creature, Force) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityImpactState.TryTriggerAreaAbilityPulse(creature) ? 1 : 0).Should().Be(0);
            Allocated(() => AbilityImpactState.AddActiveAbilityDefenseIgnorePercentAdjustment(creature, 0)).Should().Be(0);
        }

        GC.KeepAlive(sink);
    }

    // The reads of ICombatAbilityMagnitudeService (GameServices.CombatAbilityMagnitude after P5-3b) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_magnitude_reads_allocate_nothing_with_or_without_an_active_impact()
    {
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += CombatAbilityMagnitude.ApplyCombatReadinessToActivatedAbilityMagnitude(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)CombatAbilityMagnitude.GetActiveForceAffinityMagnitudeMultiplier(creature)).Should().Be(0);
            Allocated(() => sink += (int)CombatAbilityMagnitude.GetActiveForceAffinityRestorativeMagnitudeMultiplier(creature)).Should().Be(0);
            Allocated(() => sink += CombatAbilityMagnitude.ApplyActiveForceAffinityMagnitude(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)CombatAbilityMagnitude.ApplyActiveForceAffinityMagnitude(creature, 5f)).Should().Be(0);
            Allocated(() => sink += CombatAbilityMagnitude.ApplyActiveForceAffinityRestorativeMagnitude(creature, 5)).Should().Be(0);
        }

        GC.KeepAlive(sink);
    }

    // The reads of IAbilityService (stays), IAbilityHostileImpactService (GameServices.AbilityHostileImpact) and IAbilityDamageInputService
    // (GameServices.AbilityDamageInput, the weapon damage type) since P5-3f (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_impact_reads_allocate_nothing_with_or_without_an_active_impact()
    {
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += (int)Ability.GetSuccessfulImpactVisualEffect(creature)).Should().Be(0);
            Allocated(() => sink += AbilityHostileImpact.ApplyCapturedAbilityDamageBonus(creature, 5)).Should().Be(0);
            Allocated(() => sink += (int)AbilityDamageInput.GetWeaponAbilityDamageType(creature, SkillType.Force, CombatDamageType.Fire)).Should().Be(0);
        }

        GC.KeepAlive(sink);
    }

    // The reads of IAbilityImpactState (GameServices.AbilityImpactState after P5-3b) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_stat_source_read_allocates_nothing()
    {
        _world.StatSources = new[] { new StatAdjustmentSource("source", new Dictionary<StatType, int>()) };
        var sink = 0;

        Allocated(() => sink += AbilityImpactState.GetAbilityImpactStatSources(Tracked, StatType.Attack, _world.Stat).Count).Should().Be(0);
        Allocated(() => sink += AbilityImpactState.GetAbilityImpactStatSources(Untracked, StatType.Attack, _world.Stat).Count).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The per-target rules of the damage inputs and the status application (P5-3e) that answer before any engine call, read on
    // the bench's parts: the hostile impact calls them through the parts' interfaces, not through GameServices.
    [Test]
    public void The_damage_part_rules_allocate_nothing_with_or_without_an_active_impact()
    {
        IAbilityDamageInputService damageInput = _world.DamageInput;
        IAbilityStatusApplicationService statusApplication = _world.StatusApplication;
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            var impact = _world.ImpactState.GetTrackedAbilityImpact(creature);
            Allocated(() => sink += damageInput.ShouldUseNPCStatScaling(creature, false) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += damageInput.UsesQueuedNaturalWeapon(impact, SkillType.BeastMastery) ? 1 : 0).Should().Be(0);
        }

        Allocated(() => sink += (int)statusApplication.GetCombatImpactStatusCategories(null!, null!, null!)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The reads of CombatRules and ICombatQueriesService (IsForcePower), IAbilityHostileImpactService, IAbilityImpactState and ICombatAbilityMagnitudeService (P5-3b and P5-3f) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_pure_rules_allocate_nothing()
    {
        var sink = 0;

        Allocated(() => sink += CombatRules.IsForcePower(false, SkillType.Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += CombatQueries.IsForcePower(Untracked, Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += AbilityHostileImpact.CalculateHostileAbilityEnmity(5, 0, 1)).Should().Be(0);
        Allocated(() => sink += AbilityImpactState.IsAreaImpact(null!, Force) ? 1 : 0).Should().Be(0);
        Allocated(() => sink += CombatAbilityMagnitude.ApplyCombatReadinessMagnitude(Untracked, 0)).Should().Be(0);
        GC.KeepAlive(sink);
    }

    // The reads of IAbilityActivationStateService (GameServices.AbilityActivationState after P5-3c) (Readmes/MaintainabilityRefactorAbilitySplit.md, "Every member").
    [Test]
    public void The_activation_and_queued_attack_reads_allocate_nothing()
    {
        var active = new AbilityActiveActivation { ActivationId = "id", Ability = Force };
        var traits = new WeaponAbilityTraitActivation(new PairedRangedAbilityTraitsSnapshot(Guid.Empty, true, true, 1, 3), 5) { IsCommitted = true };
        var reservation = new AbilityQueuedWeaponAttack { AttackIndex = 1, Weapon = 800 };
        AbilityActivationState.SetActiveAbilityActivation(Tracked, active);
        AbilityActivationState.SetQueuedWeaponTraits(Tracked, traits);
        AbilityActivationState.TryAddQueuedWeaponAttack(Tracked, reservation);
        var sink = 0;

        foreach (var creature in new[] { Tracked, Untracked })
        {
            Allocated(() => sink += AbilityActivationState.TryGetActiveAbilityActivation(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityActivationState.TryGetQueuedWeaponTraits(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += AbilityActivationState.TryGetQueuedWeaponAttack(creature, out _) ? 1 : 0).Should().Be(0);
            Allocated(() => sink += UsePerkFeat.GetActiveWeaponTraits(creature) == null ? 1 : 0).Should().Be(0);
            Allocated(() => sink += UsePerkFeat.GetQueuedWeaponTraitAccuracy(creature)).Should().Be(0);
            Allocated(() => UsePerkFeat.BeginWeaponAttackRoll(creature)).Should().Be(0);
        }

        AbilityActivationState.RemoveActiveAbilityActivation(Tracked);
        AbilityActivationState.RemoveQueuedWeaponTraits(Tracked);
        AbilityActivationState.RemoveQueuedWeaponAttack(Tracked);
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
