using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

// Part 1 until the duration part moves: the active ability impact the refresh and the restart read (part 3 has its own seam in TestStatusEffectApplicationService).
public sealed partial class TestStatusEffectService
{
    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _bench.ActiveImpact!;
}
