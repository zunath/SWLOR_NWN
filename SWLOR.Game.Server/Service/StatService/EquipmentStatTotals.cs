using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

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
        private readonly Dictionary<ResistanceType, int> _resistances = new();
        private readonly Dictionary<EquipmentRecordStats, int> _summed = new();

        /// <summary>
        /// Item properties that feed one summed number on the record, with the record they feed.
        /// </summary>
        private static readonly Dictionary<ItemPropertyType, EquipmentRecordStats> SummedRecords = new()
        {
            [ItemPropertyType.FPRegen] = EquipmentRecordStats.FPRegen,
            [ItemPropertyType.STMRegen] = EquipmentRecordStats.STMRegen,
            [ItemPropertyType.Attack] = EquipmentRecordStats.Attack,
            [ItemPropertyType.Accuracy] = EquipmentRecordStats.Accuracy,
            [ItemPropertyType.ForceAttack] = EquipmentRecordStats.ForceAttack,
            [ItemPropertyType.Stealth] = EquipmentRecordStats.Stealth,
            [ItemPropertyType.Detection] = EquipmentRecordStats.Detection,
            [ItemPropertyType.TrapBonus] = EquipmentRecordStats.TrapBonus,
            [ItemPropertyType.Disarm] = EquipmentRecordStats.TrapDisarm,
            [ItemPropertyType.PoisonBonus] = EquipmentRecordStats.PoisonBonus,
            [ItemPropertyType.Lockpicking] = EquipmentRecordStats.Lockpicking,
        };

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
        public int GetResistance(ResistanceType type) => _resistances.TryGetValue(type, out var amount) ? amount : 0;

        /// <summary>
        /// What the gear grants to one summed record, such as Attack or Stealth.
        /// </summary>
        public int GetTotal(EquipmentRecordStats record) => _summed.TryGetValue(record, out var amount) ? amount : 0;

        /// <summary>
        /// The crafting skill a Control, Craftsmanship or CP Bonus subtype grants to (iprp_crafttype.2da), or Invalid.
        /// </summary>
        public static SkillType GetCraftSkill(int subtype) =>
            subtype >= 1 && subtype <= CraftSkills.Length ? CraftSkills[subtype - 1] : SkillType.Invalid;

        /// <summary>
        /// Counts one item property of an item of the given base type. Retired elemental Defense never counted on the
        /// wearer, and a weapon's (or creature item's) Accuracy belongs to that weapon, so both are skipped here too.
        /// </summary>
        public void Add(ItemPropertyType type, int subtype, int amount, BaseItem baseItem = BaseItem.Invalid)
        {
            if (type == ItemPropertyType.Accuracy &&
                (Item.IsAttackWeaponType(baseItem) || baseItem == BaseItem.CreatureItem))
                return;

            if (SummedRecords.TryGetValue(type, out var record))
            {
                _summed[record] = GetTotal(record) + amount;
                return;
            }

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
                case ItemPropertyType.Resistance:
                    var resistanceType = (ResistanceType)subtype;
                    if (Resistance.IsValidResistanceType(resistanceType))
                        _resistances[resistanceType] = GetResistance(resistanceType) +
                                                       Resistance.DecodeItemPropertyCostTableValue(amount);
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
            ItemPropertyType.Resistance => EquipmentRecordStats.Resistance,
            _ => SummedRecords.TryGetValue(type, out var record) ? record : EquipmentRecordStats.None
        };
    }
}
