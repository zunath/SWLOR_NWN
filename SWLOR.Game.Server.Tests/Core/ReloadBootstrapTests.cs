using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The reload bootstrap (<see cref="ReloadBootstrap"/>): a build that activates after the module has loaded reruns the rerun
/// handlers of the boot scripts in manifest order, then runs the adoption steps in the order of the boot-only handlers they stand
/// in for. Boot-only, native hook, retire and unclassified handlers never run. The carry plan holds the carry steps.
/// </summary>
[TestFixture]
public class ReloadBootstrapTests
{
    private static readonly List<string> Calls = new();

    // Declared in another order than the manifest's, so the plan can only follow the manifest.
    private static class Fixture
    {
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadRerun() => Calls.Add("load rerun");

        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(AdoptLoad))]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadBootOnly() => Calls.Add("load boot only");

        public static void AdoptLoad() => Calls.Add("adopt load");

        [BootPhase(BootReload.NativeHook)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadHook() => Calls.Add("hook");

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Unclassified() => Calls.Add("unclassified");

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void AfterRerun() => Calls.Add("cache after rerun");

        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(AdoptCache), CarriedBy = nameof(CarryCache))]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheBootOnly() => Calls.Add("cache boot only");

        public static void AdoptCache() => Calls.Add("adopt cache");

        public static void CarryCache() => Calls.Add("carry cache");

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheRerun() => Calls.Add("cache before rerun");

        [BootPhase(BootReload.BootOnly, NothingToAdopt = "Stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void PreloadBootOnly() => Calls.Add("preload boot only");

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void PreloadRerun() => Calls.Add("preload rerun");

        [BootPhase(BootReload.Retire)]
        [NWNEventHandler(ScriptName.OnSWLORApplicationShutdown)]
        public static void Shutdown() => Calls.Add("shutdown");
    }

    private sealed class Owner
    {
        public string Name { get; init; }

        [BootPhase(BootReload.BootOnly, AdoptedBy = nameof(Adopt), CarriedBy = nameof(Carry))]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void Boot() => Calls.Add($"{Name} boot");

        public void Adopt() => Calls.Add($"{Name} adopt");

        public void Carry() => Calls.Add($"{Name} carry");
    }

    private static class RerunOwner
    {
        [BootPhase(BootReload.Rerun, AdoptedBy = nameof(Adopt), CarriedBy = nameof(Carry))]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load() => Calls.Add("rerun load");

        public static void Adopt() => Calls.Add("rerun adopt");

        public static void Carry() => Calls.Add("rerun carry");
    }

    [Test]
    public void A_rerun_handler_with_an_adoption_step_reruns_and_then_adopts_and_its_carry_is_a_carry_step()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlers(typeof(RerunOwner).GetMethods(), ScriptHandlerManifest.Empty);

        ReloadBootstrap.Run(ReloadBootstrap.Plan(registry), action => action());
        ReloadBootstrap.Run(ReloadBootstrap.CarryPlan(registry), action => action());

        Calls.Should().Equal("rerun load", "rerun adopt", "rerun carry");
    }

    private sealed class Misnamed
    {
        [BootPhase(BootReload.BootOnly, AdoptedBy = "NoSuchMethod")]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Boot() { }
    }

    private static string Name(string method) => typeof(Fixture).FullName + "." + method;

    private static readonly string Manifest = string.Join("\n",
        "mod_preload",
        "  action      " + Name(nameof(Fixture.PreloadBootOnly)),
        "  action      " + Name(nameof(Fixture.PreloadRerun)),
        "mod_cache_bef",
        "  action      " + Name(nameof(Fixture.CacheBootOnly)),
        "  action      " + Name(nameof(Fixture.CacheRerun)),
        "mod_cache_aft",
        "  action      " + Name(nameof(Fixture.AfterRerun)),
        "mod_load",
        "  action      " + Name(nameof(Fixture.LoadHook)),
        "  action      " + Name(nameof(Fixture.LoadBootOnly)),
        "  action      " + typeof(Owner).FullName + "." + nameof(Owner.Boot),
        "  action      " + Name(nameof(Fixture.LoadRerun)),
        "app_shutdown",
        "  action      " + Name(nameof(Fixture.Shutdown)));

    private static ScriptRegistry Registry(params Type[] types)
    {
        var registry = new ScriptRegistry();
        var owner = new Owner { Name = "owner" };
        registry.LoadHandlers(types.SelectMany(type => type.GetMethods()), ScriptHandlerManifest.Parse(Manifest, "test"),
            type => type == typeof(Owner) ? owner : null);
        return registry;
    }

    [SetUp]
    public void ClearCalls()
    {
        Calls.Clear();
    }

    [Test]
    public void The_plan_reruns_the_rerun_handlers_in_manifest_order_then_adopts_in_the_order_of_the_boot_only_handlers()
    {
        var plan = ReloadBootstrap.Plan(Registry(typeof(Fixture), typeof(Owner)));

        plan.Select(step => (step.Kind, step.Script, step.Name)).Should().Equal(
            (ReloadStepKind.Rerun, ScriptName.OnModulePreload, Name(nameof(Fixture.PreloadRerun))),
            (ReloadStepKind.Rerun, ScriptName.OnModuleCacheBefore, Name(nameof(Fixture.CacheRerun))),
            (ReloadStepKind.Rerun, ScriptName.OnModuleCacheAfter, Name(nameof(Fixture.AfterRerun))),
            (ReloadStepKind.Rerun, ScriptName.OnModuleLoad, Name(nameof(Fixture.LoadRerun))),
            (ReloadStepKind.Adopt, ScriptName.OnModuleCacheBefore, Name(nameof(Fixture.AdoptCache))),
            (ReloadStepKind.Adopt, ScriptName.OnModuleLoad, Name(nameof(Fixture.AdoptLoad))),
            (ReloadStepKind.Adopt, ScriptName.OnModuleLoad, typeof(Owner).FullName + "." + nameof(Owner.Adopt)));
        plan.Where(step => step.Kind == ReloadStepKind.Adopt).Select(step => step.Handler).Should().Equal(
            Name(nameof(Fixture.CacheBootOnly)), Name(nameof(Fixture.LoadBootOnly)), typeof(Owner).FullName + "." + nameof(Owner.Boot));
    }

    [Test]
    public void Running_the_plan_never_runs_a_boot_only_native_hook_retire_or_unclassified_handler()
    {
        var results = ReloadBootstrap.Run(ReloadBootstrap.Plan(Registry(typeof(Fixture), typeof(Owner))), action => action());

        Calls.Should().Equal("preload rerun", "cache before rerun", "cache after rerun", "load rerun",
            "adopt cache", "adopt load", "owner adopt");
        results.Should().HaveCount(7).And.OnlyContain(result => result.Error == null && result.Milliseconds >= 0);
    }

    [Test]
    public void The_carry_plan_holds_only_the_carry_steps_bound_to_the_handlers_owner()
    {
        var plan = ReloadBootstrap.CarryPlan(Registry(typeof(Fixture), typeof(Owner)));

        plan.Select(step => step.Kind).Should().OnlyContain(kind => kind == ReloadStepKind.Carry);
        ReloadBootstrap.Run(plan, action => action());
        Calls.Should().Equal("carry cache", "owner carry");
    }

    [Test]
    public void Each_step_runs_through_the_context_and_one_that_throws_is_logged_and_the_next_runs()
    {
        var plan = ReloadBootstrap.Plan(Registry(typeof(Fixture), typeof(Owner)));
        var contexts = 0;

        var results = ReloadBootstrap.Run(plan, action =>
        {
            contexts++;
            if (contexts == 2)
                throw new InvalidOperationException("step two");
            action();
        });

        contexts.Should().Be(plan.Count);
        results[1].Error.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("step two");
        Calls.Should().HaveCount(plan.Count - 1);
        ReloadBootstrap.Describe(results).Should().Contain("reran=4 adopted=3 carried=0 failed=1")
            .And.Contain("failedSteps=" + Name(nameof(Fixture.CacheRerun)));
    }

    [Test]
    public void A_boot_only_handler_that_names_a_missing_adoption_method_stops_the_plan()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlers(typeof(Misnamed).GetMethods(), ScriptHandlerManifest.Empty);

        var plan = () => ReloadBootstrap.Plan(registry);

        plan.Should().Throw<InvalidOperationException>().WithMessage("*NoSuchMethod*");
    }

    [Test]
    public void The_servers_plan_binds_every_adoption_and_carry_step_and_reruns_every_rerun_handler()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { ScriptHandlerTestSupport.GameAssembly }, ScriptHandlerTestSupport.Owners);

        var plan = ReloadBootstrap.Plan(registry);
        var carry = ReloadBootstrap.CarryPlan(registry);

        var reruns = ReloadBootstrap.BootScripts
            .SelectMany(script => registry.GetActionScripts(script))
            .Count(handler => handler.Action.Method.GetCustomAttributes(typeof(BootPhaseAttribute), false)
                .Cast<BootPhaseAttribute>().Any(phase => phase.Reload == BootReload.Rerun));
        plan.Count(step => step.Kind == ReloadStepKind.Rerun).Should().Be(reruns);
        plan.Where(step => step.Kind == ReloadStepKind.Adopt).Select(step => step.Name).Should().Equal(
            "SWLOR.Game.Server.Domain.Foundation.DBService.CheckIndexes",
            "SWLOR.Game.Server.Domain.Economy.GuildService.AdoptGuildTasks",
            "SWLOR.Game.Server.Domain.Creatures.SpawnService.AdoptSpawns",
            "SWLOR.Game.Server.Domain.Foundation.MigrationService.CheckCharacterVersion",
            "SWLOR.Game.Server.Domain.Abilities.MimicryService.AdoptTechniqueDescriptions",
            "SWLOR.Game.Server.Domain.World.PropertyService.AdoptProperties",
            "SWLOR.Game.Server.Domain.World.ShuttleService.AdoptFlights",
            "SWLOR.Game.Server.Domain.World.WeatherService.AdoptWeather",
            "SWLOR.Game.Server.Feature.StoreManagement.AdoptStores");
        carry.Select(step => step.Name).Should().Equal(
            "SWLOR.Game.Server.Domain.Economy.GuildService.CarryGuildTasks",
            "SWLOR.Game.Server.Domain.Creatures.SpawnService.CarrySpawns",
            "SWLOR.Game.Server.Domain.Abilities.MimicryService.CarryTechniqueDescriptions",
            "SWLOR.Game.Server.Domain.World.PropertyService.CarryProperties",
            "SWLOR.Game.Server.Domain.World.WeatherService.CarryWeather");
        plan.Where(step => !step.Method.IsStatic).Should().OnlyContain(step => step.Run.Target != null,
            "an instance step runs on the container's singleton, as its handler does");
        plan.Select(step => step.Name).Should().NotContain("SWLOR.Game.Server.Domain.Crafting.CraftService.CacheData",
            "the crafting cache fills on the event the rerun skill cache signals, through the activation dispatch, not as a step");
        ReloadBootstrap.NestedScripts.SelectMany(script => registry.GetActionScripts(script))
            .Select(handler => ScriptHandlerManifest.HandlerName(handler.Action.Method))
            .Should().Contain("SWLOR.Game.Server.Domain.Crafting.CraftService.CacheData");
    }

    [Test]
    public void Gui_ids_are_reserved_in_boot_order_by_the_rerun_handlers_alone()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { ScriptHandlerTestSupport.GameAssembly }, ScriptHandlerTestSupport.Owners);
        var bootOrder = ReloadBootstrap.BootScripts
            .SelectMany(script => registry.GetActionScripts(script))
            .Select(handler => ScriptHandlerManifest.HandlerName(handler.Action.Method))
            .Where(name => name.EndsWith(".ReserveGuiIds", StringComparison.Ordinal))
            .ToArray();

        var reloadOrder = ReloadBootstrap.Plan(registry)
            .Select(step => step.Name)
            .Where(name => name.EndsWith(".ReserveGuiIds", StringComparison.Ordinal))
            .ToArray();

        bootOrder.Should().NotBeEmpty();
        reloadOrder.Should().Equal(bootOrder,
            "GuiService hands out ids from a running counter, so a reload reserves the same ids only in the boot's order");
    }
}
