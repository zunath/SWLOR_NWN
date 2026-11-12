using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Pins the reload decision of every boot-only handler (<see cref="BootPhaseAttribute.AdoptedBy"/>,
/// <see cref="BootPhaseAttribute.CarriedBy"/>, <see cref="BootPhaseAttribute.NothingToAdopt"/>) and the state the adoption steps
/// rebuild (<see cref="ReloadStateAttribute"/>), so a new boot handler cannot be added without a reload decision and a change to
/// one shows in review. The rule is in <c>Readmes/MaintainabilityRefactorHotReload.md</c> sections 2 and 3.
/// </summary>
[TestFixture]
public class ReloadAdoptionTests
{
    private const string Snapshot = "SWLOR.Game.Server.Tests/Core/Snapshots/ReloadAdoption.txt";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~ReloadAdoptionTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)";

    private static readonly string[] BootScripts = ReloadBootstrap.BootScripts.Append(ScriptName.OnSWLORApplicationShutdown).ToArray();

    private static ScriptRegistry LoadRegistry()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { ScriptHandlerTestSupport.GameAssembly }, ScriptHandlerTestSupport.Owners);
        return registry;
    }

    private static IEnumerable<(string Script, MethodInfo Method, BootPhaseAttribute Phase)> Handlers(ScriptRegistry registry)
    {
        return BootScripts
            .SelectMany(script => registry.GetActionScripts(script).Select(handler => (script, handler.Action.Method)))
            .Select(entry => (entry.script, entry.Method, entry.Method.GetCustomAttribute<BootPhaseAttribute>(false)))
            .Where(entry => entry.Item3 != null);
    }

    [Test]
    public void Every_boot_only_handler_has_exactly_one_reload_decision()
    {
        var problems = Handlers(LoadRegistry())
            .Where(handler => handler.Phase.Reload == BootReload.BootOnly)
            .Where(handler => string.IsNullOrWhiteSpace(handler.Phase.AdoptedBy) == string.IsNullOrWhiteSpace(handler.Phase.NothingToAdopt))
            .Select(handler => $"{handler.Script}: {ScriptHandlerManifest.HandlerName(handler.Method)}")
            .ToArray();

        problems.Should().BeEmpty(
            "a boot-only handler names the adoption step a reload runs instead of it (AdoptedBy) or says why a reload needs nothing " +
            "(NothingToAdopt), never both; read MaintainabilityRefactorHotReload.md section 3, then regenerate: " + RegenerateCommand);
    }

    [Test]
    public void Only_a_boot_only_handler_with_an_adoption_step_has_reload_decisions_or_a_carry_step()
    {
        var problems = Handlers(LoadRegistry())
            .Where(handler => handler.Phase.Reload != BootReload.BootOnly
                ? handler.Phase.AdoptedBy != null || handler.Phase.CarriedBy != null || handler.Phase.NothingToAdopt != null
                : handler.Phase.CarriedBy != null && handler.Phase.AdoptedBy == null)
            .Select(handler => ScriptHandlerManifest.HandlerName(handler.Method))
            .ToArray();

        problems.Should().BeEmpty("a rerun, native hook or retire handler has no adoption step, and a carry only feeds an adoption step");
    }

    [Test]
    public void Adopted_state_belongs_to_a_class_that_has_an_adoption_step_or_a_rerun_handler()
    {
        var registry = LoadRegistry();
        var owners = Handlers(registry)
            .Where(handler => handler.Phase.Reload == BootReload.Rerun || handler.Phase.AdoptedBy != null)
            .Select(handler => handler.Method.DeclaringType)
            .ToHashSet();
        // The adoption steps that delegate keep their state in the class they delegate to.
        owners.Add(typeof(SWLOR.Game.Server.Domain.World.PropertyLoaderService));

        var strays = AdoptedFields()
            .Select(field => field.DeclaringType)
            .Distinct()
            .Where(type => !owners.Contains(type))
            .Select(type => type.FullName)
            .ToArray();

        strays.Should().BeEmpty("adopted state is rebuilt by an adoption step; a field nothing rebuilds is runtime state");
    }

    [Test]
    public void Reload_decisions_match_the_snapshot()
    {
        var text = new StringBuilder();
        text.Append("# SWLOR reload decisions: what a hot reload runs in place of each boot-only handler, and the state it adopts.\n");
        text.Append("# Boot-only handlers in manifest order: adopt <step> [carry <step>] = the reload bootstrap runs the adoption step after\n");
        text.Append("# the rerun handlers (and the old build ran the carry step while it quiesced); nothing = why a reload needs nothing.\n");
        text.Append("# Adopted state: the fields marked [ReloadState(ReloadStateKind.Adopted)], compared with the old build's at quiescence.\n");
        text.Append("# The rule is in SWLOR.Game.Server/Readmes/MaintainabilityRefactorHotReload.md sections 2 and 3.\n");
        text.Append("# Regenerate (build first): " + RegenerateCommand + "\n");

        text.Append("\n== BOOT-ONLY HANDLERS ==\n");
        foreach (var (script, method, phase) in Handlers(LoadRegistry()).Where(handler => handler.Phase.Reload == BootReload.BootOnly))
        {
            text.Append(script).Append(' ').Append(ScriptHandlerManifest.HandlerName(method)).Append('\n');
            if (phase.AdoptedBy != null)
            {
                text.Append("  adopt ").Append(phase.AdoptedBy);
                if (phase.CarriedBy != null)
                    text.Append(" carry ").Append(phase.CarriedBy);
                text.Append('\n');
            }
            else
            {
                text.Append("  nothing: ").Append(phase.NothingToAdopt).Append('\n');
            }
        }

        text.Append("\n== ADOPTED STATE ==\n");
        foreach (var field in AdoptedFields().OrderBy(field => field.DeclaringType.FullName, StringComparer.Ordinal).ThenBy(field => field.Name, StringComparer.Ordinal))
            text.Append(field.DeclaringType.FullName).Append('.').Append(field.Name).Append('\n');

        SnapshotComparison.AssertMatches(Snapshot, text.ToString(), RegenerateCommand);
    }

    private static IEnumerable<FieldInfo> AdoptedFields()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        return ScriptHandlerTestSupport.GameAssembly.GetTypes()
            .SelectMany(type => type.GetFields(flags))
            .Where(field => ReloadDigestHasher.StateOf(field) == ReloadStateKind.Adopted);
    }
}
