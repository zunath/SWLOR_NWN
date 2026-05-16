using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Extension;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using NativeDamageType = NWN.Native.API.DamageType;
using NWNScriptDamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;

namespace SWLOR.Game.Server.Service.CombatService
{
    public enum CombatDamageType
    {
        [CombatDamage(
            CombatDamageCategoryType.Invalid,
            CombatDamageType.Invalid,
            AbilityType.Invalid,
            ResistanceType.Invalid,
            ResistanceType.Invalid,
            NWNScriptDamageType.Slashing,
            NativeDamageType.Slashing)]
        Invalid = 0,

        [CombatDamage(
            CombatDamageCategoryType.Physical,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Invalid,
            ResistanceType.Trauma,
            NWNScriptDamageType.Slashing,
            NativeDamageType.Slashing)]
        Physical = 1,

        [CombatDamage(
            CombatDamageCategoryType.Force,
            CombatDamageType.Force,
            AbilityType.Willpower,
            ResistanceType.Invalid,
            ResistanceType.Disruption,
            NWNScriptDamageType.Force,
            NativeDamageType.Magical)]
        Force = 2,

        [CombatDamage(
            CombatDamageCategoryType.Elemental,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Fire,
            ResistanceType.Fire,
            NWNScriptDamageType.Fire,
            NativeDamageType.Fire)]
        Fire = 3,

        [CombatDamage(
            CombatDamageCategoryType.Elemental,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Poison,
            ResistanceType.Poison,
            NWNScriptDamageType.Acid,
            NativeDamageType.Acid)]
        Poison = 4,

        [CombatDamage(
            CombatDamageCategoryType.Elemental,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Electrical,
            ResistanceType.Electrical,
            NWNScriptDamageType.Electrical,
            NativeDamageType.Electrical)]
        Electrical = 5,

        [CombatDamage(
            CombatDamageCategoryType.Elemental,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Ice,
            ResistanceType.Ice,
            NWNScriptDamageType.Cold,
            NativeDamageType.Cold)]
        Ice = 6,

        [CombatDamage(
            CombatDamageCategoryType.Elemental,
            CombatDamageType.Physical,
            AbilityType.Vitality,
            ResistanceType.Invalid,
            ResistanceType.Disruption,
            NWNScriptDamageType.Sonic,
            NativeDamageType.Sonic)]
        Sonic = 7,

        [CombatDamage(
            CombatDamageCategoryType.Ship,
            CombatDamageType.Invalid,
            AbilityType.Invalid,
            ResistanceType.Invalid,
            ResistanceType.Invalid,
            NWNScriptDamageType.Slashing,
            NativeDamageType.Slashing)]
        Thermal = 20,

        [CombatDamage(
            CombatDamageCategoryType.Ship,
            CombatDamageType.Invalid,
            AbilityType.Invalid,
            ResistanceType.Invalid,
            ResistanceType.Invalid,
            NWNScriptDamageType.Slashing,
            NativeDamageType.Slashing)]
        Explosive = 21,

        [CombatDamage(
            CombatDamageCategoryType.Ship,
            CombatDamageType.Invalid,
            AbilityType.Invalid,
            ResistanceType.Invalid,
            ResistanceType.Invalid,
            NWNScriptDamageType.Slashing,
            NativeDamageType.Slashing)]
        EM = 22,
    }

    public class CombatDamageAttribute : Attribute
    {
        public CombatDamageCategoryType Category { get; }
        public CombatDamageType DefenseDamageType { get; }
        public AbilityType DefenseAbility { get; }
        public ResistanceType ElementalResistanceType { get; }
        public ResistanceType SourceResistanceType { get; }
        public NWNScriptDamageType NWScriptDamageType { get; }
        public NativeDamageType NativeDamageType { get; }

        public CombatDamageAttribute(
            CombatDamageCategoryType category,
            CombatDamageType defenseDamageType,
            AbilityType defenseAbility,
            ResistanceType elementalResistanceType,
            ResistanceType sourceResistanceType,
            NWNScriptDamageType nWScriptDamageType,
            NativeDamageType nativeDamageType)
        {
            Category = category;
            DefenseDamageType = defenseDamageType;
            DefenseAbility = defenseAbility;
            ElementalResistanceType = elementalResistanceType;
            SourceResistanceType = sourceResistanceType;
            NWScriptDamageType = nWScriptDamageType;
            NativeDamageType = nativeDamageType;
        }
    }

    public static class CombatDamageTypeExtensions
    {
        private static readonly IReadOnlyDictionary<CombatDamageType, CombatDamageAttribute> DamageAttributes =
            Enum.GetValues(typeof(CombatDamageType))
                .Cast<CombatDamageType>()
                .ToDictionary(
                    type => type,
                    type => type.GetAttribute<CombatDamageType, CombatDamageAttribute>());

        public static CombatDamageAttribute GetDetails(this CombatDamageType type)
        {
            if (!DamageAttributes.TryGetValue(type, out var details))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(type),
                    type,
                    $"No {nameof(CombatDamageAttribute)} is registered for combat damage type {type}.");
            }

            return details;
        }

        public static bool IsCharacterDamageType(this CombatDamageType type)
        {
            var category = type.GetDetails().Category;
            return category == CombatDamageCategoryType.Physical ||
                   category == CombatDamageCategoryType.Force ||
                   category == CombatDamageCategoryType.Elemental;
        }

        public static bool IsDefenseDamageType(this CombatDamageType type)
        {
            var category = type.GetDetails().Category;
            return category == CombatDamageCategoryType.Physical ||
                   category == CombatDamageCategoryType.Force;
        }

        public static bool IsElementalDamageType(this CombatDamageType type)
        {
            return type.GetDetails().Category == CombatDamageCategoryType.Elemental;
        }

        public static bool IsPhysicalDamageType(this CombatDamageType type)
        {
            return type.GetDetails().Category == CombatDamageCategoryType.Physical;
        }

        public static CombatDamageType GetDefenseDamageType(this CombatDamageType type)
        {
            return type.GetDetails().DefenseDamageType;
        }

        public static AbilityType GetDefenseAbilityType(this CombatDamageType type)
        {
            return type.GetDetails().DefenseAbility;
        }

        public static bool TryGetElementalResistanceType(this CombatDamageType type, out ResistanceType resistanceType)
        {
            resistanceType = type.GetDetails().ElementalResistanceType;
            return resistanceType != ResistanceType.Invalid;
        }

        public static bool TryGetSourceResistanceType(this CombatDamageType type, out ResistanceType resistanceType)
        {
            resistanceType = type.GetDetails().SourceResistanceType;
            return resistanceType != ResistanceType.Invalid;
        }

        public static NWNScriptDamageType GetNWScriptDamageType(this CombatDamageType type)
        {
            return type.GetDetails().NWScriptDamageType;
        }

        public static NativeDamageType GetNativeDamageType(this CombatDamageType type)
        {
            return type.GetDetails().NativeDamageType;
        }
    }
}
