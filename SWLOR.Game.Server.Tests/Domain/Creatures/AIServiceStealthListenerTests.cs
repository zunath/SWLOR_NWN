#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// The creature AI is the listener of stealth's detection notices: the boot handler of the aggro part gives stealth that part, so
/// the notices reach the aggro code once the module cache is built.
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
        var service = new AIAggroService(
            FacadeForwarder.For<IAIService>(), FacadeForwarder.For<IAIDecisionService>(), FacadeForwarder.For<IEnmityService>(),
            FacadeForwarder.For<IEspionageInfiltrationService>(), stealth);

        service.RegisterStealthAggroListener();

        registered.Should().BeSameAs(service);
    }
}
