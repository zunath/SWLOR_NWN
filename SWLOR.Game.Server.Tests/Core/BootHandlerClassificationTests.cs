using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Records how a hot reload treats every handler of the boot scripts, and fails when a boot handler has no
/// <see cref="BootPhaseAttribute"/> or a handler's class changes. Nothing reads the attribute at runtime yet; this
/// snapshot is the reviewed list the reload work (Phase 4) is built from. The rule behind the classes is in
/// <see cref="BootReload"/> and in <c>Readmes/MaintainabilityRefactorHotReload.md</c>.
/// </summary>
[TestFixture]
public class BootHandlerClassificationTests
{
    private const string Snapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/BootHandlerClassification.txt";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~BootHandlerClassificationTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)" +
        "   (build SWLOR.Game.Server.Tests first so the generator sees your handler)";

    /// <summary>The boot scripts in the order the server reaches them, then the two scripts that run at database load and shutdown.</summary>
    private static readonly string[] BootScripts =
    {
        ScriptName.OnModulePreload,
        ScriptName.OnDatabaseLoaded,
        ScriptName.OnModuleContentChange,
        ScriptName.OnModuleCacheBefore,
        ScriptName.OnModuleCacheAfter,
        ScriptName.OnModuleLoad,
        ScriptName.OnSWLORApplicationShutdown,
    };

    private static ScriptRegistry LoadRegistry()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { ScriptHandlerTestSupport.GameAssembly }, ScriptHandlerTestSupport.Owners);
        return registry;
    }

    private static IEnumerable<MethodInfo> BootHandlers(ScriptRegistry registry, string script)
    {
        return registry.GetActionScripts(script).Select(handler => handler.Action.Method)
            .Concat(registry.GetConditionalScripts(script).Select(handler => handler.Action.Method));
    }

    private static string ClassOf(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<BootPhaseAttribute>(false);
        return attribute == null ? "UNCLASSIFIED" : attribute.Reload.ToString();
    }

    [Test]
    public void EveryBootHandlerHasABootPhase()
    {
        var registry = LoadRegistry();

        var unclassified = BootScripts
            .SelectMany(script => BootHandlers(registry, script).Select(method => (script, name: ScriptHandlerManifest.HandlerName(method), method)))
            .Where(handler => handler.method.GetCustomAttribute<BootPhaseAttribute>(false) == null)
            .Select(handler => $"{handler.script}: {handler.name}")
            .ToArray();

        unclassified.Should().BeEmpty(
            "every handler of a boot script needs [BootPhase(BootReload.<class>)] so a hot reload knows whether the new build reruns it; " +
            "read the rule in SWLOR.Game.Server/Readmes/MaintainabilityRefactorHotReload.md section 2, then add it and regenerate the snapshot: " + RegenerateCommand);
    }

    [Test]
    public void TheBootScriptListCoversEveryBootBucket()
    {
        BootScripts.Should().Contain(ScriptHandlerOrderSnapshot.BootBuckets.Select(bucket => bucket.Script),
            "a new boot bucket in the handler order snapshot needs its handlers classified here too");
    }

    [Test]
    public void BootHandlerClassesMatchTheSnapshot()
    {
        var registry = LoadRegistry();
        var text = new StringBuilder();
        text.Append("# SWLOR boot handler classification for hot reload: SWLOR.Game.Server only\n");
        text.Append("# One line per handler of a boot script, in the order the registry runs them: class, then Namespace.Type.Method.\n");
        text.Append("# Classes (BootReload): Rerun = the new build runs it again; BootOnly = never again after boot (a mixed handler is split into the two);\n");
        text.Append("# NativeHook = installs a native hook; Retire = runs at shutdown and when a generation retires. UNCLASSIFIED fails EveryBootHandlerHasABootPhase.\n");
        text.Append("# The rule and the reasons are in SWLOR.Game.Server/Readmes/MaintainabilityRefactorHotReload.md section 2.\n");
        text.Append("# Regenerate (build first so the handlers are current): " + RegenerateCommand + "\n");

        var totals = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var script in BootScripts)
        {
            var methods = BootHandlers(registry, script).ToArray();
            text.Append('\n').Append(script).Append(" (").Append(methods.Length).Append(")\n");
            foreach (var method in methods)
            {
                var kind = ClassOf(method);
                totals[kind] = totals.GetValueOrDefault(kind) + 1;
                text.Append("  ").Append(kind.PadRight(11)).Append(' ').Append(ScriptHandlerManifest.HandlerName(method)).Append('\n');
            }
        }

        text.Append("\n== TOTALS ==\n");
        foreach (var (kind, count) in totals)
            text.Append(kind.PadRight(11)).Append(' ').Append(count).Append('\n');

        SnapshotComparison.AssertMatches(Snapshot, text.ToString(), RegenerateCommand);
    }
}
