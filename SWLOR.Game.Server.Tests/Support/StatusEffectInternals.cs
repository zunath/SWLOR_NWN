using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The private tables of the status effect instances in the installed container. Those were static fields of the old
/// static class that fixtures seeded by reflection; a fixture that seeds them now installs a <see cref="FacadeContainer"/>
/// and reads them here, so what it seeds is what the facades read for as long as the container is installed.
/// <para>
/// One type constant per part of the status effect split (<c>Readmes/MaintainabilityRefactorStatusEffectSplit.md</c>) names the
/// class that holds the member, and one resolver per part finds its instance, so a pull request that moves a part changes
/// that part's line and no test.
/// </para>
/// </summary>
public static class StatusEffectInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>Part 1, <c>StatusEffectService</c>: the trackers (<c>_creatureEffects</c>) and the removal.</summary>
    public static readonly Type CoreType = typeof(StatusEffectService);

    /// <summary>Part 5, the native effect side: <c>GetStatusEffectDurationSeconds</c>.</summary>
    public static readonly Type NativeType = typeof(StatusEffectService);

    /// <summary>Part 6, the combat events: the native attack swing depth and the deferred list.</summary>
    public static readonly Type CombatEventsType = typeof(StatusEffectCombatEventService);

    /// <summary>Part 7, the session: <c>_loggedOutPlayerEffects</c> and its private nested <c>LoggedOutStatusEffects</c>.</summary>
    public static readonly Type SessionType = typeof(StatusEffectSessionService);

    /// <summary>The static class that declares the leadership damage reduction members.</summary>
    public static readonly Type LeadershipType = typeof(LeadershipDamageReductionRules);

    public static Dictionary<uint, CreatureStatusEffect> CreatureEffects =>
        (Dictionary<uint, CreatureStatusEffect>)Field(CoreType, "_creatureEffects", Core);

    public static IDictionary NativeAttackSwingDepth => (IDictionary)Field(CombatEventsType, "_nativeAttackSwingDepth", CombatEvents);

    public static IDictionary DeferredNativeAttackStatusEffects =>
        (IDictionary)Field(CombatEventsType, "_deferredNativeAttackStatusEffects", CombatEvents);

    /// <summary>The field of the logged-out effects on a session instance, for the carry tests that build their own owner.</summary>
    public static FieldInfo LoggedOutField => SessionType.GetField("_loggedOutPlayerEffects", PrivateInstance)!;

    /// <summary>Runs a private method of the native part in the installed container.</summary>
    public static TResult InvokeNative<TResult>(string method, params object[] arguments) =>
        Invoke<TResult>(NativeType, method, Native, arguments);

    /// <summary>Runs a method of the leadership damage reduction rules, which are static.</summary>
    public static TResult InvokeLeadership<TResult>(string method, params object[] arguments) =>
        (TResult)LeadershipType.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, arguments)!;

    private static TResult Invoke<TResult>(Type owner, string method, object instance, object[] arguments) =>
        (TResult)owner.GetMethod(method, PrivateInstance)!.Invoke(instance, arguments)!;

    private static object Field(Type owner, string name, object instance) => owner.GetField(name, PrivateInstance)!.GetValue(instance)!;

    // One resolver per part; each is the one line a part's move changes.
    private static object Core => Resolve<IStatusEffectService>();

    private static object Native => Resolve<IStatusEffectService>();

    private static object CombatEvents => Resolve<IStatusEffectCombatEventService>();

    private static object Resolve<TInterface>()
    {
        var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!;
        var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            return resolve.MakeGenericMethod(typeof(TInterface)).Invoke(null, null)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }
}
