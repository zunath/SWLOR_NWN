using System.Numerics;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The answers of <see cref="TestAbilityService"/> for the per-target hit roll and resolution (parts 10 and 11). When the ability split
/// (<c>Readmes/MaintainabilityRefactorAbilitySplit.md</c>) moves the part, these overrides become its own double; the
/// answers come from the bench, so the tests that set them do not change.
/// </summary>
public sealed partial class TestAbilityService
{
    protected override void TrackHostileAbilityActivity(uint creature) => Log($"TrackHostileActivity:{creature}");

    protected override void TrackHostileDefensiveCombatEntryActivity(uint creature, uint attacker) => Log($"TrackDefensiveEntry:{creature}:{attacker}");

    protected override int GetAbilityStatusCategoryHitChancePercentAdjustment(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;

    protected override bool TryResolveAbilityHit(uint attacker, uint defender, SkillType skillType, PerkType perkType, out int hitRate, int hitChancePercentAdjustment, int skillLevelOverride, AbilityType statOverride, int accuracyFlatAdjustment)
    {
        Log($"ResolveHit:{hitChancePercentAdjustment}:{skillLevelOverride}:{statOverride}:{accuracyFlatAdjustment}");
        hitRate = _bench.HitRate;
        return _bench.HitResolves;
    }

    protected override int GetAbilityImpactBaseDamageBonus(uint activator, uint target, AbilityDetail ability, SkillType skillType) => _bench.FlatBaseDamageBonus;

    protected override int GetAbilityStatusCategoryDamageBonus(uint creature, SkillType skillType, StatusEffectCategory appliedCategories) => 0;
}
