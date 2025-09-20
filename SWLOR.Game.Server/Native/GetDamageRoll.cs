using NWN.Native.API;
using NWNX.NET;

using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Core.Server;
using Ability = SWLOR.Game.Server.Service.Ability;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using DamageType = NWN.Native.API.DamageType;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using ObjectType = NWN.Native.API.ObjectType;
using RacialType = SWLOR.NWN.API.NWScript.Enum.RacialType;

namespace SWLOR.Game.Server.Native
{
    public static unsafe class GetDamageRoll
    {
        private static ILogger _logger = ServiceContainer.GetService<ILogger>();
        private const int PowerAttackDamageBonus = 3;
        private const int ImprovedPowerAttackDamageBonus = 6;
        private const int DefaultPhysicalDamage = 1;
        private const int ElectricalDroidMultiplier = 2;
        private const int CrushingMasteryMultiplier = 2;
        private const int WeaponSpecializationUnarmedDamage = 2;
        private const int WeaponSpecializationCreatureDamage = 2;
        private const int WeaponSpecializationOtherDamage = 1;
        private const int PowerAttackMode = 2;
        private const int ImprovedPowerAttackMode = 3;
        private const int AttributeNegativeThreshold = 128;
        private const int AttributeNegativeOffset = 256;
        private const int MaxValidDamageType = 6;
        private const int MinValidDamageType = 1;

        private static readonly Dictionary<BaseItem, (FeatType Feat, int Damage)> _weaponSpecializationLookup = CreateWeaponSpecializationLookup();

        private static Dictionary<BaseItem, (FeatType Feat, int Damage)> CreateWeaponSpecializationLookup()
        {
            var lookup = new Dictionary<BaseItem, (FeatType, int)>();

            // Helper to add all items from a collection
            void AddItems(IEnumerable<BaseItem> items, FeatType feat, int damage)
            {
                foreach (var item in items)
                {
                    lookup[item] = (feat, damage);
                }
            }

            // Gloves (unarmed)
            lookup[BaseItem.Gloves] = (FeatType.WeaponSpecialization_UnarmedStrike, WeaponSpecializationUnarmedDamage);

            // All other weapon types
            AddItems(Item.CreatureBaseItemTypes, FeatType.WeaponSpecialization_Creature, WeaponSpecializationCreatureDamage);
            AddItems(Item.VibrobladeBaseItemTypes, FeatType.WeaponSpecializationVibroblades, WeaponSpecializationOtherDamage);
            AddItems(Item.FinesseVibrobladeBaseItemTypes, FeatType.WeaponSpecializationFinesseVibroblades, WeaponSpecializationOtherDamage);
            AddItems(Item.LightsaberBaseItemTypes, FeatType.WeaponSpecializationLightsabers, WeaponSpecializationOtherDamage);
            AddItems(Item.HeavyVibrobladeBaseItemTypes, FeatType.WeaponSpecializationHeavyVibroblades, WeaponSpecializationOtherDamage);
            AddItems(Item.PolearmBaseItemTypes, FeatType.WeaponSpecializationPolearms, WeaponSpecializationOtherDamage);
            AddItems(Item.TwinBladeBaseItemTypes, FeatType.WeaponSpecializationTwinBlades, WeaponSpecializationOtherDamage);
            AddItems(Item.SaberstaffBaseItemTypes, FeatType.WeaponSpecializationSaberstaffs, WeaponSpecializationOtherDamage);
            AddItems(Item.KatarBaseItemTypes, FeatType.WeaponSpecializationKatars, WeaponSpecializationOtherDamage);
            AddItems(Item.StaffBaseItemTypes, FeatType.WeaponSpecialization_Staff, WeaponSpecializationOtherDamage);
            AddItems(Item.PistolBaseItemTypes, FeatType.WeaponSpecializationPistol, WeaponSpecializationOtherDamage);
            AddItems(Item.ThrowingWeaponBaseItemTypes, FeatType.WeaponSpecializationThrowingWeapons, WeaponSpecializationOtherDamage);
            AddItems(Item.RifleBaseItemTypes, FeatType.WeaponSpecializationRifles, WeaponSpecializationOtherDamage);

            return lookup;
        }
        internal delegate int GetDamageRollHook(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax);
        // ReSharper disable once NotAccessedField.Local
        private static GetDamageRollHook _callOriginal;

        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void RegisterHook()
        {
            delegate* unmanaged<void*, void*, int, int, int, int, int, int> pHook = &OnGetDamageRoll;
            var functionPtr = NativeLibrary.GetExport(
                NativeLibrary.GetMainProgramHandle(), "_ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii");
            var hookPtr = NWNXAPI.RequestFunctionHook(
                functionPtr,
                (IntPtr)pHook,
                -1000000);

            _callOriginal = Marshal.GetDelegateForFunctionPointer<GetDamageRollHook>((IntPtr)hookPtr);
        }

        [UnmanagedCallersOnly]
        private static int OnGetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        {
            return ServerManager.Executor.ExecuteInScriptContext(() =>
            {
                var attackerStats = CNWSCreatureStats.FromPointer(thisPtr);
                var attacker = CNWSCreature.FromPointer(attackerStats.m_pBaseCreature);

                var area = attacker.GetArea();
                ProfilerPlugin.PushPerfScope("RunScript",
                    "Script", $"NATIVE:{nameof(OnGetDamageRoll)}",
                    "Area", area == null ? "Unknown" : area.m_sTag.ToString(),
                    "ObjectType", "Creature");

                var targetObject = CNWSObject.FromPointer(pTarget);

                // Early exit for invalid targets
                if (targetObject == null || targetObject.m_idSelf == OBJECT_INVALID)
                {
                    ProfilerPlugin.PopPerfScope();
                    return 0;
                }

                var damageFlags = attackerStats.m_pBaseCreature.GetDamageFlags();
                var pCombatRound = attacker.m_pcCombatRound;
                var pAttackData = pCombatRound.GetAttack(pCombatRound.m_nCurrentAttack);
                var weapon = pCombatRound.GetCurrentAttackWeapon();

                var attackType = attacker.GetRangeWeaponEquipped() == 1 ? (uint)AttackType.Ranged : (uint)AttackType.Melee;

                LogAttackInfo(attacker, targetObject, attackType, weapon);

                // Nothing equipped - check gloves
                if (weapon == null)
                {
                    weapon = attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.Arms);
                }

                // Extract weapon damage properties and get ability stats
                var dmgValues = ExtractWeaponDamageProperties(weapon);
                var attackerStatType = GetWeaponDamageAbilityType(weapon);
                var weaponPerkLevel = GetWeaponPerkLevel(weapon);

                var attackerStat = Stat.GetStatValueNative(attacker, attackerStatType);

                // Apply weapon style stat override
                var damageStat = GetWeaponStyleStat(weapon, attacker);
                if (damageStat > -1)
                {
                    attackerStat = damageStat;
                }

                // Handle negative attributes
                if (attackerStat > AttributeNegativeThreshold)
                    attackerStat -= AttributeNegativeOffset;

                LogDamageCalculation(attackerStat, dmgValues);

                // Apply specialization bonus
                dmgValues[CombatDamageType.Physical] += CalculateSpecializationDMG(attacker, weapon);

                // Apply combat mode and style bonuses
                ApplyCombatModeBonus(attacker, dmgValues);
                ApplySpecialStyleBonuses(attacker, weapon, dmgValues);

                // Calculate critical multiplier
                var critical = CalculateCriticalMultiplier(attacker, weapon, bCritical);
                var attackerAttack = weapon == null ? 0 : Stat.GetAttackNative(attacker, (BaseItem)weapon.m_nBaseItem);

                var physicalDamage = ProcessDamageTypes(pTarget, attacker, weapon, dmgValues, pAttackData,
                    attackerAttack, attackerStat, critical, weaponPerkLevel, attackType, damageFlags, bOffHand, targetObject);

                ProfilerPlugin.PopPerfScope();
                return physicalDamage;
            });
        }

        private static void LogAttackInfo(CNWSCreature attacker, CNWSObject targetObject, uint attackType, CNWSItem weapon)
        {
            _logger.Write<AttackLogGroup>($"DAMAGE: Attacker: {attacker.GetFirstName().GetSimple()}, PC?: {attacker.m_bPlayerCharacter}, " +
                                      $"Defender {targetObject.GetFirstName().GetSimple()}, object type {targetObject.m_nObjectType}, " +
                                      $"Attack type: {attackType}, weapon {(weapon == null ? "None" : weapon.GetFirstName().GetSimple())}");
        }

        private static void LogDamageCalculation(int attackerStat, Dictionary<CombatDamageType, int> dmgValues)
        {
            var log = $"DAMAGE: attacker attribute modifier: {attackerStat}, weapon damage ratings ";
            foreach (var damageType in dmgValues.Keys)
            {
                log += $"{damageType}: {dmgValues[damageType]};";
            }
            _logger.Write<AttackLogGroup>(log);
        }

        private static int CalculateCriticalMultiplier(CNWSCreature attacker, CNWSItem weapon, int bCritical)
        {
            if (bCritical != 1) return 0;

            var critMultiplier = weapon != null ? Item.GetCriticalModifier((BaseItem)weapon.m_nBaseItem) : 1;
            if (HasImprovedMultiplier(attacker, weapon)) critMultiplier += 1;
            if (HasRapidReload(attacker, weapon)) critMultiplier += 1;

            return critMultiplier;
        }

        private static int ProcessDamageTypes(void* pTarget, CNWSCreature attacker, CNWSItem weapon,
            Dictionary<CombatDamageType, int> dmgValues, void* pAttackData, int attackerAttack,
            int attackerStat, int critical, int weaponPerkLevel, uint attackType, uint damageFlags,
            int bOffHand, CNWSObject targetObject)
        {
            var physicalDamage = 0;

            foreach (var damageType in dmgValues.Keys)
            {
                var damage = CalculateTargetSpecificDamage(pTarget, attacker, weapon, dmgValues, damageType,
                    attackerAttack, attackerStat, critical, weaponPerkLevel, attackType, damageFlags, bOffHand);

                // Plot target takes no damage
                if (targetObject.m_bPlotObject == 1)
                    damage = 0;

                // Ensure damage is never negative
                if (damage < 0)
                    damage = 0;

                if (damageType == CombatDamageType.Physical)
                {
                    physicalDamage = damage;
                }
                else
                {
                    AddDamageToAttackData(pAttackData, damageType, damage);
                }
            }

            return physicalDamage;
        }

        private static int CalculateSpecializationDMG(CNWSCreature attacker, CNWSItem weapon)
        {
            if (weapon == null)
            {
                return attacker.m_pStats.HasFeat((ushort)FeatType.WeaponSpecialization_UnarmedStrike);
            }

            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            if (_weaponSpecializationLookup.TryGetValue(baseItemType, out var weaponSpec) &&
                attacker.m_pStats.HasFeat((ushort)weaponSpec.Feat) == 1)
            {
                return weaponSpec.Damage;
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

        private static void AddDamageToAttackData(void* pAttackData, CombatDamageType damageType, int damage)
        {
            if (damage <= 0) return;

            var attackData = CNWSCombatAttackData.FromPointer(pAttackData);
            switch (damageType)
            {
                case CombatDamageType.Force:
                    attackData.AddDamage((ushort)DamageType.Magical, damage);
                    break;
                case CombatDamageType.Fire:
                    attackData.AddDamage((ushort)DamageType.Fire, damage);
                    break;
                case CombatDamageType.Poison:
                    attackData.AddDamage((ushort)DamageType.Acid, damage);
                    break;
                case CombatDamageType.Electrical:
                    attackData.AddDamage((ushort)DamageType.Electrical, damage);
                    break;
                case CombatDamageType.Ice:
                    attackData.AddDamage((ushort)DamageType.Cold, damage);
                    break;
            }
        }

        private static Dictionary<CombatDamageType, int> ExtractWeaponDamageProperties(CNWSItem weapon)
        {
            var dmgValues = new Dictionary<CombatDamageType, int> { [CombatDamageType.Physical] = 0 };
            var foundDMG = false;

            if (weapon == null) return dmgValues;

            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName != (ushort)ItemPropertyType.DMG) continue;

                var damageTypeId = ip.m_nSubType;
                if (damageTypeId > MaxValidDamageType || damageTypeId < MinValidDamageType)
                    damageTypeId = MinValidDamageType;

                var damageType = (CombatDamageType)damageTypeId;
                if (!dmgValues.ContainsKey(damageType))
                    dmgValues[damageType] = 0;

                dmgValues[damageType] += ip.m_nCostTableValue;
                foundDMG = true;
            }

            if (!foundDMG)
            {
                dmgValues[CombatDamageType.Physical] = DefaultPhysicalDamage;
            }

            return dmgValues;
        }

        private static AbilityType GetWeaponDamageAbilityType(CNWSItem weapon)
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

            return Item.GetWeaponDamageAbilityType((BaseItem)weapon.m_nBaseItem);
        }

        private static int GetWeaponPerkLevel(CNWSItem weapon)
        {
            if (weapon == null) return 0;

            for (var index = 0; index < weapon.m_lstPassiveProperties.Count; index++)
            {
                var ip = weapon.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.UseLimitationPerk)
                {
                    return ip.m_nCostTableValue;
                }
            }

            return 0;
        }

        private static void ApplyCombatModeBonus(CNWSCreature attacker, Dictionary<CombatDamageType, int> dmgValues)
        {
            switch (attacker?.m_nCombatMode)
            {
                case PowerAttackMode:
                    dmgValues[CombatDamageType.Physical] += PowerAttackDamageBonus;
                    break;
                case ImprovedPowerAttackMode:
                    dmgValues[CombatDamageType.Physical] += ImprovedPowerAttackDamageBonus;
                    break;
            }
        }

        private static void ApplySpecialStyleBonuses(CNWSCreature attacker, CNWSItem weapon, Dictionary<CombatDamageType, int> dmgValues)
        {
            if (weapon == null) return;

            var playerId = attacker.m_pUUID.GetOrAssignRandom().ToString();
            var mightMod = attacker.m_pStats.m_nStrengthModifier;
            var baseItemType = (BaseItem)weapon.m_nBaseItem;

            // Doublehand bonus
            if (attacker.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand) == null)
            {
                if (Item.OneHandedMeleeItemTypes.Contains(baseItemType) ||
                    Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
                {
                    var doublehandDMGBonus = Combat.GetDoublehandDMGBonusNative(attacker);
                    _logger.Write<AttackLogGroup>($"DAMAGE: Applying doublehand damage bonus. (+{doublehandDMGBonus})");
                    dmgValues[CombatDamageType.Physical] += doublehandDMGBonus;
                }
            }

            // Staff bonuses
            if (Item.StaffBaseItemTypes.Contains(baseItemType))
            {
                if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingMastery) == 1)
                    dmgValues[CombatDamageType.Physical] += mightMod * CrushingMasteryMultiplier;
                else if (attacker.m_pStats.HasFeat((ushort)FeatType.CrushingStyle) == 1)
                    dmgValues[CombatDamageType.Physical] += mightMod;
            }
            // Strong Style bonuses
            else if (Item.SaberstaffBaseItemTypes.Contains(baseItemType) &&
                Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleSaberstaff))
            {
                dmgValues[CombatDamageType.Physical] += (int)Math.Ceiling(mightMod / 2.0f);
            }
            else if (Item.LightsaberBaseItemTypes.Contains(baseItemType) &&
                Ability.IsAbilityToggled(playerId, AbilityToggleType.StrongStyleLightsaber))
            {
                dmgValues[CombatDamageType.Physical] += (int)Math.Ceiling(mightMod / 2.0f);
            }
        }

        private static int CalculateTargetSpecificDamage(void* pTarget, CNWSCreature attacker, CNWSItem weapon,
            Dictionary<CombatDamageType, int> dmgValues, CombatDamageType damageType, int attackerAttack,
            int attackerStat, int critical, int weaponPerkLevel, uint attackType, uint damageFlags, int bOffHand)
        {
            var targetObject = CNWSObject.FromPointer(pTarget);

            switch (targetObject.m_nObjectType)
            {
                case (int)ObjectType.Creature:
                    return CalculateCreatureDamage(pTarget, attacker, dmgValues, damageType, attackerAttack,
                        attackerStat, critical, weaponPerkLevel, attackType, damageFlags, bOffHand);

                case (int)ObjectType.Placeable:
                    var plc = CNWSPlaceable.FromPointer(pTarget);
                    return Combat.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                        plc.m_nHardness, plc.m_nHardness, critical);

                case (int)ObjectType.Door:
                    var door = CNWSDoor.FromPointer(pTarget);
                    return Combat.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                        door.m_nHardness, door.m_nHardness, critical);

                default:
                    return dmgValues[damageType];
            }
        }

        private static int CalculateCreatureDamage(void* pTarget, CNWSCreature attacker, Dictionary<CombatDamageType, int> dmgValues,
            CombatDamageType damageType, int attackerAttack, int attackerStat, int critical, int weaponPerkLevel,
            uint attackType, uint damageFlags, int bOffHand)
        {
            var target = CNWSCreature.FromPointer(pTarget);
            var defenderStat = target.m_pStats.GetCONStat();
            var damagePower = attacker.CalculateDamagePower(target, bOffHand);
            var defense = Stat.GetDefenseNative(target, damageType, AbilityType.Vitality);

            _logger.Write<AttackLogGroup>($"DAMAGE: attacker damage attribute: {dmgValues[damageType]} defender defense attribute: {defense}, defender racial type {target.m_pStats.m_nRace}");

            var damage = Combat.CalculateDamage(attackerAttack, dmgValues[damageType], attackerStat,
                defense, defenderStat, critical, weaponPerkLevel);

            // Apply droid electrical damage bonus
            if (target.m_pStats.m_nRace == (ushort)RacialType.Robot && damageType == CombatDamageType.Electrical)
            {
                damage *= ElectricalDroidMultiplier;
            }

            // Apply NWN damage mechanics for physical damage only
            if (damageType == CombatDamageType.Physical)
            {
                var bRangedAttack = attackType == (uint)AttackType.Ranged ? 1 : 0;
                damage = target.DoDamageImmunity(attacker, damage, damageFlags, 0, 1);
                damage = target.DoDamageResistance(attacker, damage, damageFlags, 0, 1, 1, bRangedAttack);
                damage = target.DoDamageReduction(attacker, damage, damagePower, 0, 1, bRangedAttack);
            }

            return damage;
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
            else if (Item.PistolBaseItemTypes.Contains(baseItemType) ||
                     Item.RifleBaseItemTypes.Contains(baseItemType) ||
                     Item.ThrowingWeaponBaseItemTypes.Contains(baseItemType))
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
