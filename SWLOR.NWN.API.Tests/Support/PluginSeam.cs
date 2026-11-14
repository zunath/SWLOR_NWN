using System.Reflection;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Support;

/// <summary>
/// One NWNX plugin seam, found by naming convention: the static wrapper `ChatPlugin`, its interface `IChatPlugin`, the
/// pass-through `NwnCoreChatPlugin`, the runtime `ChatPluginRuntime` and the fake `FakeChatPlugin`. Discovery starts from the
/// static wrapper classes, so a plugin added without running the generator fails every test that uses this list.
/// </summary>
public sealed class PluginSeam
{
    private const string PluginNamespace = "SWLOR.NWN.API.NWNX";
    private const string FakeNamespace = "SWLOR.NWN.API.Testing";

    private PluginSeam(Type wrapper)
    {
        Wrapper = wrapper;
        Interface = Require(wrapper.Assembly, $"{PluginNamespace}.I{wrapper.Name}");
        PassThrough = Require(wrapper.Assembly, $"{PluginNamespace}.NwnCore{wrapper.Name}");
        Runtime = Require(wrapper.Assembly, $"{PluginNamespace}.{wrapper.Name}Runtime");
        Fake = Require(typeof(FakeEngine).Assembly, $"{FakeNamespace}.Fake{wrapper.Name}");
    }

    /// <summary>Every plugin seam, ordered by plugin name.</summary>
    public static IReadOnlyList<PluginSeam> All { get; } = typeof(SWLOR.NWN.API.NWNX.ChatPlugin).Assembly.GetTypes()
        .Where(t => t.Namespace == PluginNamespace && t.IsAbstract && t.IsSealed && t.Name.EndsWith("Plugin", StringComparison.Ordinal))
        .OrderBy(t => t.Name, StringComparer.Ordinal)
        .Select(t => new PluginSeam(t))
        .ToList();

    /// <summary>The hand-written static wrapper, e.g. `ChatPlugin`.</summary>
    public Type Wrapper { get; }

    /// <summary>The generated interface, e.g. `IChatPlugin`.</summary>
    public Type Interface { get; }

    /// <summary>The generated implementation that holds the original bodies and calls the engine, e.g. `NwnCoreChatPlugin`.</summary>
    public Type PassThrough { get; }

    /// <summary>The generated static runtime, e.g. `ChatPluginRuntime`.</summary>
    public Type Runtime { get; }

    /// <summary>The generated recording fake, e.g. `FakeChatPlugin`.</summary>
    public Type Fake { get; }

    public string Name => Wrapper.Name;

    /// <summary>The engine the wrappers call right now.</summary>
    public object Current => Runtime.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    public FakeEngine NewFake() => (FakeEngine)Activator.CreateInstance(Fake)!;

    /// <summary>Installs <paramref name="engine"/> as the plugin's engine; dispose the scope to restore the previous one.</summary>
    public IDisposable Override(object engine)
    {
        try
        {
            return (IDisposable)Runtime.GetMethod("Override", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new[] { engine })!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    public override string ToString() => Name;

    private static Type Require(Assembly assembly, string name)
    {
        return assembly.GetType(name) ?? throw new InvalidOperationException(
            $"{name} was not found. Run tools/Refactor/Apply-EngineSeam.ps1 to generate the seam of a new or renamed plugin wrapper.");
    }
}
