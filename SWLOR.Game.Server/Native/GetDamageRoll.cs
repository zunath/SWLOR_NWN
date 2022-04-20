using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.Game.Server.Core.NWScript.Enum.InventorySlot;
using ObjectType = NWN.Native.API.ObjectType;
using RacialType = SWLOR.Game.Server.Core.NWScript.Enum.RacialType;
using Random = SWLOR.Game.Server.Service.Random;
using DamageType = NWN.Native.API.DamageType;

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
            var hookPtr = VM.RequestHook(new IntPtr(FunctionsLinux._ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii), (IntPtr)pHook, -1000000);
            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetDamageRollHook>(hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int OnGetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        {
            var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
            var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
            var targetObject = CNWSObject.FromPointer(pTarget);
            var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
            var pCombatRound = attacker.m_pcCombatRound;
            var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);

            // On a critical hit, this method appears to be invoked multiple times, with an invalid target the second and
            // subsequent times.  Bail out early.
            if (targetObject == null || targetObject.m_idSelf == NWScript.OBJECT_INVALID) return 0;

            uint attackType = (uint) AttackType.Melee;
            CNWSItem weapon = bOffHand == 1
                    ? attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand)
                    : attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);

            // Twin blades need special handling. 
            if (bOffHand == 1 && weapon == null) weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand); 

            if (weapon == null)
            {
                // Check for creature weapons.  Randomise the order of slots.
                uint slot1 = (uint)EquipmentSlot.CreatureWeaponBite;
                uint slot2 = (uint)EquipmentSlot.CreatureWeaponLeft;
                uint slot3= (uint)EquipmentSlot.CreatureWeaponRight;

                switch (Random.Next(1, 3))
                {
                    case 2:
                        slot3 = (uint)EquipmentSlot.CreatureWeaponBite;
                        slot1 = (uint)EquipmentSlot.CreatureWeaponLeft;
                        slot2 = (uint)EquipmentSlot.CreatureWeaponRight;
                        break;
                    case 3:
                        slot2 = (uint)EquipmentSlot.CreatureWeaponBite;
                        slot3 = (uint)EquipmentSlot.CreatureWeaponLeft;
                        slot1 = (uint)EquipmentSlot.CreatureWeaponRight;
                        break;
                }

                weapon = attacker.m_pInventory.GetItemInSlot(slot1);
                if (weapon == null) weapon = attacker.m_pInventory.GetItemInSlot(slot2);
                if (weapon == null) weapon = attacker.m_pInventory.GetItemInSlot(slot3);
            }

            // Figure out what sort of attack this is.  Note - damage from abilities does not go through this function.
            if (attacker.GetRangeWeaponEquipped() == 1)
            {
                attackType = (uint) AttackType.Ranged;
            }

            // Override based on variables.
            var attackOverride = attacker.m_ScriptVars.GetInt(new CExoString("ATTACK_TYPE_OVERRIDE"));

            if (attackOverride != 0 & attackOverride < 4)
            {
                attackType = (uint)attackOverride;
                attacker.m_ScriptVars.DestroyInt(new CExoString("ATTACK_TYPE_OVERRIDE"));
            }
            else if (weapon != null)
            {
                attackOverride = weapon.m_ScriptVars.GetInt(new CExoString("ATTACK_TYPE_OVERRIDE"));

                if (attackOverride != 0 & attackOverride < 4)
                {
                    attackType = (uint)attackOverride;
                    attacker.m_ScriptVars.DestroyInt(new CExoString("ATTACK_TYPE_OVERRIDE"));
                }
            }

            // We have now resolved what sort of attack we are doing.
            Log.Write(LogGroup.Attack, "DAMAGE: Attacker: " + attacker.GetFirstName().GetSimple() + ", PC?: " + attacker.m_bPlayerCharacter +
                ", Defender " + targetObject.GetFirstName().GetSimple() + ", object type " + targetObject.m_nObjectType + ", Attack type: " + attackType + ", weapon " + 
                                        (weapon == null ? "None" : weapon.GetFirstName().GetSimple()));

            // Initialise damage array to read properties from equipped weapon.
            Dictionary<CombatDamageType, float> dmgValues = new Dictionary<CombatDamageType, float>();
            dmgValues[CombatDamageType.Physical] = 0;
            dmgValues[CombatDamageType.Force] = 0;
            dmgValues[CombatDamageType.Fire] = 0;
            dmgValues[CombatDamageType.Electrical] = 0;
            dmgValues[CombatDamageType.Poison] = 0;
            dmgValues[CombatDamageType.Ice] = 0;
            var physicalDamage = 0;
            var specializationDMGBonus = 0f;
            var foundDMG = false;

            // Calculate attacker's base DMG
            if (attacker != null)
            {
                specializationDMGBonus = CalculateSpecializationDMG(attacker, weapon);

                // Nothing equipped - check gloves.
                if (weapon == null)
                {
                    weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.Arms);
                }

                if (weapon != null)
                {
                    // Weapons may have multiple DMG properties, especially if enhanced.  The DMG types may not all be the
                    // same. Add all DMG properties of the same type together.
                    for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                    {

                        var ip = weapon.GetPassiveProperty(index);
                        if (ip != null && ip.m_nPropertyName == (ushort)ItemPropertyType.DMG)
                        {
                            // Catch old-style DMG properties here, and correct the damage type by hand.
                            var damageType = ip.m_nSubType;
                            if (damageType > 6 || damageType < 1) damageType = 1;

                            // Add the value of this property to the array.
                            var dmg = dmgValues[(CombatDamageType)damageType];
                            dmg += Combat.GetDMGValueFromItemPropertyCostTableValue(ip.m_nCostTableValue);
                            dmgValues[(CombatDamageType)damageType] = dmg;
                            foundDMG = true;
                        }
                    }
                }
            }

            if (!foundDMG)
            {
                // If no properties default to 0.5 physical.
                dmgValues[CombatDamageType.Physical] = 0.5f;
            }

            int attackAttribute = attackerStats.m_nStrengthModifier;

            if (attackType == (uint) AttackType.Ranged)
            {
                // Throwing weapons should add Strength.  Other ranged types are based on weapon base damage rating. 
                if (weapon != null && !Item.ThrowingWeaponBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                {
                    attackAttribute = weapon == null ? 0 : (int)(dmgValues[CombatDamageType.Physical] / 3);
                }
            }
            else if (attackType == (uint) AttackType.Spirit)
            {
                attackAttribute = attackerStats.m_nWisdomModifier;
            }

            // Attributes are stored as a byte (uint) - values over 128 are meant to be negative.
            if (attackAttribute > 128) attackAttribute -= 256;

            var log = "DAMAGE: attacker attribute modifier: " + attackAttribute.ToString() + ", weapon damage ratings ";
            foreach(var damageType in dmgValues.Keys)
            {
                log += damageType.ToString() + ": " + dmgValues[damageType] + ";";
            }
            Log.Write(LogGroup.Attack, log);

            // Add in the specialization damage bonus now.  We don't want to count this for the ranged weapon
            // attack attribute, so it can't happen earlier.
            dmgValues[CombatDamageType.Physical] += specializationDMGBonus;

            // Combat Mode - Power Attack (+1.0 DMG)
            if (attacker?.m_nCombatMode == 2) // 2 = Power Attack
            {
                dmgValues[CombatDamageType.Physical] += 1.0f;
            }
            // Combat Mode - Improved Power Attack (+2.5 DMG)
            else if (attacker?.m_nCombatMode == 3) // 3 = Improved Power Attack
            {
                dmgValues[CombatDamageType.Physical] += 2.5f;
            }

            // 2-handed weapons and Doublehand perk
            if (attackType == (uint)AttackType.Melee && weapon != null)
            {
                bool bTwoHander = Item.HeavyVibrobladeBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) ||
                                    Item.PolearmBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem);
                bool bDoubleHand = attacker.m_pInventory.GetItemInSlot((uint)InventorySlot.LeftHand) == null &&
                                      attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand) == 1;
                if (bTwoHander || bDoubleHand)
                {
                    Log.Write(LogGroup.Attack, "DAMAGE: Applying two-handed damage bonus.");
                    attackAttribute = (int)(attackAttribute * 1.5f);
                }
            }

            var critMultiplier = 1;
            if (bCritical == 1)
            {
                // Only do this calculation if it's actually a critical hit. 
                // Look up the weapon's base crit multiplier and apply any bonus from the Improved Multiplier perk. 
                critMultiplier = weapon != null ? Item.GetCriticalModifier((BaseItem)weapon.m_nBaseItem) : 1;
                if (HasImprovedMultiplier(attacker, weapon)) critMultiplier += 1;
            }

            int critical = bCritical == 1 ? critMultiplier : 0;
            var damage = 0;

            foreach (var damageType in dmgValues.Keys)
            {
                // Calculate total defense on the target.
                if (targetObject.m_nObjectType == (int)ObjectType.Creature)
                {
                    var target = CNWSCreature.FromPointer(pTarget);
                    float vitality = target.m_pStats.m_nConstitutionModifier;
                    var damagePower = attackerStats.m_pBaseCreature.CalculateDamagePower(target, bOffHand);

                    // Numbers over 128 are negative.
                    if (vitality > 128) vitality -= 256;

                    var defense = Stat.GetDefenseNative(target, damageType);

                    Log.Write(LogGroup.Attack, "DAMAGE: attacker damage attribute: " + dmgValues[damageType].ToString() + " defender defense attribute: " + defense.ToString() + ", defender racial type " + target.m_pStats.m_nRace);
                    damage = Combat.CalculateDamage(dmgValues[damageType], attackAttribute, defense, vitality, critical);

                    // Apply droid bonus for electrical damage.
                    if (target.m_pStats.m_nRace == (ushort)RacialType.Robot && damageType == CombatDamageType.Electrical)
                    {
                        damage *= 2;
                    }

                    // Apply NWN mechanics to damage reduction - physical only
                    if (damageType == CombatDamageType.Physical)
                    {
                        damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                        damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1);
                        damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1);
                    }
                }
                else if (targetObject.m_nObjectType == (int)ObjectType.Placeable)
                {
                    // Placeables and doors use their hardness attribute as their defense score and vitality. 
                    CNWSPlaceable plc = CNWSPlaceable.FromPointer(pTarget);
                    int hardness = plc.m_nHardness;
                    damage = Combat.CalculateDamage(dmgValues[damageType], attackAttribute, hardness, hardness, critical);
                }
                else if (targetObject.m_nObjectType == (int)ObjectType.Door)
                {
                    // Placeables and doors use their hardness attribute as their defense score and vitality. 
                    CNWSDoor door = CNWSDoor.FromPointer(pTarget);
                    int hardness = door.m_nHardness;
                    damage = Combat.CalculateDamage(dmgValues[damageType], attackAttribute, hardness, hardness, critical);
                }
                else
                {
                    // We're attacking something that is neither a creature not a placeable.  Just use the base dmg value.
                    damage = (int)dmgValues[damageType];
                }

                // Plot target - zero damage
                if (targetObject.m_bPlotObject == 1)
                {
                    damage = 0;
                }

                // Final sanity check.
                if (damage < 0)
                {
                    damage = 0;
                }

                if (damageType == CombatDamageType.Physical)
                {
                    physicalDamage = damage;
                }
                else if (damageType == CombatDamageType.Force && damage > 0)
                {
                    pAttackData.AddDamage((ushort)DamageType.Sonic, damage);
                }
                else if (damageType == CombatDamageType.Fire && damage > 0)
                {
                    pAttackData.AddDamage((ushort)DamageType.Fire, damage);
                }
                else if (damageType == CombatDamageType.Poison && damage > 0)
                {
                    pAttackData.AddDamage((ushort)DamageType.Acid, damage);
                }
                else if (damageType == CombatDamageType.Electrical && damage > 0)
                {
                    pAttackData.AddDamage((ushort)DamageType.Electrical, damage);
                }
                else if (damageType == CombatDamageType.Ice && damage > 0)
                {
                    pAttackData.AddDamage((ushort)DamageType.Cold, damage);
                }
            }

            return physicalDamage;
        }

        private static float CalculateSpecializationDMG(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_UnarmedStrike);
            }

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            // Unarmed strike (glove)
            if (baseItemType == BaseItem.Gloves &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_UnarmedStrike) == 1)
            {
                return 1;
            }

            // Creature weapons
            if (Item.CreatureBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_Creature) == 1)
            {
                return 1;
            }

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

        private static bool HasImprovedMultiplier(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null) return false;
            if (attacker.m_pStats.HasFeat((ushort)FeatType.IncreaseMultiplier) == 0) return false;

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            if (Item.StaffBaseItemTypes.Contains(baseItemType)) return true;
            if (Item.PolearmBaseItemTypes.Contains(baseItemType)) return true;
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType)) return true;

            return false;
        }
    }
}
