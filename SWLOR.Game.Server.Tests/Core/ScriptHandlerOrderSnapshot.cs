using System.Reflection;
using System.Text;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Renders what a <see cref="ScriptRegistry"/> holds as the canonical text snapshot compared by
/// <see cref="ScriptHandlerOrderTests"/>. It only reads the registry through the same lookups the server uses
/// to run handlers (<see cref="ScriptRegistry.GetActionScripts"/> and <see cref="ScriptRegistry.GetConditionalScripts"/>);
/// it never decides an order itself.
/// </summary>
public static class ScriptHandlerOrderSnapshot
{
    /// <summary>
    /// The boot buckets in the order the server reaches them. The order and the descriptions are a record of
    /// today's wiring (see <see cref="ScriptHandlerOrderTests"/>); the handlers inside each bucket come from the registry.
    /// </summary>
    public static readonly IReadOnlyList<(string Script, string Description)> BootBuckets = new[]
    {
        (ScriptName.OnModulePreload,
            "run by the engine before the module loads (NWNX_UTIL_PRE_MODULE_START_SCRIPT=mod_preload in Docker/swlor.env)"),
        (ScriptName.OnModuleContentChange,
            "run by EventRegistration.OnModulePreload through ExecuteScript, only when the module file changed since the last boot"),
        (ScriptName.OnModuleCacheBefore,
            "run by EventRegistration.OnModulePreload through ExecuteScript"),
        (ScriptName.OnModuleCacheAfter,
            "run by EventRegistration.OnModulePreload through ExecuteScript, after every mod_cache_bef handler"),
        (ScriptName.OnModuleLoad,
            "run by the engine as the module OnModuleLoad event, which EventRegistration.OnModulePreload points at this script"),
    };

    /// <param name="title">First line of the snapshot, naming which assemblies were scanned.</param>
    /// <param name="registry">A registry loaded with the assemblies to record.</param>
    /// <param name="include">
    /// When given, only handlers whose method passes are listed, and scripts left with no handler are omitted.
    /// </param>
    public static string Render(string title, ScriptRegistry registry, Func<MethodInfo, bool> include = null)
    {
        var text = new StringBuilder();
        text.Append("# ").Append(title).Append('\n');
        text.Append("# One line per handler, in the order the registry runs them: kind, then Namespace.Type.Method.\n");
        text.Append("# Kinds: action = void handler, run in this order; conditional = bool handler, every one runs and the results are ANDed.\n");
        text.Append("# The order is whatever reflection returns for the compiled assembly (see ScriptHandlerOrderTests for what drives it).\n");
        text.Append("# Regenerate: SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~ScriptHandlerOrderTests\"\n");
        text.Append('\n');

        text.Append("== BOOT BUCKETS (in the order the server reaches them) ==\n");
        foreach (var (script, description) in BootBuckets)
            AppendScript(text, registry, script, description, include);

        var bootScripts = new HashSet<string>(BootBuckets.Select(b => b.Script), StringComparer.Ordinal);
        var otherScripts = registry.GetActionScriptNames()
            .Concat(registry.GetConditionalScriptNames())
            .Where(name => !bootScripts.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        text.Append('\n');
        text.Append("== OTHER SCRIPTS (alphabetical; the registry never reads its script names in order) ==\n");
        foreach (var script in otherScripts)
            AppendScript(text, registry, script, null, include);

        return text.ToString();
    }

    /// <summary>
    /// The handlers the registry holds for a script as <c>"action ..."</c> lines followed by <c>"conditional ..."</c>
    /// lines, each in the order the server runs them. The text after the kind is <c>Namespace.Type.Method</c>.
    /// </summary>
    public static IReadOnlyList<string> HandlerLines(ScriptRegistry registry, string script, Func<MethodInfo, bool> include = null)
    {
        var lines = new List<string>();
        foreach (var (action, _) in registry.GetActionScripts(script))
        {
            if (include == null || include(action.Method))
                lines.Add("action      " + FullName(action.Method));
        }

        foreach (var (action, _) in registry.GetConditionalScripts(script))
        {
            if (include == null || include(action.Method))
                lines.Add("conditional " + FullName(action.Method));
        }

        return lines;
    }

    /// <summary>Every script that has at least one handler of either kind.</summary>
    public static IReadOnlyList<string> AllScriptNames(ScriptRegistry registry)
    {
        return registry.GetActionScriptNames()
            .Concat(registry.GetConditionalScriptNames())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string FullName(MethodInfo method)
    {
        return method.DeclaringType!.FullName + "." + method.Name;
    }

    private static void AppendScript(
        StringBuilder text,
        ScriptRegistry registry,
        string script,
        string description,
        Func<MethodInfo, bool> include)
    {
        var lines = HandlerLines(registry, script, include);
        if (lines.Count == 0 && include != null)
            return;

        text.Append('\n');
        text.Append(script);
        if (description != null)
            text.Append("  # ").Append(description);
        text.Append('\n');

        if (lines.Count == 0)
            text.Append("  (no handlers)\n");
        foreach (var line in lines)
            text.Append("  ").Append(line).Append('\n');
    }
}
