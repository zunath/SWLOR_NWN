using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The private tables of the status effect instance in the installed container. Those were static fields of the old
/// static class that fixtures seeded by reflection; a fixture that seeds them now installs a <see cref="FacadeContainer"/>
/// and reads them here, so what it seeds is what the facades read for as long as the container is installed.
/// </summary>
public static class StatusEffectInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static Dictionary<uint, CreatureStatusEffect> CreatureEffects =>
        (Dictionary<uint, CreatureStatusEffect>)Field("_creatureEffects");

    public static IDictionary NativeAttackSwingDepth => (IDictionary)Field("_nativeAttackSwingDepth");

    public static IDictionary DeferredNativeAttackStatusEffects => (IDictionary)Field("_deferredNativeAttackStatusEffects");

    /// <summary>Runs a private method of the installed instance.</summary>
    public static TResult Invoke<TResult>(string method, params object[] arguments) =>
        (TResult)typeof(StatusEffectService).GetMethod(method, PrivateInstance)!.Invoke(Current, arguments)!;

    private static object Field(string name) => typeof(StatusEffectService).GetField(name, PrivateInstance)!.GetValue(Current)!;

    private static IStatusEffectService Current
    {
        get
        {
            var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!;
            var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                return (IStatusEffectService)resolve.MakeGenericMethod(typeof(IStatusEffectService)).Invoke(null, null)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }
    }
}
