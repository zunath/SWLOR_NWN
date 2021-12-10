using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using ObjectType = NWN.Native.API.ObjectType;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetDamageRoll
    {
        internal delegate int GetDamageRollHook(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax);
        // ReSharper disable once NotAccessedField.Local
        private static GetDamageRollHook _callOriginal;

        [NWNEventHandler("mod_load")]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, int, int, int, int, int, int> pHook = &OnGetDamageRoll;
            var hookPtr = Internal.NativeFunctions.RequestHook(new IntPtr(FunctionsLinux._ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii), (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetDamageRollHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int OnGetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        {
            var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
            var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
            var targetObject = CNWSObject.FromPointer(pTarget);
            var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();

            var dmg = 0f;
            var attackAttribute = attackerStats.m_nStrengthBase < 10 ? 0 : attackerStats.m_nStrengthModifier;
            var damage = 0;
            var specializationDMGBonus = 0f;

            // Calculate attacker's DMG
            if (attacker != null)
            {
                var weapon = bOffHand == 1
                    ? attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand)
                    : attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);

                if (weapon != null)
                {
                    specializationDMGBonus = CalculateSpecializationDMG(attacker, weapon);
                }

                // Nothing equipped - check gloves.
                if (weapon == null)
                {
                    weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.Arms);
                }

                // Gloves not equipped. Check claws
                if (weapon == null)
                {
                    weapon = bOffHand == 1
                        ? attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponLeft)
                        : attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureWeaponRight);
                }

                if (weapon != null)
                {
                    // Iterate over properties and take the highest DMG rating.
                    for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                    {
                        var ip = weapon.GetPassiveProperty(index);
                        if (ip != null && ip.m_nPropertyName == (ushort)ItemPropertyType.DMG)
                        {
                            if (ip.m_nCostTableValue > dmg)
                            {
                                dmg = Combat.GetDMGValueFromItemPropertyCostTableValue(ip.m_nCostTableValue);
                            }
                        }
                    }                   
                }
            }

            dmg += specializationDMGBonus;

            // Safety check - DMG minimum is 0.5
            if (dmg < 0.5f)
            {
                dmg = 0.5f;
            }

            // Combat Mode - Power Attack (+1.0 DMG)
            if (attacker?.m_nCombatMode == 2) // 2 = Power Attack
            {
                dmg += 1.0f;
            }
            // Combat Mode - Improved Power Attack (+2.5 DMG)
            else if (attacker?.m_nCombatMode == 3) // 3 = Improved Power Attack
            {
                dmg += 2.5f;
            }

            // Calculate total defense on the target.
            if (targetObject != null && targetObject.m_nObjectType == (int)ObjectType.Creature)
            {
                var target = CNWSCreature.FromPointer(pTarget);
                var damagePower = attackerStats.m_pBaseCreature.CalculateDamagePower(target, bOffHand);
                float vitality = target.m_pStats.m_nConstitutionModifier;
                var defense = Stat.GetDefenseNative(target, CombatDamageType.Physical);

                damage = Combat.CalculateDamage(dmg, attackAttribute, defense, vitality, bCritical == 1);

                // Plot target - zero damage
                if (target.m_bPlotObject == 1)
                {
                    damage = 0;
                }

                // Apply NWN mechanics to damage reduction
                damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1);
                damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1);
                if (damage < 0)
                    damage = 0;
            }

            return damage;
        }

        private static float CalculateSpecializationDMG(CNWSCreature attacker, CNWSItem weapon)
        {
            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            // Vibroblades
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationVibroblades) == 1)
            {
                return 0.5f;
            }

            // Finesse Vibroblades
            if (Item.FinesseVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationFinesseVibroblades) == 1)
            {
                return 0.5f;
            }

            // Lightsabers
            if (Item.LightsaberBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationLightsabers) == 1)
            {
                return 0.5f;
            }

            // Heavy Vibroblades
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationHeavyVibroblades) == 1)
            {
                return 0.5f;
            }

            // Polearms
            if (Item.PolearmBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationPolearms) == 1)
            {
                return 0.5f;
            }

            // Twin Blades
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationTwinBlades) == 1)
            {
                return 0.5f;
            }

            // Saberstaffs
            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationSaberstaffs) == 1)
            {
                return 0.5f;
            }

            // Katars
            if (Item.KatarBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationKatars) == 1)
            {
                return 0.5f;
            }

            // Staves
            if (Item.StaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_Staff) == 1)
            {
                return 0.5f;
            }

            // Pistols
            if (Item.PistolBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationPistol) == 1)
            {
                return 0.5f;
            }

            // Throwing Weapons
            if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationThrowingWeapons) == 1)
            {
                return 0.5f;
            }

            // Rifles
            if (Item.RifleBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationRifles) == 1)
            {
                return 0.5f;
            }

            return 0.0f;
        }
    }
}
