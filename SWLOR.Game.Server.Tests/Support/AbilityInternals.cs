using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Reaches the private members of the ability instance, which the tests used to reach as private statics of the
/// <c>Ability</c> class. <see cref="NoNeighbours"/> is an instance for a test of pure rules or of the impact tracker's
/// own data: its constructor only assigns, so it runs until something calls a neighbour.
/// </summary>
public static class AbilityInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The private nested type that tracks one impact while it resolves.</summary>
    public static Type TrackedImpactType { get; } =
        typeof(AbilityService).GetNestedType("TrackedAbilityImpact", BindingFlags.NonPublic)!;

    /// <summary>An instance with no neighbours.</summary>
    public static AbilityService NoNeighbours() =>
        new(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!,
            FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IPerkService>());

    /// <summary>An instance whose only neighbour is the party, for a test of party changes that reach the aura code.</summary>
    public static AbilityService WithParty(IPartyService party) =>
        new(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, party, null!, null!, null!, null!, null!, null!, null!,
            FacadeForwarder.For<IStatService>(), FacadeForwarder.For<IPerkService>());

    /// <summary>A new tracker of <paramref name="service"/>; the arguments follow the service in its constructor.</summary>
    public static object NewTrackedImpact(AbilityService service, params object?[] arguments)
    {
        var all = new object?[arguments.Length + 1];
        all[0] = service;
        arguments.CopyTo(all, 1);
        return TrackedImpactType.GetConstructors().Single().Invoke(all);
    }

    /// <summary>The trackers of the impacts in progress, by activator.</summary>
    public static IDictionary TrackedImpacts(AbilityService service) =>
        (IDictionary)typeof(AbilityService).GetField("_trackedAbilityImpacts", PrivateInstance)!.GetValue(service)!;

    /// <summary>A private method of the instance, for a test to invoke on an instance of its own.</summary>
    public static MethodInfo Method(string name) =>
        typeof(AbilityService).GetMethod(name, PrivateInstance)
        ?? throw new MissingMethodException(nameof(AbilityService), name);

    /// <summary>A private method of <paramref name="service"/> as a delegate.</summary>
    public static TDelegate Delegate<TDelegate>(AbilityService service, string name)
        where TDelegate : Delegate =>
        Method(name).CreateDelegate<TDelegate>(service);
}
