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
/// own data: its constructor only assigns, so it runs until something calls a neighbour. Its impact state store, geometry and
/// magnitude parts (the ability split, <c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) are real instances over one store,
/// so a test that puts a tracker in the store (<see cref="TrackedImpacts"/>) sees the instance read it.
/// </summary>
public static class AbilityInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The type that tracks one impact while it resolves (the nested <c>TrackedAbilityImpact</c> before the split).</summary>
    public static Type TrackedImpactType { get; } = typeof(AbilityImpactTracker);

    /// <summary>An instance with no neighbours.</summary>
    public static AbilityService NoNeighbours() => WithParty(null!);

    /// <summary>An instance whose only neighbour is the party, for a test of party changes that reach the aura code.</summary>
    public static AbilityService WithParty(IPartyService party)
    {
        var impactState = new AbilityImpactStore();
        return new(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, party, null!, null!, null!, null!, null!, null!,
            FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IAbilityCatalog>(), impactState, Geometry(),
            new CombatAbilityMagnitudeService(impactState, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatService>()),
            FacadeForwarder.For<ICombatAbilityBonusService>(), FacadeForwarder.For<ICombatAbilityHitService>(), FacadeForwarder.For<ICombatAbilityImpactService>(), FacadeForwarder.For<ICombatActivityService>(), FacadeForwarder.For<ICombatDamageModifiersService>(), FacadeForwarder.For<ICombatDamageTakenService>(), FacadeForwarder.For<ICombatFeedbackService>(), FacadeForwarder.For<ICombatHitEffectsService>(), FacadeForwarder.For<ICombatQueriesService>(), FacadeForwarder.For<ICombatResourcesService>(), FacadeForwarder.LazyFor<ISpaceService>());
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
}
