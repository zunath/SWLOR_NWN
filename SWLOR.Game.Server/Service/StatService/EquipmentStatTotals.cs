using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service.StatService
{
    /// <summary>
    /// What worn gear grants to the equipment records, counted the way equipping counts it.
    /// </summary>
    public sealed class EquipmentStatTotals
    {
        private readonly Dictionary<CombatDamageType, int> _defenses = new();

        public int Evasion { get; private set; }
        public int FP { get; private set; }
        public int Stamina { get; private set; }

        public int GetDefense(CombatDamageType type) => _defenses.TryGetValue(type, out var amount) ? amount : 0;

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
            }
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
            _ => EquipmentRecordStats.None
        };
    }
}
