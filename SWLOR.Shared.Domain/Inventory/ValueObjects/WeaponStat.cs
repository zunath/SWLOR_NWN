using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Shared.Domain.Inventory.ValueObjects
{
    public class WeaponStat
    {
        public int DMG { get; set; } 
        public int Delay { get; set; }
        public int Tier { get; set; }
        public uint Item { get; set; } = OBJECT_INVALID;
        public CombatDamageType DamageType { get; set; } = CombatDamageType.Physical;
        public AbilityType AccuracyStat { get; set; }
        public AbilityType DamageStat { get; set; }
        public SkillType Skill { get; set; }
    }
}
