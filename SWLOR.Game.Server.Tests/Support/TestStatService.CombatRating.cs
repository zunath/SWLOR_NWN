using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Support;

// Part 7, CombatRatingService (Combat): Attack, Accuracy, Defense and Evasion.
public sealed partial class TestStatService
{
    protected override SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => _bench.BaseItemSkill;

    protected override AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType) => _bench.WeaponDamageAbility;

    protected override AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType) => _bench.WeaponAccuracyAbility;

    protected override int GetNearbyStatusTargetAttackAdjustment(uint creature) => _bench.NearbyStatusTargetAdjustment;

    protected override int GetLowHPAttackAdjustment(uint creature) => _bench.LowHPAdjustment;

    protected override int GetLowFPAttackAdjustment(uint creature) => _bench.LowFPAdjustment;

    protected override bool IsRangedDamageSkill(SkillType skillType) => _bench.RangedSkills.Contains(skillType);
}
