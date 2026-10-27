using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Proves where the boot builds the service container: after the internal systems are up and before the step that
/// registers the script handlers. A handler (<c>mod_preload</c> ones included) can only run once it is registered, so
/// the container exists before any of them runs. The tests drive the real step list, <see cref="ServerBootstrapper.Steps"/>,
/// which is the list <see cref="ServerBootstrapper.Bootstrap"/> runs.
/// </summary>
[TestFixture]
public class ServerBootstrapperOrderTests
{
    private static readonly string[] StepOrder =
    {
        "InitializeNWNCore",
        "RegisterNativeHandlers",
        "InitializeSWLORSystems",
        "BuildServiceContainer",
        "RegisterEventHandlers",
        "LoadEngineTestAssembly",
        "LoadScripts"
    };

    [Test]
    public void The_boot_runs_its_steps_in_this_order()
    {
        var bootstrapper = new ServerBootstrapper(new ContainerHost());

        bootstrapper.Steps.Select(step => step.Name).Should().Equal(StepOrder,
            "the first four steps are the order the boot always had; the container is built right after them, before the handlers load");
    }

    [Test]
    public void The_container_is_built_before_the_script_handlers_are_registered()
    {
        var container = new ContainerHost();
        var bootstrapper = new ServerBootstrapper(container);
        var ran = new List<string>();
        var builtWhenHandlersLoaded = (bool?)null;
        var builtBeforeContainerStep = false;

        // Every step except the real BuildServiceContainer is replaced by a recorder: the others need the game engine.
        var steps = bootstrapper.Steps.Select(step => step.Name switch
        {
            "BuildServiceContainer" => new BootstrapStep(step.Name, () =>
            {
                builtBeforeContainerStep = container.IsBuilt;
                step.Run();
                ran.Add(step.Name);
            }),
            "LoadScripts" => new BootstrapStep(step.Name, () =>
            {
                builtWhenHandlersLoaded = container.IsBuilt;
                ran.Add(step.Name);
            }),
            _ => new BootstrapStep(step.Name, () => ran.Add(step.Name))
        }).ToList();

        ServerBootstrapper.Run(steps);

        ran.Should().Equal(StepOrder);
        builtBeforeContainerStep.Should().BeFalse();
        builtWhenHandlersLoaded.Should().BeTrue("handlers exist only after LoadScripts, and every handler may need a service from the container");
        container.IsBuilt.Should().BeTrue();
    }

    [Test]
    public void The_container_build_step_builds_the_servers_registrations()
    {
        var container = new ContainerHost();
        var bootstrapper = new ServerBootstrapper(container);

        bootstrapper.Steps.Single(step => step.Name == "BuildServiceContainer").Run();

        container.IsBuilt.Should().BeTrue();
    }

    [Test]
    public void Registering_the_script_handlers_without_a_container_is_refused_before_anything_else_happens()
    {
        var bootstrapper = new ServerBootstrapper(new ContainerHost());
        var loadScripts = bootstrapper.Steps.Single(step => step.Name == "LoadScripts");

        var load = () => loadScripts.Run();

        load.Should().Throw<ContainerNotBuiltException>().WithMessage("*before the script handlers are registered*");
    }

    [Test]
    public void No_script_handler_exists_until_the_handlers_are_loaded()
    {
        var registry = new ScriptRegistry(() => ScriptHandlerManifest.Empty);

        registry.HasScript(ScriptName.OnModulePreload).Should().BeFalse();
        registry.GetActionScripts(ScriptName.OnModulePreload).Should().BeEmpty();
    }

    [Test]
    public void A_failing_step_stops_the_boot_and_the_exception_reaches_the_caller()
    {
        var ran = new List<string>();
        var steps = new[]
        {
            new BootstrapStep("first", () => ran.Add("first")),
            new BootstrapStep("second", () => throw new InvalidOperationException("second failed")),
            new BootstrapStep("third", () => ran.Add("third"))
        };

        var run = () => ServerBootstrapper.Run(steps);

        run.Should().Throw<InvalidOperationException>().WithMessage("second failed");
        ran.Should().Equal("first");
    }

    [Test]
    public void A_container_that_fails_to_build_stops_the_boot_before_the_handlers_load()
    {
        var container = new ContainerHost();
        var bootstrapper = new ServerBootstrapper(container);
        var ran = new List<string>();
        var steps = bootstrapper.Steps.Select(step => step.Name switch
        {
            "BuildServiceContainer" => new BootstrapStep(step.Name, () => container.Build(_ => throw new InvalidOperationException("registration failed"))),
            _ => new BootstrapStep(step.Name, () => ran.Add(step.Name))
        }).ToList();

        var run = () => ServerBootstrapper.Run(steps);

        run.Should().Throw<InvalidOperationException>().WithMessage("registration failed");
        ran.Should().NotContain("LoadScripts");
        ran.Should().NotContain("RegisterEventHandlers");
    }

    [Test]
    public void Bootstrap_rejects_a_missing_container_and_steps_reject_missing_parts()
    {
        var withoutContainer = () => new ServerBootstrapper(null);
        var withoutName = () => new BootstrapStep(null, () => { });
        var withoutWork = () => new BootstrapStep("x", null);

        withoutContainer.Should().Throw<ArgumentNullException>();
        withoutName.Should().Throw<ArgumentNullException>();
        withoutWork.Should().Throw<ArgumentNullException>();
    }
}
