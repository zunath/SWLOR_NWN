#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The creature AI is the listener of stealth's detection notices: the boot handler gives stealth this service, so the
/// notices reach the aggro code once the module cache is built.
/// </summary>
[TestFixture]
public class AIServiceStealthListenerTests
{
    [Test]
    public void The_boot_handler_registers_the_AI_service_with_stealth()
    {
        var stealth = ScriptedProxy.Create<IStealthService>(out var script);
        IStealthAggroListener? registered = null;
        script.On("RegisterAggroListener", arguments =>
        {
            registered = (IStealthAggroListener)arguments[0]!;
            return null;
        });
        var service = new AIService(
            FacadeForwarder.For<IPartyRoster>(), FacadeForwarder.For<ICompanionLookup>(),
            FacadeForwarder.For<IDMPresenceService>(), FacadeForwarder.For<IAbilityActivationService>(),
            FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActivityService>(),
            FacadeForwarder.LazyFor<ICompanionControlService>(), FacadeForwarder.For<IConversationMenuService>(),
            FacadeForwarder.For<IConversationService>(), FacadeForwarder.LazyFor<IEnmityService>(),
            FacadeForwarder.LazyFor<IEspionageInfiltrationService>(), FacadeForwarder.For<IPerkService>(),
            FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IStatusEffectService>(), stealth);

        service.RegisterStealthAggroListener();

        registered.Should().BeSameAs(service);
    }
}
