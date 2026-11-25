using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using static SWLOR.Game.Server.Tests.Core.ScriptHandlerTestSupport;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The handlers the branch split. A split is a line of <c>tools/Refactor/HandlerRenames.txt</c> with several branch names
/// (master's one handler, then its parts in run order); the parts must stand together in the manifest where master's
/// handler stood, so the boot runs them back to back in the old handler's place, and the parity run maps master's one
/// handler onto them. There are two kinds: the boot handler splits, each into a part a hot reload reruns and a boot-only
/// part, and the service splits, where a service split moves part of a handler into the new service and every part keeps
/// one BootReload class, so a hot reload treats the handler as before.
/// </summary>
[TestFixture]
public class SplitBootHandlerTests
{
    private static readonly string[] SplitMasterHandlers =
    {
        "SWLOR.Game.Server.Service.DB.Load",
        "SWLOR.Game.Server.Feature.EventRegistration.OnModulePreload",
        "SWLOR.Game.Server.Service.Spawn.CacheData",
        "SWLOR.Game.Server.Service.Shuttle.LoadShuttleFlights",
        "SWLOR.Game.Server.Service.Space.LoadLandingPoints",
        "SWLOR.Game.Server.Feature.ServerTasks.ProcessBootUp",
        "SWLOR.Game.Server.Feature.StoreManagement.ProcessStores",
    };

    // Service splits. Add each later service split here.
    private static readonly string[] ServiceSplitMasterHandlers =
    {
        // Stat split part 4: the trait stats' cache handler splits out of Mimicry.CacheData.
        "SWLOR.Game.Server.Service.Mimicry.CacheData",
        // Stat split part 2: the stat effect listener's registration splits out of Stat.CacheData.
        "SWLOR.Game.Server.Service.Stat.CacheData",
        // Status effect split: the melee, impact and capstone listeners register out of StatusEffect.CacheData.
        "SWLOR.Game.Server.Service.StatusEffect.CacheData",
        // Creatures split: the stealth aggro listener's registration splits out of AI.CacheAIData.
        "SWLOR.Game.Server.Service.AI.CacheAIData",
        // Combat services split: the recorder damage part's session listener registers out of Combat.LoadDamageTypes.
        "SWLOR.Game.Server.Service.Combat.LoadDamageTypes",
    };

    private static IReadOnlyList<string[]> Splits()
    {
        var path = Path.Combine(ClockSeamSources.FindRepositoryRoot(), "tools", "Refactor", "HandlerRenames.txt");
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(names => names.Length > 2)
            .ToList();
    }

    [Test]
    public void The_split_lines_are_the_mixed_boot_handlers_and_the_service_splits()
    {
        Splits().Select(names => names[0]).Should().BeEquivalentTo(SplitMasterHandlers.Concat(ServiceSplitMasterHandlers));
    }

    [Test]
    public void The_parts_of_a_split_handler_stand_together_in_one_script_of_the_manifest_in_their_run_order()
    {
        var manifest = ScriptHandlerManifest.Parse(ShippedManifestText(), "ScriptHandlerOrder.txt");

        foreach (var names in Splits())
        {
            var parts = names.Skip(1).ToArray();
            var scripts = manifest.ScriptNames
                .Where(script => manifest.GetHandlers(script, false).Contains(parts[0]) || manifest.GetHandlers(script, true).Contains(parts[0]))
                .ToList();
            scripts.Should().ContainSingle($"{names[0]} ran in one script");

            var handlers = manifest.GetHandlers(scripts[0], false).Contains(parts[0])
                ? manifest.GetHandlers(scripts[0], false)
                : manifest.GetHandlers(scripts[0], true);
            var first = handlers.ToList().IndexOf(parts[0]);
            handlers.Skip(first).Take(parts.Length).Should().Equal(parts, $"the parts of {names[0]} run back to back where it ran");
        }
    }

    [Test]
    public void Each_split_boot_handler_has_one_part_a_hot_reload_reruns_and_one_boot_only_part()
    {
        var handlers = MethodsOf(GameAssembly).Where(IsHandler)
            .GroupBy(ScriptHandlerManifest.HandlerName)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var names in Splits().Where(names => SplitMasterHandlers.Contains(names[0])))
        {
            var classes = names.Skip(1).Select(name => handlers[name].GetCustomAttribute<BootPhaseAttribute>()!.Reload).ToList();
            classes.Should().BeEquivalentTo(new[] { BootReload.Rerun, BootReload.BootOnly }, names[0]);
        }
    }

    [Test]
    public void The_parts_of_a_service_split_all_have_the_same_boot_reload_class()
    {
        var handlers = MethodsOf(GameAssembly).Where(IsHandler)
            .GroupBy(ScriptHandlerManifest.HandlerName)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var names in Splits().Where(names => ServiceSplitMasterHandlers.Contains(names[0])))
        {
            var classes = names.Skip(1).Select(name => handlers[name].GetCustomAttribute<BootPhaseAttribute>()!.Reload).Distinct().ToList();
            classes.Should().ContainSingle($"the parts of {names[0]} must not change how a hot reload treats it");
        }
    }
}
