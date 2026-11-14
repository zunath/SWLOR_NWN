using System.Collections;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The private state and helpers of the temporary stat modifier instance in the installed container. They were static
/// members of the old static class that fixtures reached by reflection; a fixture that needs them installs a
/// <see cref="FacadeContainer"/> and reads them here, so what it seeds is what the facade reads.
/// </summary>
public static class TemporaryStatModifierInternals
{
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static IDictionary Modifiers =>
        (IDictionary)typeof(TemporaryStatModifierService).GetField("_modifiers", PrivateInstance)!.GetValue(Current)!;

    /// <summary>Runs a private method of the installed instance.</summary>
    public static object? Invoke(string method, params object[] arguments) =>
        typeof(TemporaryStatModifierService).GetMethod(method, PrivateInstance)!.Invoke(Current, arguments);

    /// <summary>The modifier table of a given instance.</summary>
    public static IDictionary ModifiersOf(TemporaryStatModifierService service) =>
        (IDictionary)typeof(TemporaryStatModifierService).GetField("_modifiers", PrivateInstance)!.GetValue(service)!;

    private static ITemporaryStatModifierService Current
    {
        get
        {
            var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true)!;
            var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic)!;
            try
            {
                return (ITemporaryStatModifierService)resolve.MakeGenericMethod(typeof(ITemporaryStatModifierService)).Invoke(null, null)!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
        }
    }
}
