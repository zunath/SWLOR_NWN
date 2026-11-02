using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.StatService
{
    /// <summary>
    /// What worn gear grants to the equipment records, counted the way equipping counts it.
    /// </summary>
    public sealed class EquipmentStatTotals
    {
        private readonly Dictionary<CombatDamageType, int> _defenses = new();
        private readonly Dictionary<SkillType, int> _control = new();
        private readonly Dictionary<SkillType, int> _craftsmanship = new();
        private readonly Dictionary<SkillType, int> _cpBonus = new();

        /// <summary>
        /// The crafting skills gear can grant Control, Craftsmanship and CP to, in iprp_crafttype.2da subtype order.
        /// </summary>
        public static readonly SkillType[] CraftSkills =
        {
            SkillType.Smithery, SkillType.Engineering, SkillType.Fabrication, SkillType.Agriculture,
        };

        public int Evasion { get; private set; }
        public int FP { get; private set; }
        public int Stamina { get; private set; }

        public int GetDefense(CombatDamageType type) => _defenses.TryGetValue(type, out var amount) ? amount : 0;
        public int GetControl(SkillType skill) => _control.TryGetValue(skill, out var amount) ? amount : 0;
        public int GetCraftsmanship(SkillType skill) => _craftsmanship.TryGetValue(skill, out var amount) ? amount : 0;
        public int GetCPBonus(SkillType skill) => _cpBonus.TryGetValue(skill, out var amount) ? amount : 0;

        /// <summary>
        /// The crafting skill a Control, Craftsmanship or CP Bonus subtype grants to (iprp_crafttype.2da), or Invalid.
        /// </summary>
        public static SkillType GetCraftSkill(int subtype) =>
            subtype >= 1 && subtype <= CraftSkills.Length ? CraftSkills[subtype - 1] : SkillType.Invalid;

        /// <summary>
        /// Counts one item property. Retired elemental Defense never counted on the wearer, so it is skipped here too.
        /// </summary>
        public void Add(ItemPropertyType type, int subtype, int amount)
        {
            switch (type)
            {
                case ItemPropertyType.Defense:
                    var damageType = (CombatDamageType)subtype;
                    if (damageType.IsDefenseDamageType())
                        _defenses[damageType] = GetDefense(damageType) + amount;
                    break;
                case ItemPropertyType.Evasion:
                    Evasion += amount;
                    break;
                case ItemPropertyType.FP:
                    FP += amount;
                    break;
                case ItemPropertyType.Stamina:
                    Stamina += amount;
                    break;
                case ItemPropertyType.Control:
                    AddCraftStat(_control, subtype, amount);
                    break;
                case ItemPropertyType.Craftsmanship:
                    AddCraftStat(_craftsmanship, subtype, amount);
                    break;
                case ItemPropertyType.CPBonus:
                    AddCraftStat(_cpBonus, subtype, amount);
                    break;
            }
        }

        private static void AddCraftStat(Dictionary<SkillType, int> stats, int subtype, int amount)
        {
            var skill = GetCraftSkill(subtype);
            if (skill != SkillType.Invalid)
                stats[skill] = (stats.TryGetValue(skill, out var current) ? current : 0) + amount;
        }

        /// <summary>
        /// The records an item property feeds.
        /// </summary>
        public static EquipmentRecordStats GetRecordStat(ItemPropertyType type) => type switch
        {
            ItemPropertyType.Defense => EquipmentRecordStats.Defense,
            ItemPropertyType.Evasion => EquipmentRecordStats.Evasion,
            ItemPropertyType.FP => EquipmentRecordStats.FP,
            ItemPropertyType.Stamina => EquipmentRecordStats.Stamina,
            ItemPropertyType.Control => EquipmentRecordStats.Control,
            ItemPropertyType.Craftsmanship => EquipmentRecordStats.Craftsmanship,
            ItemPropertyType.CPBonus => EquipmentRecordStats.CPBonus,
            _ => EquipmentRecordStats.None
        };
    }
}
