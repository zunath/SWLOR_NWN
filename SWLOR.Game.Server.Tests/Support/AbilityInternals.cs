using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Space;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Reaches the private members of the ability instance, which the tests used to reach as private statics of the
/// <c>Ability</c> class. <see cref="NoNeighbours"/> is an instance for a test of pure rules or of the impact tracker's
/// own data: its constructor only assigns, so it runs until something calls a neighbour. Its impact state store, geometry,
/// magnitude, lifecycle, per-target and damage parts (the ability split, <c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>)
/// are real instances over one store, so a test that puts a tracker in the store (<see cref="TrackedImpacts"/>) sees the
/// instance read it.
/// </summary>
public static class AbilityInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The type that tracks one impact while it resolves (the nested <c>TrackedAbilityImpact</c> before the split).</summary>
    public static Type TrackedImpactType { get; } = typeof(AbilityImpactTracker);

    /// <summary>An instance with no neighbours.</summary>
    public static AbilityService NoNeighbours()
    {
        var impactState = new AbilityImpactStore();
        var magnitude = new CombatAbilityMagnitudeService(impactState, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>());
        var darkForce = new AbilityDarkForceService(impactState, null!, FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICombatResourcesService>());
        var damageInput = new AbilityDamageInputService(impactState, FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ICombatRatingService>(), FacadeForwarder.For<ICombatActivityService>(),
            FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityImpactService>(),
            FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatRepeatedTargetService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatDamageReflectionService>(), magnitude, null!);
        var damageDelivery = new AbilityDamageDeliveryService(null!, null!, darkForce, FacadeForwarder.For<ICombatFeedbackService>(),
            FacadeForwarder.For<ICombatRepeatedTargetService>(), FacadeForwarder.For<ICombatHitEffectsService>(), FacadeForwarder.For<ICombatDamageReflectionService>(),
            FacadeForwarder.For<ICombatAbilityBonusService>());
        var statusApplication = new AbilityStatusApplicationService(null!, null!, null!, impactState, FacadeForwarder.For<ICombatAbilityBonusService>());
        var damageStage = new AbilityDamageStageService(damageInput, impactState, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<ICombatAbilityHitService>(),
            FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatDamageReflectionService>(), FacadeForwarder.For<ICombatFeedbackService>(),
            FacadeForwarder.For<ICombatCriticalHitEffectsService>(), magnitude, null!);
        var impactMessage = new AbilityImpactMessageService(null!, null!, FacadeForwarder.For<ICombatFeedbackService>());
        var impactVisual = new AbilityImpactVisualService(null!);
        var observers = Array.Empty<IAbilityImpactObserver>();
        var lifecycle = new AbilityImpactLifecycleService(impactState, null!, null!, damageDelivery, null!, FacadeForwarder.For<ICombatQueriesService>(),
            FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityImpactService>(), FacadeForwarder.For<ICombatResourcesService>(),
            FacadeForwarder.For<ICombatCriticalHitEffectsService>());
        var hostileImpact = new AbilityHostileImpactService(impactState, observers, null!, null!, FacadeForwarder.For<ICombatResourcesService>(),
            FacadeForwarder.For<ICombatAbilityImpactService>(), damageDelivery, statusApplication, darkForce, impactVisual);
        var hitRoll = new AbilityHitRollService(observers, null!, damageInput, statusApplication, impactMessage, null!, null!,
            FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityHitService>());
        var targetImpact = new AbilityTargetImpactService(impactState, hitRoll, hostileImpact, darkForce, damageInput, damageStage,
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityImpactService>(), FacadeForwarder.For<ICombatAbilityBonusService>());
        return new(impactState, lifecycle, targetImpact, impactMessage, impactVisual, Geometry(), null!, null!,
            FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.For<ICombatAbilityImpactService>());
    }

    /// <summary>A geometry part with no telegraph service, for a test of the shape tests that resolve no marker.</summary>
    public static CombatImpactGeometryService Geometry() => new(null!);

    /// <summary>A new tracker; the arguments are its constructor's.</summary>
    public static object NewTrackedImpact(params object?[] arguments) =>
        TrackedImpactType.GetConstructors().Single().Invoke(arguments);

    /// <summary>The impact state store of <paramref name="service"/>.</summary>
    public static AbilityImpactStore ImpactState(AbilityService service) =>
        (AbilityImpactStore)typeof(AbilityService).GetField("_impactState", PrivateInstance)!.GetValue(service)!;

    /// <summary>The trackers of the impacts in progress, by activator, in the impact state store of <paramref name="service"/>.</summary>
    public static IDictionary TrackedImpacts(AbilityService service) => TrackedImpacts(ImpactState(service));

    /// <summary>The trackers of the impacts in progress, by activator, in a store.</summary>
    public static IDictionary TrackedImpacts(AbilityImpactStore store) =>
        (IDictionary)typeof(AbilityImpactStore).GetField("_trackedAbilityImpacts", PrivateInstance)!.GetValue(store)!;

    /// <summary>A private method of the instance, for a test to invoke on an instance of its own.</summary>
    public static MethodInfo Method(string name) =>
        typeof(AbilityService).GetMethod(name, PrivateInstance)
        ?? throw new MissingMethodException(nameof(AbilityService), name);

    /// <summary>A private method of the geometry part, for a test to invoke on <see cref="Geometry"/>.</summary>
    public static MethodInfo GeometryMethod(string name) =>
        typeof(CombatImpactGeometryService).GetMethod(name, PrivateInstance)
        ?? throw new MissingMethodException(nameof(CombatImpactGeometryService), name);

    /// <summary>A private method of <paramref name="service"/> as a delegate.</summary>
    public static TDelegate Delegate<TDelegate>(AbilityService service, string name)
        where TDelegate : Delegate =>
        Method(name).CreateDelegate<TDelegate>(service);

    /// <summary>The impact lifecycle part (part 5) of <paramref name="service"/>.</summary>
    public static AbilityImpactLifecycleService Lifecycle(AbilityService service) =>
        (AbilityImpactLifecycleService)typeof(AbilityService).GetField("_lifecycle", PrivateInstance)!.GetValue(service)!;

    /// <summary>The per-target resolution part (part 11) of <paramref name="service"/>.</summary>
    public static AbilityTargetImpactService TargetImpact(AbilityService service) =>
        (AbilityTargetImpactService)typeof(AbilityService).GetField("_targetImpact", PrivateInstance)!.GetValue(service)!;

    /// <summary>The damage inputs part (part 16) of <paramref name="service"/>, which its per-target resolution reads.</summary>
    public static AbilityDamageInputService DamageInput(AbilityService service) =>
        (AbilityDamageInputService)typeof(AbilityTargetImpactService).GetField("_damageInput", PrivateInstance)!.GetValue(TargetImpact(service))!;

    /// <summary>A private method of a part of the ability split as a delegate on that part.</summary>
    public static TDelegate PartDelegate<TDelegate>(object part, string name)
        where TDelegate : Delegate =>
        (part.GetType().GetMethod(name, PrivateInstance) ?? throw new MissingMethodException(part.GetType().Name, name))
        .CreateDelegate<TDelegate>(part);
}
