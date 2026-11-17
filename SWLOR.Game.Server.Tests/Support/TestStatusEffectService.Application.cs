using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

// Part 3, StatusEffectApplicationService: the active ability impact and the unconverted notices an application sends (the
// hostile party buff is also part 4's, after a restart).
public sealed partial class TestStatusEffectService
{
    protected override AbilityImpactSummary GetActiveAbilityImpactSummary(uint activator) => _bench.ActiveImpact!;

    protected override void ApplyStatusAppliedTargetStaminaDrain(uint activator, uint target, StatusEffectCategory appliedCategories) =>
        _bench.Log.Add($"StaminaDrain:{activator}:{target}:{appliedCategories}");

    protected override void ApplyHostileAbilityPartyBuff(uint activator, AbilityDetail ability, int damage, bool statusApplied) =>
        _bench.Log.Add($"HostileBuff:{activator}:{ability?.Name}:{damage}:{statusApplied}");
}
