using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using Ability = SWLOR.Game.Server.Service.Ability;
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
            ProfilerPlugin.PushPerfScope($"NATIVE:{nameof(OnGetDamageRoll)}", "RunScript", "Script");

            var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
            var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);
            var targetObject = CNWSObject.FromPointer(pTarget);
            var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
            var pCombatRound = attacker.m_pcCombatRound;
            var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
            var weapon = pCombatRound.GetCurrentAttackWeapon();
            
            // On a critical hit, this method appears to be invoked multiple times, with an invalid target the second and
            // subsequent times.  Bail out early.
            if (targetObject == null || targetObject.m_idSelf == OBJECT_INVALID)
            {
                ProfilerPlugin.PopPerfScope();
                return 0;
            }

            var attackType = (uint) AttackType.Melee;
            
            // Figure out what sort of attack this is.  Note - damage from abilities does not go through this function.
            if (attacker.GetRangeWeaponEquipped() == 1)
            {
                attackType = (uint) AttackType.Ranged;
            }

            // We have now resolved what sort of attack we are doing.
            Log.Write(LogGroup.Attack, "DAMAGE: Attacker: " + attacker.GetFirstName().GetSimple() + ", PC?: " + attacker.m_bPlayerCharacter +
                ", Defender " + targetObject.GetFirstName().GetSimple() + ", object type " + targetObject.m_nObjectType + ", Attack type: " + attackType + ", weapon " + 
                                        (weapon == null ? "None" : weapon.GetFirstName().GetSimple()));

            // Initialise damage array to read properties from equipped weapon.
            var dmgValues = new Dictionary<CombatDamageType, int>();
            dmgValues[CombatDamageType.Physical] = 0;
            var physicalDamage = 0;
            var foundDMG = false;
            var weaponPerkLevel = 0;

            // Calculate attacker's base DMG
            var specializationDMGBonus = CalculateSpecializationDMG(attacker, weapon);

            // Nothing equipped - check gloves.
            if (weapon == null)
            {
                weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.Arms);
            }

            var attackerStatType = weapon == null
                ? AbilityType.Might
                : Item.GetWeaponDamageAbilityType((BaseItem)weapon.m_nBaseItem);

            if (weapon != null)
            {
                for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
                {
                    var ip = weapon.GetPassiveProperty(index);
                    if (ip == null) continue;

                    // Weapons may have multiple DMG properties, especially if enhanced.  The DMG types may not all be the
                    // same. Add all DMG properties of the same type together.
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.DMG)
                    {
                        // Catch old-style DMG properties here, and correct the damage type by hand.
                        var damageTypeId = ip.m_nSubType;
                        if (damageTypeId > 6 || damageTypeId < 1) damageTypeId = 1;

                        // Add the value of this property to the array.
                        var damageType = (CombatDamageType)damageTypeId;
                        if (!dmgValues.ContainsKey(damageType))
                            dmgValues[damageType] = 0;

                        var dmg = dmgValues[damageType];
                        dmg += ip.m_nCostTableValue;
                        dmgValues[damageType] = dmg;
                        foundDMG = true;
                    }
                    // Some weapons, such as creature claws, may have a stat override assigned.
                    else if (ip.m_nPropertyName == (ushort)ItemPropertyType.DamageStat)
                    {
                        var abilityType = (AbilityType)ip.m_nSubType;
                        attackerStatType = abilityType;
                    }

                    if (weaponPerkLevel == 0 && ip.m_nPropertyName == (ushort)ItemPropertyType.UseLimitationPerk)
                    {
                        weaponPerkLevel = ip.m_nCostTableValue;
                    }
                }
            }
            

            if (!foundDMG)
            {
                // If no properties default to 1 physical.
                dmgValues[CombatDamageType.Physical] = 1;
            }

            var attackerStat = Stat.GetStatValueNative(attacker, attackerStatType);

            // Weapon Style Toggle
            // Swaps damage stat as appropriate.
            var damageStat = GetWeaponStyleStat(weapon, attacker);
            if (damageStat > -1)
            {
                attackerStat = damageStat;
            }

            // Attributes are stored as a byte (uint) - values over 128 are meant to be negative.
            if (attackerStat > 128) attackerStat -= 256;

            var log = "DAMAGE: attacker attribute modifier: " + attackerStat + ", weapon damage ratings ";
            foreach(var damageType in dmgValues.Keys)
            {
                log += damageType + ": " + dmgValues[damageType] + ";";
            }
            Log.Write(LogGroup.Attack, log);

            // Add in the specialization damage bonus now.  We don't want to count this for the ranged weapon
            // attack attribute, so it can't happen earlier.
            dmgValues[CombatDamageType.Physical] += specializationDMGBonus;

            // Combat Mode - Power Attack (+3 DMG)
            if (attacker?.m_nCombatMode == 2) // 2 = Power Attack
            {
                dmgValues[CombatDamageType.Physical] += 3;
            }
            // Combat Mode - Improved Power Attack (+6 DMG)
            else if (attacker?.m_nCombatMode == 3) // 3 = Improved Power Attack
            {
                dmgValues[CombatDamageType.Physical] += 6;
            }

            var mightMod = attacker.m_pStats.m_nStrengthModifier;
            var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();

            // Doublehand perk, MGT mod bonus to Crushing Staves + Strong Style
            if (weapon != null)
            {
                if (attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand) == null)
                {
                    var weaponType = (BaseItem)weapon.m_nBaseItem;

                    if (Item.OneHandedMeleeItemTypes.Contains(weaponType) ||
                        Item.ThrowingWeaponBaseItemTypes.Contains(weaponType))
                    {
                        var doublehandDMGBonus = Combat.GetDoublehandDMGBonusNative(attacker);
                        Log.Write(LogGroup.Attack, $"DAMAGE: Applying doublehand damage bonus. (+{doublehandDMGBonus})");
                        dmgValues[CombatDamageType.Physical] += doublehandDMGBonus;
                    }
                }

                if (Item.StaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem))
                {
                    if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingMastery) == 1)
                        dmgValues[CombatDamageType.Physical] += mightMod * 2; // Mastery gives 2x MGT
                    else if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingStyle) == 1)
                        dmgValues[CombatDamageType.Physical] += mightMod; // Crushing Staves 1x MGT
                }
                else if (Item.SaberstaffBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) &&
                    Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
                    dmgValues[CombatDamageType.Physical] += (int)Math.Ceiling(mightMod / 2.0f);
                else if (Item.LightsaberBaseItemTypes.Contains((BaseItem)weapon.m_nBaseItem) &&
                    Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
                    dmgValues[CombatDamageType.Physical] += (int)Math.Ceiling(mightMod / 2.0f);
            }

            var critMultiplier = 1;
            if (bCritical == 1)
            {
                // Only do this calculation if it's actually a critical hit. 
                // Look up the weapon's base crit multiplier and apply any bonus from the Improved Multiplier perk. 
                critMultiplier = weapon != null ? Item.GetCriticalModifier((BaseItem)weapon.m_nBaseItem) : 1;
                if (HasImprovedMultiplier(attacker, weapon)) critMultiplier += 1;
                if (HasRapidReload(attacker, weapon)) critMultiplier += 1;
            }

            var critical = bCritical == 1 ? critMultiplier : 0;
            var attackerAttack = weapon == null ? 0 : Stat.GetAttackNative(attacker, (BaseItem)weapon.m_nBaseItem);

            foreach (var damageType in dmgValues.Keys)
            {
                // Calculate total defense on the target.
                int damage;
                if (targetObject.m_nObjectType == (int)ObjectType.Creature)
                {
                    var target = CNWSCreature.FromPointer(pTarget);
                    int defenderStat = target.m_pStats.GetCONStat();
                    var damagePower = attackerStats.m_pBaseCreature.CalculateDamagePower(target, bOffHand);
                    var defense = Stat.GetDefenseNative(target, damageType, AbilityType.Vitality);

                    Log.Write(LogGroup.Attack, "DAMAGE: attacker damage attribute: " + dmgValues[damageType].ToString() + " defender defense attribute: " + defense.ToString() + ", defender racial type " + target.m_pStats.m_nRace);
                    damage = Combat.CalculateDamage(
                        attackerAttack,
                        dmgValues[damageType], 
                        attackerStat, 
                        defense,
                        defenderStat, 
                        critical,
                        weaponPerkLevel);

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
                    var plc = CNWSPlaceable.FromPointer(pTarget);
                    int hardness = plc.m_nHardness;
                    damage = Combat.CalculateDamage(
                        attackerAttack,
                        dmgValues[damageType], 
                        attackerStat, 
                        hardness,
                        hardness, 
                        critical);
                }
                else if (targetObject.m_nObjectType == (int)ObjectType.Door)
                {
                    // Placeables and doors use their hardness attribute as their defense score and vitality. 
                    var door = CNWSDoor.FromPointer(pTarget);
                    int hardness = door.m_nHardness;
                    damage = Combat.CalculateDamage(
                        attackerAttack,
                        dmgValues[damageType], 
                        attackerStat, 
                        hardness, 
                        hardness, 
                        critical);
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
                    pAttackData.AddDamage((ushort)DamageType.Magical, damage);
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

            ProfilerPlugin.PopPerfScope();

            return physicalDamage;
        }

        private static int CalculateSpecializationDMG(CNWSCreature attacker, CNWSItem weapon)
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
                return 2;
            }

            // Creature weapons
            if (Item.CreatureBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_Creature) == 1)
            {
                return 2;
            }

            // Vibroblades
            if (Item.VibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationVibroblades) == 1)
            {
                return 1;
            }

            // Finesse Vibroblades
            if (Item.FinesseVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationFinesseVibroblades) == 1)
            {
                return 1;
            }

            // Lightsabers
            if (Item.LightsaberBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationLightsabers) == 1)
            {
                return 1;
            }

            // Heavy Vibroblades
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationHeavyVibroblades) == 1)
            {
                return 1;
            }

            // Polearms
            if (Item.PolearmBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationPolearms) == 1)
            {
                return 1;
            }

            // Twin Blades
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationTwinBlades) == 1)
            {
                return 1;
            }

            // Saberstaffs
            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationSaberstaffs) == 1)
            {
                return 1;
            }

            // Katars
            if (Item.KatarBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationKatars) == 1)
            {
                return 1;
            }

            // Staves
            if (Item.StaffBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_Staff) == 1)
            {
                return 1;
            }

            // Pistols
            if (Item.PistolBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationPistol) == 1)
            {
                return 1;
            }

            // Throwing Weapons
            if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationThrowingWeapons) == 1)
            {
                return 1;
            }

            // Rifles
            if (Item.RifleBaseItemTypes.Contains(baseItemType) &&
                attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecializationRifles) == 1)
            {
                return 1;
            }

            return 0;
        }

        private static bool HasImprovedMultiplier(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null) return false;
            if (attacker.m_pStats.HasFeat((ushort)FeatType.IncreaseMultiplier) == 0) return false;

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            if (Item.SaberstaffBaseItemTypes.Contains(baseItemType)) return true;
            if (Item.TwinBladeBaseItemTypes.Contains(baseItemType)) return true;
            if (Item.PolearmBaseItemTypes.Contains(baseItemType)) return true;
            if (Item.HeavyVibrobladeBaseItemTypes.Contains(baseItemType)) return true;

            return false;
        }

        private static bool HasRapidReload(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null) return false;
            if (attacker.m_pStats.HasFeat((ushort)FeatType.RapidReload) == 0) return false;

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            if (Item.RifleBaseItemTypes.Contains(baseItemType)) return true;

            return false;
        }

        private static int GetWeaponStyleStat(CNWSItem weapon, CNWSCreature attacker)
        {
            if (weapon == null)
                return -1;

            var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();

            var baseItemType = (BaseItem)weapon.m_nBaseItem;
            var wil = Stat.GetStatValueNative(attacker, AbilityType.Willpower);
            var weaponDamageAbilityType = Item.GetWeaponDamageAbilityType(baseItemType);
            var weaponDamageAbilityStat = Stat.GetStatValueNative(attacker, weaponDamageAbilityType);

            if (Item.LightsaberBaseItemTypes.Contains(baseItemType))
            {
                if (Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
                    return attacker.m_pStats.GetSTRStat();
            }
            else if (Item.SaberstaffBaseItemTypes.Contains(baseItemType))
            {
                if (Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
                    return attacker.m_pStats.GetSTRStat();
            }
            else if (Item.PistolBaseItemTypes.Contains(baseItemType))
            {
                if (wil > weaponDamageAbilityStat && attacker.m_pStats.HasFeat((ushort)FeatType.ZenMarksmanship) == 1)
                    return attacker.m_pStats.GetWISStat();
            }
            else if (Item.RifleBaseItemTypes.Contains(baseItemType))
            {
                if (wil > weaponDamageAbilityStat && attacker.m_pStats.HasFeat((ushort)FeatType.ZenMarksmanship) == 1)
                    return attacker.m_pStats.GetWISStat();
            }
            else if (Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
            {
                if (wil > weaponDamageAbilityStat && attacker.m_pStats.HasFeat((ushort)FeatType.ZenMarksmanship) == 1)
                    return attacker.m_pStats.GetWISStat();
            }
            else if (Item.StaffBaseItemTypes.Contains(baseItemType))
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.FlurryStyle) == 1)
                    return attacker.m_pStats.GetDEXStat();
            }

            return -1;
        }
    }
}
