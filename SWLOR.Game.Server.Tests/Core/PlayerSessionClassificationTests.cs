using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Records what a hot reload does with every handler of <c>mod_enter</c> and <c>mod_exit</c> for the players who stay online
/// (<see cref="PlayerSessionAttribute"/>, <see cref="PlayerReload"/>), and fails when a handler of either script has no decision, when a
/// decision is made on the wrong script, or when a step does not bind. The snapshot is the reviewed list; the rule is in
/// <c>Readmes/MaintainabilityRefactorHotReload.md</c> (P4-15).
/// </summary>
[TestFixture]
public class PlayerSessionClassificationTests
{
    private const string Snapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/PlayerSessionClassification.txt";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~PlayerSessionClassificationTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)" +
        "   (build SWLOR.Game.Server.Tests first so the generator sees your handler)";

    private static ScriptRegistry LoadRegistry()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { ScriptHandlerTestSupport.GameAssembly }, ScriptHandlerTestSupport.Owners);
        return registry;
    }

    private static IEnumerable<(string Script, Action Handler, PlayerSessionAttribute Session)> Handlers(ScriptRegistry registry)
    {
        foreach (var script in PlayerReloadPlan.Scripts)
        {
            foreach (var (handler, _) in registry.GetActionScripts(script))
                yield return (script, handler, handler.Method.GetCustomAttribute<PlayerSessionAttribute>(false));
        }
    }

    [Test]
    public void EveryLoginAndLogoutHandlerHasAPlayerSession()
    {
        var registry = LoadRegistry();
        registry.GetConditionalScripts(ScriptName.OnModuleEnter).Should().BeEmpty();
        registry.GetConditionalScripts(ScriptName.OnModuleExit).Should().BeEmpty();

        var unclassified = Handlers(registry)
            .Where(entry => entry.Session == null)
            .Select(entry => $"{entry.Script}: {ScriptHandlerManifest.HandlerName(entry.Handler.Method)}")
            .ToArray();

        unclassified.Should().BeEmpty(
            "every mod_enter and mod_exit handler needs [PlayerSession(PlayerReload.<class>, \"<reason>\")] so a hot reload knows what to do " +
            "for the players who stay online; read P4-15 in SWLOR.Game.Server/Readmes/MaintainabilityRefactorHotReload.md, then add it and " +
            "regenerate the snapshot: " + RegenerateCommand);
    }

    [Test]
    public void EachClassBelongsToItsScriptAndGivesAReason()
    {
        var problems = new List<string>();
        foreach (var (script, handler, session) in Handlers(LoadRegistry()).Where(entry => entry.Session != null))
        {
            var name = ScriptHandlerManifest.HandlerName(handler.Method);
            var allowed = script == ScriptName.OnModuleEnter
                ? new[] { PlayerReload.Restore, PlayerReload.LoginOnly }
                : new[] { PlayerReload.Quiesce, PlayerReload.LogoutOnly };
            if (!allowed.Contains(session.Reload))
                problems.Add($"{script}: {name} is {session.Reload}, which is not a class of {script}");
            if (string.IsNullOrWhiteSpace(session.Reason))
                problems.Add($"{script}: {name} gives no reason");

            var runs = session.Reload is PlayerReload.Restore or PlayerReload.Quiesce;
            if (!runs && !string.IsNullOrEmpty(session.Step))
                problems.Add($"{script}: {name} is {session.Reload} but names step {session.Step}, which never runs");
            if (runs)
            {
                try
                {
                    PlayerReloadPlan.BindStep(handler, session.Step);
                }
                catch (InvalidOperationException ex)
                {
                    problems.Add(ex.Message);
                }
            }
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void TheQuiesceAndRestorePlansRunTheStepsInManifestOrder()
    {
        var registry = LoadRegistry();
        var quiesce = PlayerReloadPlan.QuiescePlan(registry, () => Array.Empty<uint>());
        var restore = PlayerReloadPlan.RestorePlan(registry, () => Array.Empty<uint>());

        quiesce.Should().OnlyContain(step => step.Kind == ReloadStepKind.Quiesce && step.Script == ScriptName.OnModuleExit);
        restore.Should().OnlyContain(step => step.Kind == ReloadStepKind.Restore && step.Script == ScriptName.OnModuleEnter);
        quiesce.Select(step => step.Handler).Should().Equal(Handlers(registry)
            .Where(entry => entry.Script == ScriptName.OnModuleExit && entry.Session?.Reload == PlayerReload.Quiesce)
            .Select(entry => ScriptHandlerManifest.HandlerName(entry.Handler.Method)));
        restore.Select(step => step.Handler).Should().Equal(Handlers(registry)
            .Where(entry => entry.Script == ScriptName.OnModuleEnter && entry.Session?.Reload == PlayerReload.Restore)
            .Select(entry => ScriptHandlerManifest.HandlerName(entry.Handler.Method)));
        restore.First().Name.Should().NotBe(restore.First().Handler, "a step runs the handler's per-player method, not the handler");
    }

    [Test]
    public void PlayerSessionClassesMatchTheSnapshot()
    {
        var text = new StringBuilder();
        text.Append("# SWLOR login and logout handler classification for hot reload (P4-15): SWLOR.Game.Server only\n");
        text.Append("# One block per handler of mod_enter and mod_exit, in the order the registry runs them: class, Namespace.Type.Method, the step a\n");
        text.Append("# reload runs for each online player (Restore at activation, Quiesce at quiescence), then the reason.\n");
        text.Append("# Classes (PlayerReload): Restore and LoginOnly for mod_enter, Quiesce and LogoutOnly for mod_exit. UNCLASSIFIED fails\n");
        text.Append("# EveryLoginAndLogoutHandlerHasAPlayerSession. The rule is in SWLOR.Game.Server/Readmes/MaintainabilityRefactorHotReload.md (P4-15).\n");
        text.Append("# Regenerate (build first so the handlers are current): " + RegenerateCommand + "\n");

        var totals = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in Handlers(LoadRegistry()).GroupBy(entry => entry.Script))
        {
            var handlers = group.ToArray();
            text.Append('\n').Append(group.Key).Append(" (").Append(handlers.Length).Append(")\n");
            foreach (var (_, handler, session) in handlers)
            {
                var kind = session?.Reload.ToString() ?? "UNCLASSIFIED";
                totals[kind] = totals.GetValueOrDefault(kind) + 1;
                text.Append("  ").Append(kind.PadRight(11)).Append(' ').Append(ScriptHandlerManifest.HandlerName(handler.Method));
                if (!string.IsNullOrEmpty(session?.Step))
                    text.Append(" -> ").Append(session.Step);
                text.Append('\n');
                if (session != null)
                    text.Append("              ").Append(session.Reason).Append('\n');
            }
        }

        text.Append("\n== TOTALS ==\n");
        foreach (var (kind, count) in totals)
            text.Append(kind.PadRight(11)).Append(' ').Append(count).Append('\n');

        SnapshotComparison.AssertMatches(Snapshot, text.ToString(), RegenerateCommand);
    }
}
