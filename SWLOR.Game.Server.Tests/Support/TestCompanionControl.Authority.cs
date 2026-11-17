#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Support;

// Part CC-4, CompanionTargetAuthorityService: the weapon range, the perk, the feat and the ability use.
public sealed partial class TestCompanionControl
{
    protected override DateTime GetLastOffensiveActivityAt(uint creature) =>
        OffensiveAt.GetValueOrDefault(creature);

    protected override SkillType GetEquippedWeaponSkillType(uint creature) => SkillType.Invalid;

    protected override float GetWeaponEngagementRange(SkillType skill) => WeaponRange;

    protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);

    protected override bool IsFeatRegistered(FeatType feat) => Registered.ContainsKey(feat);

    protected override AbilityDetail GetAbilityDetail(FeatType feat)
    {
        AbilityLookups++;
        return Registered[feat];
    }

    protected override bool CanUseAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation)
    {
        UsedLevels.Add(effectiveLevel);
        return UsableFeats.Contains(feat);
    }
}
