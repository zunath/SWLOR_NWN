using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

// Part 3, StatusEffectApplicationService: the active ability impact. The notices an application sends reach the listener stand-ins the bench registers.
public sealed partial class TestStatusEffectService
{
    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _bench.ActiveImpact!;
}
