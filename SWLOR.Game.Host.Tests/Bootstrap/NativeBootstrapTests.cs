using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Host.Bootstrap;

namespace SWLOR.Game.Host.Tests.Bootstrap;

/// <summary>The host's NWNX entry is what swlor.env names for <c>SWLOR_DOTNET_ENTRY=host</c>.</summary>
[TestFixture]
public class NativeBootstrapTests
{
    [Test]
    public void NWNX_calls_a_public_static_parameterless_Initialize_on_the_named_type()
    {
        typeof(NativeBootstrap).FullName.Should().Be("SWLOR.Game.Host.Bootstrap.NativeBootstrap");
        typeof(NativeBootstrap).Assembly.GetName().Name.Should().Be("SWLOR.Game.Host");

        var initialize = typeof(NativeBootstrap).GetMethod("Initialize", Type.EmptyTypes);

        initialize.Should().NotBeNull();
        initialize!.IsPublic.Should().BeTrue();
        initialize.IsStatic.Should().BeTrue();
        initialize.ReturnType.Should().Be(typeof(void), "NWNX binds it as System.Action");
    }

    [Test]
    public void The_default_game_path_is_the_game_folder_the_deploy_tools_stage()
    {
        HostSettings.DefaultGameAssemblyRelativePath.Should().Be("game/SWLOR.Game.Server.dll");
    }
}
