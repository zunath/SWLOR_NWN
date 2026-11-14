using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Tests.Core;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// The composition root builds what a hot reload's player and state steps need from the domains (<see cref="ReloadWiring"/>, P4-15):
/// a carrier whose ability converter reads the container's ability service, and the players' screens over the container's GUI and
/// conversation services.
/// </summary>
[TestFixture]
public class ReloadWiringTests
{
    [Test]
    public void The_state_carrier_writes_a_carried_class_through_the_containers_services()
    {
        var container = ScriptHandlerTestSupport.ServerContainer;
        var carrier = ReloadWiring.CreateStateCarrier(container);
        var (type, fields) = ReloadStateCarrier.CarriedClasses(typeof(ReloadWiring).Assembly)
            .Single(entry => entry.Type.Name == "StatusEffectService");

        var text = carrier.Write(fields, container.ResolveHandlerOwner(type));

        text.Should().StartWith("{").And.Contain("_creatureEffects");
    }

    [Test]
    public void The_screen_carry_is_the_ui_domains_over_the_containers_services()
    {
        ReloadWiring.CreateScreenCarry(ScriptHandlerTestSupport.ServerContainer).Should().BeOfType<PlayerScreenCarry>();
    }

    [Test]
    public void Both_need_a_container()
    {
        var carrier = () => ReloadWiring.CreateStateCarrier(null);
        var screens = () => ReloadWiring.CreateScreenCarry(null);

        carrier.Should().Throw<ArgumentNullException>();
        screens.Should().Throw<ArgumentNullException>();
    }
}
