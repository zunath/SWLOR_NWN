using Microsoft.Extensions.DependencyInjection;
using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Combat.Service
{
    

    public class CombatService : ICombatService
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IStatCalculationService> _statCalculationService;

        private readonly IWeaponStatService _weaponStatService;

        public CombatService(
            IDatabaseService db, 
            IRandomService random, 
            IServiceProvider serviceProvider,
            IWeaponStatService weaponStatService)
        {
            _db = db;
            _random = random;
            _serviceProvider = serviceProvider;
            _weaponStatService = weaponStatService;

            // Initialize lazy services
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _statCalculationService = new Lazy<IStatCalculationService>(() => _serviceProvider.GetRequiredService<IStatCalculationService>());
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _abilityService.Value;
        private IItemService ItemService => _itemService.Value;
        private IPerkService PerkService => _perkService.Value;
        private IStatCalculationService StatCalculationService => _statCalculationService.Value;


        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses. 
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>

        public int GetAbilityDamageBonus(uint creature, SkillType skill)
        {
            var level = 0;
            if (!GetIsPC(creature))
            {
                level = StatCalculationService.CalculateLevel(creature);
            }
            else
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                var pcSkill = dbPlayer.Skills[skill];
                level = pcSkill.Rank;
            }


            return (int)(0.15f * level);
        }

        /// <summary>
        /// Check for weapon type and perk. Returns either the default ability score or the perk replaced ability score if the user has the relevant perk or active stance.
        /// This is currently used for zen marksmanship, strong style, crushing style, and flurry style.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The correct damage ability score, or 0 if a weapon is not equipped.</returns>

        public int GetPerkAdjustedAbilityScore(uint attacker)
        {
            var weapon = GetItemInSlot(InventorySlotType.RightHand, attacker);
            if (!GetIsObjectValid(weapon)) return 0;
            var weaponType = GetBaseItemType(weapon);

            // Pistol and Rifle - Zen Marksmanship
            if (ItemService.PistolBaseItemTypes.Contains(weaponType) || ItemService.RifleBaseItemTypes.Contains(weaponType))
            {
                var willpower = GetAbilityScore(attacker, AbilityType.Willpower);
                var perception = GetAbilityScore(attacker, AbilityType.Perception);
                return (GetHasFeat(FeatType.ZenMarksmanship, attacker) && (willpower > perception)) ? willpower : perception;
            }

            // Throwing - Zen Marksmanship
            if (ItemService.ThrowingWeaponBaseItemTypes.Contains(weaponType))
            {
                var willpower = GetAbilityScore(attacker, AbilityType.Willpower);
                var might = GetAbilityScore(attacker, AbilityType.Might);
                return (GetHasFeat(FeatType.ZenMarksmanship, attacker) && (willpower > might)) ? willpower : might;
            }

            // Lightsaber - Strong Style
            if (ItemService.LightsaberBaseItemTypes.Contains(weaponType))
                return AbilityService.IsAbilityToggled(attacker, AbilityToggleType.StrongStyleLightsaber) ? GetAbilityScore(attacker, AbilityType.Might) : GetAbilityScore(attacker, AbilityType.Perception);

            // Saberstaff - Strong Style
            if (ItemService.SaberstaffBaseItemTypes.Contains(weaponType))
                return AbilityService.IsAbilityToggled(attacker, AbilityToggleType.StrongStyleSaberstaff) ? GetAbilityScore(attacker, AbilityType.Might) : GetAbilityScore(attacker, AbilityType.Perception);

            // Staff: there are 3 style perks for staff so it has to be handled slightly differently.
            if (ItemService.StaffBaseItemTypes.Contains(weaponType))
            {
                if (GetHasFeat(FeatType.FlurryStyle)) return GetAbilityScore(attacker, AbilityType.Perception);
                if (GetHasFeat(FeatType.CrushingMastery)) return 3 * GetAbilityScore(attacker, AbilityType.Might);
                if (GetHasFeat(FeatType.CrushingStyle)) return 2 * GetAbilityScore(attacker, AbilityType.Might);
                return GetAbilityScore(attacker, AbilityType.Might);
            }

            //Handle weapon types without ability adjustment perks as well for consistency.
            var weaponStat = _weaponStatService.LoadWeaponStat(weapon);
            return GetAbilityScore(attacker, weaponStat.DamageStat);
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand, Power Attack, and Might scaling.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>

        public int GetMiscDMGBonus(uint attacker, BaseItemType weaponType)
        {
            var bonusDMG = 0;

            bonusDMG += GetDoublehandDMGBonus(attacker) +
                GetPowerAttackDMGBonus(attacker) +
                GetMightDMGBonus(attacker, weaponType);

            return bonusDMG;
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by Might scaling on Crushing Style Staves and Strong Style Sabers.
        /// Returns 0 if an invalid weapon is held.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>

        public int GetMightDMGBonus(uint attacker, BaseItemType weaponType)
        {
            var mgtMod = GetAbilityModifier(AbilityType.Might, attacker);

            if (ItemService.StaffBaseItemTypes.Contains(weaponType))
                return mgtMod * PerkService.GetPerkLevel(attacker, PerkType.CrushingStyle);
            else if (ItemService.LightsaberBaseItemTypes.Contains(weaponType) && AbilityService.IsAbilityToggled(attacker, AbilityToggleType.StrongStyleLightsaber))
                return mgtMod / 2;
            else if (ItemService.SaberstaffBaseItemTypes.Contains(weaponType) && AbilityService.IsAbilityToggled(attacker, AbilityToggleType.StrongStyleSaberstaff))
                return mgtMod / 2;

            return 0;

        }

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand.
        /// If attacker does not meet the requirements of Doublehand, 0 will be returned.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        public int GetDoublehandDMGBonus(uint attacker)
        {
            var dmg = 0;
            var rightHand = GetItemInSlot(InventorySlotType.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlotType.LeftHand, attacker);

            if (!GetIsObjectValid(rightHand) || GetIsObjectValid(leftHand))
                return 0;

            var rightHandType = GetBaseItemType(rightHand);
            if (!ItemService.OneHandedMeleeItemTypes.Contains(rightHandType) && 
                !ItemService.ThrowingWeaponBaseItemTypes.Contains(rightHandType))
                return 0;

            if (GetHasFeat(FeatType.Doublehand5, attacker))
                dmg = 19;
            else if (GetHasFeat(FeatType.Doublehand4, attacker))
                dmg = 14;
            else if (GetHasFeat(FeatType.Doublehand3, attacker))
                dmg = 10;
            else if (GetHasFeat(FeatType.Doublehand2, attacker))
                dmg = 6;
            else if (GetHasFeat(FeatType.Doublehand1, attacker))
                dmg = 2;

            return dmg;
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by Power Attack.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <returns>The DMG bonus, or 0 if Power Attack is not enabled.</returns>
        public int GetPowerAttackDMGBonus(uint attacker)
        {
            if (GetActionMode(attacker, ActionModeType.PowerAttack))
                return 3;
            else if (GetActionMode(attacker, ActionModeType.ImprovedPowerAttack))
                return 6;
            return 0;
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand.
        /// If attacker does not meet the requirements of Doublehand, 0 will be returned.
        /// Must be called from within a native context.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        public int GetDoublehandDMGBonusNative(CNWSCreature attacker)
        {
            var dmg = 0;

            if (attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand5) == 1)
                dmg = 19;
            else if (attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand4) == 1)
                dmg = 14;
            else if (attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand3) == 1)
                dmg = 10;
            else if (attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand2) == 1)
                dmg = 6;
            else if (attacker.m_pStats.HasFeat((ushort)FeatType.Doublehand1) == 1)
                dmg = 2;

            return dmg;
        }

        /// <summary>
        /// Determines the DC for an attacker's saving throw.
        /// </summary>
        /// <param name="attacker">The attacker to check.</param>
        /// <param name="type">The type of saving throw.</param>
        /// <param name="baseDC">The base DC amount.</param>
        /// <param name="abilityOverride">Use this to specify a specific ability to be used.</param>
        /// <returns>A DC value with any bonuses applied.</returns>
        public int CalculateSavingThrowDC(
            uint attacker, 
            SavingThrowCategoryType type, 
            int baseDC, 
            AbilityType abilityOverride = AbilityType.Invalid)
        {
            var ability = abilityOverride;

            if (ability == AbilityType.Invalid)
            {
                switch (type)
                {
                    case SavingThrowCategoryType.Fortitude:
                        ability = AbilityType.Might;
                        break;
                    case SavingThrowCategoryType.Reflex:
                        ability = AbilityType.Perception;
                        break;
                    case SavingThrowCategoryType.Will:
                        ability = AbilityType.Willpower;
                        break;
                    default:
                        return baseDC;
                }
            }

            var modifier = GetAbilityModifier(ability, attacker);

            return baseDC + modifier;
        }

    }
}
