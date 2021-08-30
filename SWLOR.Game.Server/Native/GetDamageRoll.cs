using System;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = SWLOR.Game.Server.Core.NWScript.Enum.Item.EquipmentSlot;

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
            var creatureStats = CNWSCreatureStats.FromPointer(thisPtr);
            var creature = CNWSCreature.FromPointer(creatureStats.m_pBaseCreature);
            var targetObject = CNWSObject.FromPointer(pTarget);
            var damageFlags = creatureStats.m_pBaseCreature.GetDamageFlags();
            
            var dmg = 0f;
            var attackAttribute = creatureStats.m_nStrengthBase < 10 ? 0 : creatureStats.m_nStrengthModifier;
            var damage = 0;
            
            // Calculate attacker's DMG
            if (creature != null)
            {
                var weapon = bOffHand == 1
                    ? creature.m_pInventory.GetItemInSlot((uint) EquipmentSlot.LeftHand)
                    : creature.m_pInventory.GetItemInSlot((uint) EquipmentSlot.RightHand);

                // Nothing equipped - check gloves.
                if (weapon == null)
                {
                    weapon = creature.m_pInventory.GetItemInSlot((uint) EquipmentSlot.Arms);
                }

                // Gloves not equipped. Check claws
                if (weapon == null)
                {
                    weapon = bOffHand == 1
                        ? creature.m_pInventory.GetItemInSlot((uint) EquipmentSlot.CreatureWeaponLeft)
                        : creature.m_pInventory.GetItemInSlot((uint) EquipmentSlot.CreatureWeaponRight);
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

                    // Ranged weapons use Perception (NWN's DEX)
                    // All others use Might (NWN's STR)
                    if (weapon.m_nBaseItem == (uint) BaseItem.Rifle ||
                        weapon.m_nBaseItem == (uint) BaseItem.Pistol ||
                        weapon.m_nBaseItem == (uint) BaseItem.Cannon ||
                        weapon.m_nBaseItem == (uint) BaseItem.Longbow ||
                        weapon.m_nBaseItem == (uint) BaseItem.Sling)
                    {
                        attackAttribute = creatureStats.m_nDexterityBase < 10 ? 0 : creatureStats.m_nDexterityModifier;
                    }
                }
            }

            // Safety check - DMG minimum is 0.5
            if (dmg < 0.5f)
            {
                dmg = 0.5f;
            }

            // Combat Mode - Power Attack (+1.0 DMG)
            if (creature?.m_nCombatMode == 2) // 2 = Power Attack
            {
                dmg += 1.0f;
            }
            // Combat Mode - Improved Power Attack (+2.5 DMG)
            else if (creature?.m_nCombatMode == 3) // 3 = Improved Power Attack
            {
                dmg += 2.5f;
            }

            // Calculate total defense on the target.
            if (targetObject != null && targetObject.m_nObjectType == (int)ObjectType.Creature)
            {
                var target = CNWSCreature.FromPointer(pTarget);
                var damagePower = creatureStats.m_pBaseCreature.CalculateDamagePower(target, bOffHand);
                float vitality = target.m_pStats.m_nConstitutionModifier;
                var defense = Stat.GetDefenseNative(target, CombatDamageType.Physical);

                damage = Combat.CalculateDamage(dmg, attackAttribute, defense, vitality, bCritical == 1);

                // Plot target - zero damage
                if (target.m_bPlotObject == 1)
                {
                    damage = 0;
                }

                // Apply NWN mechanics to damage reduction
                damage = target.DoDamageImmunity(creature, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(creature, damage, damageFlags, 0, 1, 1);
                damage = target.DoDamageReduction(creature, damage, damagePower, 0, 1);
                if (damage < 0)
                    damage = 0;
            }

            return damage;
        }
    }
}
