using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The item properties an enhancement builds, and how a crafted property merges into an item or a list of properties. Moved out of
    /// <see cref="CraftService"/> unchanged (the one long switch is split by property family).
    /// </summary>
    public class CraftEnhancementPropertyService : ICraftEnhancementPropertyService
    {
        private readonly IResistanceService _resistance;

        public CraftEnhancementPropertyService(IResistanceService resistance)
        {
            _resistance = resistance;
        }

        /// <summary>
        /// Builds an item property for a given enhancement type.
        /// </summary>
        /// <param name="subTypeId">The sub type of the enhancement</param>
        /// <param name="amount">The amount to apply.</param>
        /// <returns></returns>
        public IEnumerable<ItemProperty> BuildItemPropertiesForEnhancement(
            EnhancementSubType subTypeId,
            int amount,
            CombatDamageType damageType = CombatDamageType.Invalid)
        {
            yield return BuildItemPropertyForEnhancement(subTypeId, amount);

            if (TryGetWeaponDamageTypeForEnhancement(subTypeId, damageType, out var resolvedDamageType) &&
                !resolvedDamageType.IsPhysicalDamageType())
            {
                yield return ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)resolvedDamageType);
            }
        }

        public ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount)
        {
            if (TryBuildGearProperty(subTypeId, amount, out var property) ||
                TryBuildFoodAndCraftingProperty(subTypeId, amount, out property) ||
                TryBuildAttackAndDroidProperty(subTypeId, amount, out property))
                return property;

            throw new Exception("Unsupported enhancement type.");
        }

        /// <summary>The defense, resistance, hit point, stamina, ability and damage enhancements.</summary>
        private bool TryBuildGearProperty(EnhancementSubType subTypeId, int amount, out ItemProperty property)
        {
            switch (subTypeId)
            {
                case EnhancementSubType.DefensePhysical: // Defense - Physical
                    property = ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, amount); return true;
                case EnhancementSubType.DefenseForce: // Defense - Force
                    property = ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, amount); return true;
                case EnhancementSubType.ResistanceFire: // Resistance - Fire
                    property = BuildResistanceItemProperty(ResistanceType.Fire, amount); return true;
                case EnhancementSubType.ResistancePoison: // Resistance - Poison
                    property = BuildResistanceItemProperty(ResistanceType.Poison, amount); return true;
                case EnhancementSubType.ResistanceElectrical: // Resistance - Electrical
                    property = BuildResistanceItemProperty(ResistanceType.Electrical, amount); return true;
                case EnhancementSubType.ResistanceIce: // Resistance - Ice
                    property = BuildResistanceItemProperty(ResistanceType.Ice, amount); return true;
                case EnhancementSubType.ResistanceMind: // Resistance - Mind
                    property = BuildResistanceItemProperty(ResistanceType.Mind, amount); return true;
                case EnhancementSubType.ResistanceMobility: // Resistance - Mobility
                    property = BuildResistanceItemProperty(ResistanceType.Mobility, amount); return true;
                case EnhancementSubType.ResistanceTrauma: // Resistance - Trauma
                    property = BuildResistanceItemProperty(ResistanceType.Trauma, amount); return true;
                case EnhancementSubType.ResistanceDisruption: // Resistance - Disruption
                    property = BuildResistanceItemProperty(ResistanceType.Disruption, amount); return true;
                case EnhancementSubType.Evasion: // Evasion
                    property = ItemPropertyCustom(ItemPropertyType.Evasion, -1, amount); return true;
                case EnhancementSubType.HP: // HP
                    property = ItemPropertyCustom(ItemPropertyType.HPBonus, -1, amount); return true;
                case EnhancementSubType.FP: // FP
                    property = ItemPropertyCustom(ItemPropertyType.FP, -1, amount); return true;
                case EnhancementSubType.Stamina: // Stamina
                    property = ItemPropertyCustom(ItemPropertyType.Stamina, -1, amount); return true;
                case EnhancementSubType.Vitality: // Vitality
                    property = ItemPropertyAbilityBonus(AbilityType.Vitality, amount); return true;
                case EnhancementSubType.Social: // Social
                    property = ItemPropertyAbilityBonus(AbilityType.Social, amount); return true;
                case EnhancementSubType.Willpower: // Willpower
                    property = ItemPropertyAbilityBonus(AbilityType.Willpower, amount); return true;
                case EnhancementSubType.ControlSmithery: // Control - Smithery
                    property = ItemPropertyCustom(ItemPropertyType.Control, 1, amount); return true;
                case EnhancementSubType.CraftsmanshipSmithery: // Craftsmanship - Smithery
                    property = ItemPropertyCustom(ItemPropertyType.Craftsmanship, 1, amount); return true;
                case EnhancementSubType.ShieldDeflection: // Shield Deflection
                    property = ItemPropertyCustom(ItemPropertyType.ShieldDeflection, -1, amount); return true;

                // 16 and 17 are applied within the view model, as they are not actually item properties.

                case EnhancementSubType.DMG: // DMG
                    property = ItemPropertyCustom(ItemPropertyType.DMG, -1, amount); return true;
                case EnhancementSubType.Might: // Might
                    property = ItemPropertyAbilityBonus(AbilityType.Might, amount); return true;
                case EnhancementSubType.Perception: // Perception
                    property = ItemPropertyAbilityBonus(AbilityType.Perception, amount); return true;
                case EnhancementSubType.Accuracy: // Accuracy
                    property = ItemPropertyCustom(ItemPropertyType.Accuracy, -1, amount); return true;
                case EnhancementSubType.CombatReadiness: // Combat Readiness
                    property = ItemPropertyCustom(ItemPropertyType.CombatReadiness, -1, amount); return true;
                case EnhancementSubType.StructureBonus: // Structure Bonus
                    property = ItemPropertyCustom(ItemPropertyType.StructureBonus, -1, amount); return true;
            }

            property = null;
            return false;
        }

        /// <summary>The food bonus, crafting control and craftsmanship, module and starship enhancements.</summary>
        private bool TryBuildFoodAndCraftingProperty(EnhancementSubType subTypeId, int amount, out ItemProperty property)
        {
            switch (subTypeId)
            {
                case EnhancementSubType.FoodBonusHPRegen: // Food Bonus - HP Regen
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HPRegen, amount); return true;
                case EnhancementSubType.FoodBonusFPRegen: // Food Bonus - FP Regen
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FPRegen, amount); return true;
                case EnhancementSubType.FoodBonusSTMRegen: // Food Bonus - STM Regen
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STMRegen, amount); return true;
                case EnhancementSubType.FoodBonusRestRegen: // Food Bonus - Rest Regen
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.RestRegen, amount); return true;
                case EnhancementSubType.FoodBonusXPBonus: // Food Bonus - XP Bonus
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.XPBonus, amount); return true;
                case EnhancementSubType.FoodBonusCombatReadiness: // Food Bonus - Combat Readiness
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CombatReadiness, amount); return true;
                case EnhancementSubType.FoodBonusDuration: // Food Bonus - Duration
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, amount); return true;
                case EnhancementSubType.FoodBonusHP: // Food Bonus - HP
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.HP, amount); return true;
                case EnhancementSubType.FoodBonusFP: // Food Bonus - FP
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.FP, amount); return true;
                case EnhancementSubType.FoodBonusSTM: // Food Bonus - STM
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.STM, amount); return true;
                case EnhancementSubType.ControlEngineering: // Control - Engineering
                    property = ItemPropertyCustom(ItemPropertyType.Control, 2, amount); return true;
                case EnhancementSubType.CraftsmanshipEngineering: // Craftsmanship - Engineering
                    property = ItemPropertyCustom(ItemPropertyType.Craftsmanship, 2, amount); return true;
                case EnhancementSubType.ControlFabrication: // Control - Fabrication
                    property = ItemPropertyCustom(ItemPropertyType.Control, 3, amount); return true;
                case EnhancementSubType.CraftsmanshipFabrication: // Craftsmanship - Fabrication
                    property = ItemPropertyCustom(ItemPropertyType.Craftsmanship, 3, amount); return true;
                case EnhancementSubType.ControlAgriculture: // Control - Agriculture
                    property = ItemPropertyCustom(ItemPropertyType.Control, 4, amount); return true;
                case EnhancementSubType.CraftsmanshipAgriculture: // Craftsmanship - Agriculture
                    property = ItemPropertyCustom(ItemPropertyType.Craftsmanship, 4, amount); return true;
                case EnhancementSubType.ModuleBonus: // Module Bonus
                    property = ItemPropertyCustom(ItemPropertyType.ModuleBonus, -1, amount); return true;
                case EnhancementSubType.StarshipHull: // Starship Hull
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 46, amount); return true;
                case EnhancementSubType.StarshipCapacitor: // Starship Capacitor
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 47, amount); return true;
                case EnhancementSubType.StarshipShield: // Starship Shield
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 48, amount); return true;
                case EnhancementSubType.StarshipShieldRechargeRate: // Starship Shield Recharge Rate
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 49, amount); return true;
                case EnhancementSubType.StarshipEMDamage: // Starship EM Damage
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 50, amount); return true;
                case EnhancementSubType.StarshipThermalDamage: // Starship Thermal Damage
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 51, amount); return true;
                case EnhancementSubType.StarshipExplosiveDamage: // Starship Explosive Damage
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 52, amount); return true;
                case EnhancementSubType.StarshipAccuracy: // Starship Accuracy
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 53, amount); return true;
                case EnhancementSubType.StarshipEvasion: // Starship Evasion
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 54, amount); return true;
                case EnhancementSubType.StarshipThermalDefense: // Starship Thermal Defense
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 55, amount); return true;
                case EnhancementSubType.StarshipExplosiveDefense: // Starship Explosive Defense
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 56, amount); return true;
                case EnhancementSubType.StarshipEMDefense: // Starship EM Defense
                    property = ItemPropertyCustom(ItemPropertyType.StarshipBonus, 57, amount); return true;
                case EnhancementSubType.Agility: // Agility
                    property = ItemPropertyAbilityBonus(AbilityType.Agility, amount); return true;

                // 59 is free

                case EnhancementSubType.FoodBonusAttack: // Food Bonus - Attack
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Attack, amount); return true;
                case EnhancementSubType.FoodBonusAccuracy: // Food Bonus - Accuracy
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Accuracy, amount); return true;
                case EnhancementSubType.FoodBonusPhysicalDefense: // Food Bonus - Physical Defense
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefensePhysical, amount); return true;
                case EnhancementSubType.FoodBonusForceDefense: // Food Bonus - Force Defense
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.DefenseForce, amount); return true;
                case EnhancementSubType.FoodBonusPoisonResistance: // Food Bonus - Poison Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistancePoison, amount); return true;
                case EnhancementSubType.FoodBonusFireResistance: // Food Bonus - Fire Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceFire, amount); return true;
                case EnhancementSubType.FoodBonusIceResistance: // Food Bonus - Ice Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceIce, amount); return true;
                case EnhancementSubType.FoodBonusElectricalResistance: // Food Bonus - Electrical Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceElectrical, amount); return true;
                case EnhancementSubType.FoodBonusMindResistance: // Food Bonus - Mind Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceMind, amount); return true;
                case EnhancementSubType.FoodBonusMobilityResistance: // Food Bonus - Mobility Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceMobility, amount); return true;
                case EnhancementSubType.FoodBonusTraumaResistance: // Food Bonus - Trauma Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceTrauma, amount); return true;
                case EnhancementSubType.FoodBonusDisruptionResistance: // Food Bonus - Disruption Resistance
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ResistanceDisruption, amount); return true;
                case EnhancementSubType.FoodBonusEvasion: // Food Bonus - Evasion
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Evasion, amount); return true;
                case EnhancementSubType.FoodBonusControlSmithery: // Food Bonus - Control Smithery
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlSmithery, amount); return true;
                case EnhancementSubType.FoodBonusCraftsmanshipSmithery: // Food Bonus - Craftsmanship Smithery
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipSmithery, amount); return true;
                case EnhancementSubType.FoodBonusControlFabrication: // Food Bonus - Control Fabrication
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlFabrication, amount); return true;
                case EnhancementSubType.FoodBonusCraftsmanshipFabrication: // Food Bonus - Craftsmanship Fabrication
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipFabrication, amount); return true;
                case EnhancementSubType.FoodBonusControlEngineering: // Food Bonus - Control Engineering
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlEngineering, amount); return true;
                case EnhancementSubType.FoodBonusCraftsmanshipEngineering: // Food Bonus - Craftsmanship Engineering
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipEngineering, amount); return true;
                case EnhancementSubType.FoodBonusControlAgriculture: // Food Bonus - Control Agriculture
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.ControlAgriculture, amount); return true;
                case EnhancementSubType.FoodBonusCraftsmanshipAgriculture: // Food Bonus - Craftsmanship Agriculture
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.CraftsmanshipAgriculture, amount); return true;
                case EnhancementSubType.FoodBonusMight: // Food Bonus - Might
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Might, amount); return true;
                case EnhancementSubType.FoodBonusPerception: // Food Bonus - Perception
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Perception, amount); return true;
                case EnhancementSubType.FoodBonusVitality: // Food Bonus - Vitality
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Vitality, amount); return true;
                case EnhancementSubType.FoodBonusWillpower: // Food Bonus - Willpower
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Willpower, amount); return true;
                case EnhancementSubType.FoodBonusAgility: // Food Bonus - Agility
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Agility, amount); return true;
                case EnhancementSubType.FoodBonusSocial: // Food Bonus - Social
                    property = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Social, amount); return true;
            }

            property = null;
            return false;
        }

        /// <summary>The attack enhancements and the droid stat enhancements.</summary>
        private bool TryBuildAttackAndDroidProperty(EnhancementSubType subTypeId, int amount, out ItemProperty property)
        {
            switch (subTypeId)
            {
                case EnhancementSubType.Attack: // Attack
                    property = ItemPropertyCustom(ItemPropertyType.Attack, -1, amount); return true;
                case EnhancementSubType.ForceAttack: // Force Attack
                    property = ItemPropertyCustom(ItemPropertyType.ForceAttack, -1, amount); return true;

                case EnhancementSubType.DroidResistanceFire: // Droid: Fire Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, amount); return true;
                case EnhancementSubType.DroidResistancePoison: // Droid: Poison Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistancePoison, amount); return true;
                case EnhancementSubType.DroidResistanceElectrical: // Droid: Electrical Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceElectrical, amount); return true;
                case EnhancementSubType.DroidResistanceIce: // Droid: Ice Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceIce, amount); return true;
                case EnhancementSubType.DroidResistanceMind: // Droid: Mind Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMind, amount); return true;
                case EnhancementSubType.DroidResistanceMobility: // Droid: Mobility Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMobility, amount); return true;
                case EnhancementSubType.DroidResistanceTrauma: // Droid: Trauma Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceTrauma, amount); return true;
                case EnhancementSubType.DroidResistanceDisruption: // Droid: Disruption Resistance
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceDisruption, amount); return true;

                case EnhancementSubType.DroidAISlot: // Droid: AI Slot
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AISlots, amount); return true;
                case EnhancementSubType.DroidHP: // Droid: HP
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, amount); return true;
                case EnhancementSubType.DroidSTM: // Droid: STM
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.STM, amount); return true;
                case EnhancementSubType.DroidMGT: // Droid: MGT
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.MGT, amount); return true;
                case EnhancementSubType.DroidPER: // Droid: PER
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.PER, amount); return true;
                case EnhancementSubType.DroidVIT: // Droid: VIT
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.VIT, amount); return true;
                case EnhancementSubType.DroidWIL: // Droid: WIL
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.WIL, amount); return true;
                case EnhancementSubType.DroidAGI: // Droid: AGI
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AGI, amount); return true;
                case EnhancementSubType.DroidSOC: // Droid: SOC
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.SOC, amount); return true;
                case EnhancementSubType.DroidVibroblade: // Droid: Vibroblade
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroblade, amount); return true;
                case EnhancementSubType.DroidVibroknife: // Droid: Vibroknife
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroknife, amount); return true;
                case EnhancementSubType.DroidLightsaber: // Droid: Lightsaber
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Lightsaber, amount); return true;
                case EnhancementSubType.DroidHeavyVibroblade: // Droid: Heavy Vibroblade
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HeavyVibroblade, amount); return true;
                case EnhancementSubType.DroidSpear: // Droid: Spear
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Spear, amount); return true;
                case EnhancementSubType.DroidTwinBlade: // Droid: Twin Blade
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinBlade, amount); return true;
                case EnhancementSubType.DroidSaberstaff: // Droid: Saberstaff
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Saberstaff, amount); return true;
                case EnhancementSubType.DroidKatar: // Droid: Katar
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Katar, amount); return true;
                case EnhancementSubType.DroidStaff: // Droid: Staff
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Staff, amount); return true;
                case EnhancementSubType.DroidPistol: // Droid: Pistol
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol, amount); return true;
                case EnhancementSubType.DroidRifle: // Droid: Rifle
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Rifle, amount); return true;
                case EnhancementSubType.DroidThrowing: // Droid: Throwing
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Throwing, amount); return true;
                case EnhancementSubType.DroidUnarmed: // Droid: Unarmed
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Unarmed, amount); return true;
                case EnhancementSubType.DroidTwinPistol: // Droid: Twin Pistol
                    property = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinPistol, amount); return true;
            }

            property = null;
            return false;
        }

        private ItemProperty BuildResistanceItemProperty(ResistanceType type, int amount)
        {
            return ItemPropertyCustom(
                ItemPropertyType.Resistance,
                (int)type,
                _resistance.EncodeItemPropertyCostTableValue(amount));
        }

        public bool IsWeaponDamageEnhancement(EnhancementSubType subTypeId)
        {
            return subTypeId == EnhancementSubType.DMG;
        }

        public bool TryGetWeaponDamageTypeForEnhancement(
            EnhancementSubType subTypeId,
            CombatDamageType explicitDamageType,
            out CombatDamageType damageType)
        {
            if (!IsWeaponDamageEnhancement(subTypeId))
            {
                damageType = CombatDamageType.Invalid;
                return false;
            }

            // Force conversion belongs to Imbuement Stance, including its FP cost.
            if (explicitDamageType == CombatDamageType.Force)
            {
                damageType = CombatDamageType.Physical;
                return true;
            }

            if (explicitDamageType.IsCharacterDamageType())
            {
                damageType = explicitDamageType;
                return true;
            }

            damageType = CombatDamageType.Physical;
            return true;
        }

        /// <summary>
        /// Applies an enhancement-provided item property onto a crafted item,
        /// merging with any existing property of the same type. DMG amounts are
        /// summed together; a conflicting WeaponDamageType is resolved by randomly
        /// keeping one of the two rather than always favoring the latest one applied.
        /// </summary>
        public void ApplyCraftedItemProperty(uint item, ItemProperty ip)
        {
            var type = GetItemPropertyType(ip);
            var subType = GetItemPropertySubType(ip);
            var amount = GetItemPropertyCostTableValue(ip);

            if (type == ItemPropertyType.WeaponDamageType)
            {
                if (subType == (int)CombatDamageType.Force)
                    return;
                ApplyWeaponDamageTypeProperty(item, ip, subType);
                return;
            }

            // Scan for matches first and remove them in a separate pass afterward -
            // removing an item property while GetNextItemProperty is still iterating
            // shifts the underlying list, which silently skips every other match.
            //
            // Subtype matching is judged off the INCOMING property's subtype, not the
            // existing property's. Property types with no subtype table in
            // itempropdef.2da (DMG, Attack, ForceAttack, Evasion, HPBonus, FP, Stamina,
            // ShieldDeflection, CombatReadiness, StructureBonus, ...) are always packed
            // with subtype -1, but re-querying that -1 back off a property already
            // attached to a live item is not reliable - the engine has no subtype table
            // to round-trip it through. Trusting the freshly-built incoming property's
            // subtype instead of re-reading the attached one is what actually holds: if
            // the enhancement never had a meaningful subtype to begin with, any existing
            // property of the same type is a match; if it does (Defense, Resistance,
            // FoodBonus, DroidStat), only the same subtype matches.
            var matches = new List<ItemProperty>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == type &&
                    (subType == -1 || GetItemPropertySubType(property) == subType))
                {
                    amount += GetItemPropertyCostTableValue(property);
                    matches.Add(property);
                }
            }

            foreach (var property in matches)
            {
                RemoveItemProperty(item, property);
            }

            var unpacked = ItemPropertyPlugin.UnpackIP(ip);
            unpacked.CostTableValue = amount;
            ip = ItemPropertyPlugin.PackIP(unpacked);

            BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
        }

        /// <summary>
        /// Merges a crafted property into a list of properties by the rules <see cref="ApplyCraftedItemProperty"/> applies to an item:
        /// a property of the same type (and subtype, when the incoming one has one) is summed into one, a weapon keeps one damage type,
        /// and Force is never a weapon damage type. Working on a list lets a craft settle every property before any of them reaches
        /// the item, so the item never has a property removed and re-added: a removal is not committed to the item's saved property
        /// list until later, so a serialized form or copy taken in the same script would still carry the pre-merge property.
        /// </summary>
        public void MergeCraftedItemProperty(List<ItemProperty> properties, ItemProperty ip)
        {
            var type = GetItemPropertyType(ip);
            var subType = GetItemPropertySubType(ip);

            if (type == ItemPropertyType.WeaponDamageType)
            {
                if (subType == (int)CombatDamageType.Force)
                    return;

                var damageTypes = properties.Where(property => GetItemPropertyType(property) == ItemPropertyType.WeaponDamageType).ToList();
                var keepExisting = damageTypes.Count > 0 &&
                                   GetItemPropertySubType(damageTypes[0]) != subType &&
                                   Domain.Foundation.Random.D100(1) <= 50;
                if (keepExisting)
                    return;

                properties.RemoveAll(property => GetItemPropertyType(property) == ItemPropertyType.WeaponDamageType);
                properties.Add(ip);
                return;
            }

            // The same matching as ApplyCraftedItemProperty: judged off the incoming property's subtype.
            var amount = GetItemPropertyCostTableValue(ip);
            var matches = properties
                .Where(property => GetItemPropertyType(property) == type &&
                                   (subType == -1 || GetItemPropertySubType(property) == subType))
                .ToList();
            foreach (var match in matches)
            {
                amount += GetItemPropertyCostTableValue(match);
                properties.Remove(match);
            }

            var unpacked = ItemPropertyPlugin.UnpackIP(ip);
            unpacked.CostTableValue = amount;
            properties.Add(ItemPropertyPlugin.PackIP(unpacked));
        }

        /// <summary>
        /// A weapon can only carry one damage type. If it already has a different
        /// type than the one being applied now, keep only one - chosen at random
        /// rather than always letting whichever enhancement was socketed last
        /// silently win.
        /// </summary>
        private void ApplyWeaponDamageTypeProperty(uint item, ItemProperty ip, int subType)
        {
            var damageTypeMatches = new List<ItemProperty>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == ItemPropertyType.WeaponDamageType)
                {
                    damageTypeMatches.Add(property);
                }
            }

            var keepExisting = damageTypeMatches.Count > 0 &&
                                GetItemPropertySubType(damageTypeMatches[0]) != subType &&
                                Domain.Foundation.Random.D100(1) <= 50;

            if (keepExisting)
                return;

            foreach (var property in damageTypeMatches)
            {
                RemoveItemProperty(item, property);
            }

            BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);
        }
    }
}
