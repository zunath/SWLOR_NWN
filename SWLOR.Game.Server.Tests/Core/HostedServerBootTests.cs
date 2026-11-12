using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>The real boot of the host entry: the server's own registrations and bootstrapper.</summary>
[TestFixture]
public class HostedServerBootTests
{
    [Test]
    public async Task Validation_builds_the_servers_registrations_without_building_the_servers_container()
    {
        var validate = async () => await new GameGeneration().ValidateAsync(CancellationToken.None);

        await validate.Should().NotThrowAsync();
        CompositionRoot.IsBuilt.Should().BeFalse("validation builds and discards a container of its own");
    }

    [Test]
    public void Activation_and_retirement_go_to_the_servers_bootstrapper()
    {
        var boot = new HostedServerBoot();

        var activateWithoutHost = () => boot.Activate(null);
        var retireUnbooted = () => boot.Retire();

        activateWithoutHost.Should().Throw<ArgumentNullException>("BootstrapHosted needs what the host hands the game");
        retireUnbooted.Should().Throw<InvalidOperationException>().WithMessage("*not booted by the host*");
        CompositionRoot.IsBuilt.Should().BeFalse();
    }
}
