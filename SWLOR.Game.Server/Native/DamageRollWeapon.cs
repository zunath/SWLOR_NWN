using NWN.Native.API;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// The weapon's side of the native damage roll: its DMG rating and damage type, the ability that scales it, the
    /// attribute-delta cap its skill requirement sets, and the combat mode and Might bonuses.
    /// </summary>
    public static class DamageRollWeapon
    {
        private const int PowerAttackDamageBonus = 3;
        private const int ImprovedPowerAttackDamageBonus = 6;
        private const int DefaultPhysicalDamage = 1;
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;

        internal static WeaponDamageProfile ExtractWeaponDamageProfile(CNWSItem weapon)
        {
            var damageType = CombatDamageType.Physical;
            var damage = 0;
            var hasDamageProperty = false;

            if (weapon != null)
            {
                for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                {
                    var ip = weapon.GetPassiveProperty(index);
                    if (ip == null)
                        continue;

                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.DMG)
                    {
                        damage += ip.m_nCostTableValue;
                        hasDamageProperty = true;
                    }
                    else if (ip.m_nPropertyName == (ushort)ItemPropertyType.WeaponDamageType)
                    {
                        damageType = ResolveWeaponDamageType(damageType, ip.m_nSubType);
                    }
                }
            }

            // A damage type only selects the type of a real DMG property. Items without DMG use
            // the unarmed/default physical fallback instead of manufacturing elemental damage.
            if (!hasDamageProperty)
            {
                return new WeaponDamageProfile(CombatDamageType.Physical, DefaultPhysicalDamage, false);
            }

            return new WeaponDamageProfile(damageType, damage);
        }

        private static CombatDamageType ResolveWeaponDamageType(CombatDamageType current, int damageTypeId)
        {
            return WeaponDamage.ResolveDamageType(current, damageTypeId);
        }

        internal static AbilityType GetWeaponDamageAbilityType(uint attacker, CNWSItem weapon)
        {
            if (weapon == null) return AbilityType.Might;

            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.DamageStat)
                {
                    return (AbilityType)ip.m_nSubType;
                }
            }

            return GameServices.CombatQueries.GetWeaponDamageAbilityType(attacker, (BaseItem)weapon.m_nBaseItem);
        }

        internal static int GetWeaponDeltaCap(CNWSItem weapon)
        {
            var requiredSkillRank = GetWeaponRequiredSkillRank(weapon);
            return requiredSkillRank < 0
                ? 0
                : GetWeaponDeltaCapFromRequiredSkillRank(requiredSkillRank);
        }

        private static int GetWeaponRequiredSkillRank(CNWSItem weapon)
        {
            if (weapon == null) return -1;

            var requiredSkillRank = -1;
            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.RequiresSkill)
                {
                    requiredSkillRank = Math.Max(requiredSkillRank, ip.m_nCostTableValue);
                }
            }

            return requiredSkillRank;
        }

        private static int GetWeaponDeltaCapFromRequiredSkillRank(int requiredSkillRank)
        {
            return requiredSkillRank <= 0
                ? 1
                : Math.Clamp((requiredSkillRank / 10) + 1, 1, 6);
        }

        internal static WeaponDamageProfile ApplyCombatModeBonus(CNWSCreature attacker, WeaponDamageProfile damageProfile)
        {
            switch (attacker?.m_nCombatMode)
            {
                case PowerAttackMode:
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + PowerAttackDamageBonus, damageProfile.HasItemDamage);
                case ImprovedPowerAttackMode:
                    return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + ImprovedPowerAttackDamageBonus, damageProfile.HasItemDamage);
                default:
                    return damageProfile;
            }
        }

        internal static WeaponDamageProfile ApplyMightModifierDamageBonus(CNWSCreature attacker, CNWSItem weapon, WeaponDamageProfile damageProfile)
        {
            if (attacker == null)
                return damageProfile;

            var mightModifier = Math.Max(0, Stat.GetStatValueNative(attacker, AbilityType.Might));
            if (mightModifier <= 0)
                return damageProfile;

            var multiplier = Stat.GetStatAdjustment(attacker.m_idSelf, StatType.WeaponMightModifierDamageMultiplier);
            if (weapon != null && Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
            {
                multiplier += Stat.GetStatAdjustment(attacker.m_idSelf, StatType.StaffMightModifierDamageMultiplier);
            }

            if (multiplier <= 0)
                return damageProfile;

            return new WeaponDamageProfile(damageProfile.DamageType, damageProfile.Damage + mightModifier * multiplier, damageProfile.HasItemDamage);
        }
    }
}
