using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The impact messages of the ability split (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>, part 15) over the bench's
/// messaging and recorder stand-ins, with the combat log text answered as <see cref="TestAbilityService"/> answered it before
/// the move.
/// </summary>
public sealed class TestAbilityImpactMessageService : AbilityImpactMessageService
{
    public TestAbilityImpactMessageService(IMessagingService messaging, ICombatRecorderService combatRecorder)
        : base(messaging, combatRecorder, FacadeForwarder.For<ICombatFeedbackService>())
    {
    }

    protected override string BuildAbilityCombatLogMessage(uint observer, uint attacker, uint defender, string abilityName, int attackResultType, int chanceToHit) =>
        $"{abilityName}:{attackResultType}:{chanceToHit}";

    protected override string BuildAbilityNoTargetCombatLogMessage(uint observer, uint attacker, string abilityName) => $"{abilityName}:no target";
}
