using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Host.Bootstrap;
using SWLOR.Game.Host.Services;

namespace SWLOR.Game.Host.Tests.Services;

/// <summary>What every generation reads about the process: the environment, the reload switch and the module load.</summary>
[TestFixture]
public class HostLifecycleTests
{
    private static HostSettings Settings(params (string Name, string? Value)[] variables)
    {
        var map = variables.ToDictionary(variable => variable.Name, variable => variable.Value);
        return HostSettings.Read(name => map.GetValueOrDefault(name), Path.Combine(Path.GetTempPath(), "home", "dotnet"));
    }

    [Test]
    public void The_environment_and_the_reload_switch_come_from_the_settings()
    {
        var on = new HostLifecycle(Settings((HostSettings.EnvironmentVariable, "test"), (HostSettings.HotReloadVariable, "true")));
        var refused = new HostLifecycle(Settings((HostSettings.EnvironmentVariable, "production"), (HostSettings.HotReloadVariable, "true")));
        var off = new HostLifecycle(Settings((HostSettings.EnvironmentVariable, "dev")));

        (on.Environment, on.ReloadEnabled).Should().Be(("Test", true));
        (refused.Environment, refused.ReloadEnabled).Should().Be(("Production", false));
        (off.Environment, off.ReloadEnabled).Should().Be(("Development", false));
    }

    [Test]
    public void The_module_is_loaded_once_the_host_marks_it_and_only_the_first_mark_counts()
    {
        var lifecycle = new HostLifecycle(Settings());

        lifecycle.ModuleLoaded.Should().BeFalse("generation 1 activates before the engine loads the module");
        lifecycle.MarkModuleLoaded().Should().BeTrue();
        lifecycle.ModuleLoaded.Should().BeTrue();
        lifecycle.MarkModuleLoaded().Should().BeFalse();
        lifecycle.ModuleLoaded.Should().BeTrue();
    }

    [Test]
    public void The_lifecycle_needs_the_settings()
    {
        var create = () => new HostLifecycle(null!);

        create.Should().Throw<ArgumentNullException>();
    }
}
