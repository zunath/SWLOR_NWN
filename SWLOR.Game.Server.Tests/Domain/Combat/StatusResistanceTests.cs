using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// <c>IStatusResistance</c>, the port the status effects read their resistances through: the container hands out the resistance
/// service itself, so the port answers what <c>ResistanceService</c> answers.
/// </summary>
[TestFixture]
public class StatusResistanceTests
{
    [Test]
    public void ThePortIsTheResistanceServiceInstance()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IStatusResistance>().Should().BeSameAs(provider.GetRequiredService<IResistanceService>());
    }

    [Test]
    public void TheResistanceServiceIsAStatusResistance()
    {
        typeof(IResistanceService).GetInterfaces().Should().Contain(typeof(IStatusResistance));
        typeof(IStatusResistance).GetMethods().Select(method => method.Name).Should().BeEquivalentTo(
            nameof(IStatusResistance.IsValidResistanceType),
            nameof(IStatusResistance.HasImmunity),
            nameof(IStatusResistance.CalculateResistedTicks));
    }

    [Test]
    public void ThePortAnswersWhatTheServiceAnswers()
    {
        var service = new ResistanceService(new InMemoryDBService(), FacadeForwarder.For<IMimicryService>(),
            FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>());
        IStatusResistance port = service;

        port.IsValidResistanceType(ResistanceType.Fire).Should().Be(service.IsValidResistanceType(ResistanceType.Fire)).And.BeTrue();
        port.IsValidResistanceType(ResistanceType.Invalid).Should().BeFalse();
        port.CalculateResistedTicks(1u, ResistanceType.Invalid, 9).Should().Be(9);
    }
}
