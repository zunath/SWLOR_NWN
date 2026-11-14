using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// What the script handler manifest tests share: the assemblies they scan, the handler methods of an assembly in
/// today's discovery order, the manifest text the server ships, and the resolver the server passes for handlers on
/// instance methods.
/// </summary>
internal static class ScriptHandlerTestSupport
{
    public const string ManifestRepositoryPath = "SWLOR.Game.Server/Core/ScriptHandlerOrder.txt";

    private static readonly Lazy<ContainerHost> Container = new(() =>
    {
        var host = new ContainerHost();
        CompositionRoot.Build(host);
        return host;
    });

    public static Assembly GameAssembly => typeof(ScriptRegistry).Assembly;

    public static Assembly EngineTestAssembly => Assembly.Load(new AssemblyName("SWLOR.Game.Server.EngineTests"));

    /// <summary>A container built from the server's registrations, as the boot builds it (one per test run).</summary>
    public static ContainerHost ServerContainer => Container.Value;

    /// <summary>
    /// What <c>ServerBootstrapper.LoadScripts</c> passes the registry: the container's singleton of a class that holds
    /// handlers on instance methods (a converted service whose facade was removed).
    /// </summary>
    public static Func<Type, object> Owners => ServerContainer.ResolveHandlerOwner;

    /// <summary>
    /// <see cref="Owners"/>, and a new instance of a class of this test assembly (its instance-handler fixtures), for the
    /// tests that scan every assembly loaded in the AppDomain.
    /// </summary>
    public static Func<Type, object> OwnersAndTestFixtures =>
        type => ServerContainer.ResolveHandlerOwner(type) ?? (type.Assembly == typeof(ScriptHandlerTestSupport).Assembly ? Activator.CreateInstance(type) : null);

    /// <summary>Every public method of every type, in the order reflection returns them (what discovery walked before the manifest).</summary>
    public static IReadOnlyList<MethodInfo> MethodsOf(params Assembly[] assemblies)
    {
        return assemblies.SelectMany(a => a.GetTypes()).SelectMany(t => t.GetMethods()).ToArray();
    }

    public static bool IsHandler(MethodInfo method)
    {
        return method.GetCustomAttributes(typeof(NWNEventHandler), false).Length > 0;
    }

    /// <summary>Full names of the handlers the registry runs for a script, in run order.</summary>
    public static IReadOnlyList<string> Names(ScriptRegistry registry, string script, bool conditional)
    {
        return conditional
            ? registry.GetConditionalScripts(script).Select(h => ScriptHandlerManifest.HandlerName(h.Action.Method)).ToArray()
            : registry.GetActionScripts(script).Select(h => ScriptHandlerManifest.HandlerName(h.Action.Method)).ToArray();
    }

    /// <summary>The manifest text embedded in the game assembly, exactly as the server reads it.</summary>
    public static string ShippedManifestText()
    {
        using var stream = GameAssembly.GetManifestResourceStream(ScriptHandlerManifest.ResourceName);
        if (stream == null)
            throw new InvalidOperationException($"{ScriptHandlerManifest.ResourceName} is not embedded in the game assembly.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
