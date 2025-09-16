using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using SavingThrow = SWLOR.NWN.API.NWScript.Enum.SavingThrow;

namespace SWLOR.Game.Server.Service
{
    public static class Combat
    {
        private static readonly List<CombatDamageType> _allValidDamageTypes = new();

        /// <summary>
        /// When the module loads, add all valid damage types to the cache.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadDamageTypes()
        {
            var allValues = Enum.GetValues(typeof(CombatDamageType)).Cast<CombatDamageType>();

            foreach (var type in allValues)
            {
                if (type == CombatDamageType.Invalid)
                    continue;

                _allValidDamageTypes.Add(type);
            }
        }

        /// <summary>
        /// When a player enters the server, apply any defenses towards damage types they don't already have.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AddDamageTypeDefenses()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var foundNewType = false;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            foreach (var type in _allValidDamageTypes)
            {
                if (!dbPlayer.Defenses.ContainsKey(type))
                {
                    foundNewType = true;
                    dbPlayer.Defenses[type] = 0;
                }
            }

            if (foundNewType)
            {
                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public static List<CombatDamageType> GetAllDamageTypes()
        {
            return _allValidDamageTypes.ToList();
        }

        /// <summary>
        /// Calculates the minimum and maximum damage possible with the provided stats.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A minimum and maximum damage range</returns>
        public static (int, int) CalculateDamageRange(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            const float RatioMax = 3.625f;
            const float RatioMin = 0.01f;

            if (defenderDefense < 1)
                defenderDefense = 1;

            var statDelta = attackerStat - defenderStat;
            if (deltaCap > 0) Math.Clamp(statDelta, -deltaCap, 8 + deltaCap);
            var baseDamage = attackerDMG + statDelta;
            var ratio = (float)attackerAttack / (float)defenderDefense;

            if (ratio > RatioMax)
                ratio = RatioMax;
            else if (ratio < RatioMin)
                ratio = RatioMin;

            var maxDamage = baseDamage * ratio;
            var minDamage = maxDamage * 0.70f;

            Log.Write(LogGroup.Attack, $"attackerAttack = {attackerAttack}, attackerDMG = {attackerDMG}, attackerStat = {attackerStat}, defenderDefense = {defenderDefense}, defenderStat = {defenderStat}, critical = {critical}");
            Log.Write(LogGroup.Attack, $"statDelta = {statDelta}, baseDamage = {baseDamage}, ratio = {ratio}, minDamage = {minDamage}, maxDamage = {maxDamage}");

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                maxDamage *= ((critical - 1) / 4.0f) + 1.0f;
                Log.Write(LogGroup.Attack, $"Critical Multiplier: {critical}, minDamage = {minDamage}, maxDamage = {maxDamage}");
            }

            return ((int)minDamage, (int)maxDamage);
        }

        /// <summary>
        /// Calculates the hit rate against a given target.
        /// Range is clamped to values between 20 and 95, inclusive.
        /// </summary>
        /// <param name="attackerAccuracy">The total accuracy of the attacker.</param>
        /// <param name="defenderEvasion">The total evasion of the defender.</param>
        /// <param name="percentageModifier">Modifies the raw hit change by a certain percentage. This is done after all prior calculations.</param>
        /// <returns>The hit rate, clamped between 20 and 95, inclusive.</returns>
        public static int CalculateHitRate(
            int attackerAccuracy,
            int defenderEvasion,
            int percentageModifier)
        {
            const int BaseHitRate = 75;
            
            var hitRate = BaseHitRate + (int)Math.Floor((attackerAccuracy - defenderEvasion) / 2.0f) + percentageModifier;

            if (hitRate < 20)
                hitRate = 20;
            else if (hitRate > 95)
                hitRate = 95;

            return hitRate;
        }

        /// <summary>
        /// Calculates the critical hit rate against a given target.
        /// </summary>
        /// <param name="attackerPER">The attacker's perception stat</param>
        /// <param name="defenderMGT">The defender's might stat.</param>
        /// <param name="criticalModifier">A modifier to the critical rating based on external factors.</param>
        /// <returns>The critical rate, in a percentage</returns>
        public static int CalculateCriticalRate(int attackerPER, int defenderMGT, int criticalModifier)
        {
            const int BaseCriticalRate = 5;
            var delta = attackerPER - defenderMGT;

            if (delta < 0)
                delta = 0;
            else if (delta > 15)
                delta = 15;

            var criticalRate = BaseCriticalRate + delta + criticalModifier;
            if (criticalRate < BaseCriticalRate)
                criticalRate = BaseCriticalRate;
            else if (criticalRate > 90)
                criticalRate = 90;


            return criticalRate;
        }

        /// <summary>
        /// Calculates a random damage amount based on the provided stats of the attacker and defender.
        /// </summary>
        /// <param name="attackerAttack">The attacker's attack rating.</param>
        /// <param name="attackerDMG">The attacker's DMG rating</param>
        /// <param name="attackerStat">The attacker's attack stat value</param>
        /// <param name="defenderDefense">The defender's defense rating.</param>
        /// <param name="defenderStat">The defender's defend stat value</param>
        /// <param name="critical">the critical rating of the attack, or 0 if the attack is not critical.</param>
        /// <param name="deltaCap">Value to cap the lower and upper bounds of stat delta to. For weapons, should be weapon rank.</param>
        /// <returns>A damage value to apply to the target.</returns>
        public static int CalculateDamage(
            int attackerAttack,
            int attackerDMG,
            int attackerStat,
            int defenderDefense,
            int defenderStat,
            int critical,
            int deltaCap = 0)
        {
            var (minDamage, maxDamage) = CalculateDamageRange(
                attackerAttack,
                attackerDMG,
                attackerStat,
                defenderDefense,
                defenderStat,
                critical,
                deltaCap);

            return (int)Random.NextFloat(minDamage, maxDamage);
        }

        /// <summary>
        /// Return a damage bonus equal to 0.15 of the player's relevant skill or an NPC's level.
        /// This helps abilities as the player progresses. 
        ///
        /// Global scaling on gear is closer to 0.25 DMG per player skill level so low tier abilities will still
        /// become less useful over time, and get replaced by higher tier ones.  But they will have some utility still.
        /// </summary>
        /// <returns> 0.15 * the player's rank in the specified skill, or the level for NPCs.</returns>

        public static int GetAbilityDamageBonus(uint creature, SkillType skill)
        {
            var level = 0;
            if (!GetIsPC(creature))
            {
                var npcStats = Stat.GetNPCStats(creature);
                level = npcStats.Level;
            }
            else
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                var pcSkill = dbPlayer.Skills[skill];
                level = pcSkill.Rank;
            }


            return (int)(0.15f * level);
        }

        /// <summary>
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void ClearCombatState()
        {
            uint player = OBJECT_SELF;

            // Clear combat state.
            if (!GetIsInCombat(player))
            {
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_X");
                DeleteLocalFloat(player, "ATTACK_ORIENTATION_Y");
            }
        }

        /// <summary>
        /// Builds a combat log message based on the provided information.
        /// </summary>
        /// <param name="attacker">The id of the attacker</param>
        /// <param name="defender">The id of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public static string BuildCombatLogMessage(
            uint attacker,
            uint defender,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
            }

            var attackerName = GetIsPC(attacker) ? ColorToken.GetNamePCColor(attacker) : ColorToken.GetNameNPCColor(attacker);
            var defenderName = GetIsPC(defender) ? ColorToken.GetNamePCColor(defender) : ColorToken.GetNameNPCColor(defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        /// <summary>
        /// Builds a combat log message based on the provided information, for native contexts.
        /// </summary>
        /// <param name="attacker">The CNWSCreature of the attacker</param>
        /// <param name="defender">The CNWSCreature of the defender</param>
        /// <param name="attackResultType">The type of result. 1, 7 = Hit, 3 = Critical, 4 = Miss</param>
        /// <param name="chanceToHit">The percent chance to hit</param>
        /// <returns></returns>
        public static string BuildCombatLogMessageNative(
            CNWSCreature attacker,
            CNWSCreature defender,
            int attackResultType,
            int chanceToHit)
        {
            var type = string.Empty;

            switch (attackResultType)
            {
                case 1:
                case 7:
                    type = ": *hit*";
                    break;
                case 3:
                    type = ": *critical*";
                    break;
                case 4:
                    type = ": *miss*";
                    break;
                case 2:
                    type = ": *deflect*";
                    break;
            }

            var attackerName = ColorToken.GetNameColorNative(attacker);
            var defenderName = ColorToken.GetNameColorNative(defender);

            return ColorToken.Combat($"{attackerName} attacks {defenderName}{type} : ({chanceToHit}% chance to hit)");
        }

        /// <summary>
        /// Check for weapon type and perk. Returns either the default ability score or the perk replaced ability score if the user has the relevant perk or active stance.
        /// This is currently used for zen marksmanship, strong style, crushing style, and flurry style.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The correct damage ability score, or 0 if a weapon is not equipped.</returns>

        public static int GetPerkAdjustedAbilityScore(uint attacker)
        {
            var weapon = GetItemInSlot(InventorySlot.RightHand, attacker);
            if (!GetIsObjectValid(weapon)) return 0;
            var weaponType = GetBaseItemType(weapon);

            // Pistol and Rifle - Zen Marksmanship
            if (Item.PistolBaseItemTypes.Contains(weaponType) || Item.RifleBaseItemTypes.Contains(weaponType))
            {
                var willpower = GetAbilityScore(attacker, AbilityType.Willpower);
                var perception = GetAbilityScore(attacker, AbilityType.Perception);
                return (GetHasFeat(FeatType.ZenMarksmanship, attacker) && (willpower > perception)) ? willpower : perception;
            }

            // Throwing - Zen Marksmanship
            if (Item.ThrowingWeaponBaseItemTypes.Contains(weaponType))
            {
                var willpower = GetAbilityScore(attacker, AbilityType.Willpower);
                var might = GetAbilityScore(attacker, AbilityType.Might);
                return (GetHasFeat(FeatType.ZenMarksmanship, attacker) && (willpower > might)) ? willpower : might;
            }

            // Lightsaber - Strong Style
            if (Item.LightsaberBaseItemTypes.Contains(weaponType))
                return Ability.IsAbilityToggled(attacker, AbilityService.AbilityToggleType.StrongStyleLightsaber) ? GetAbilityScore(attacker, AbilityType.Might) : GetAbilityScore(attacker, AbilityType.Perception);

            // Saberstaff - Strong Style
            if (Item.SaberstaffBaseItemTypes.Contains(weaponType))
                return Ability.IsAbilityToggled(attacker, AbilityService.AbilityToggleType.StrongStyleSaberstaff) ? GetAbilityScore(attacker, AbilityType.Might) : GetAbilityScore(attacker, AbilityType.Perception);

            // Staff: there are 3 style perks for staff so it has to be handled slightly differently.
            if (Item.StaffBaseItemTypes.Contains(weaponType))
            {
                if (GetHasFeat(FeatType.FlurryStyle)) return GetAbilityScore(attacker, AbilityType.Perception);
                if (GetHasFeat(FeatType.CrushingMastery)) return 3 * GetAbilityScore(attacker, AbilityType.Might);
                if (GetHasFeat(FeatType.CrushingStyle)) return 2 * GetAbilityScore(attacker, AbilityType.Might);
                return GetAbilityScore(attacker, AbilityType.Might);
            }

            //Handle weapon types without ability adjustment perks as well for consistency.
            return GetAbilityScore(attacker, Item.GetWeaponDamageAbilityType(weaponType));
        }

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand, Power Attack, and Might scaling.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <param name="weaponType">The BaseItem of the weapon held</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>

        public static int GetMiscDMGBonus(uint attacker, BaseItem weaponType)
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

        public static int GetMightDMGBonus(uint attacker, BaseItem weaponType)
        {
            var mgtMod = GetAbilityModifier(AbilityType.Might, attacker);

            if (Item.StaffBaseItemTypes.Contains(weaponType))
                return mgtMod * Perk.GetPerkLevel(attacker, PerkService.PerkType.CrushingStyle);
            else if (Item.LightsaberBaseItemTypes.Contains(weaponType) && Ability.IsAbilityToggled(attacker, AbilityService.AbilityToggleType.StrongStyleLightsaber))
                return mgtMod / 2;
            else if (Item.SaberstaffBaseItemTypes.Contains(weaponType) && Ability.IsAbilityToggled(attacker, AbilityService.AbilityToggleType.StrongStyleSaberstaff))
                return mgtMod / 2;

            return 0;

        }

        /// <summary>
        /// Retrieves the DMG bonus granted by doublehand.
        /// If attacker does not meet the requirements of Doublehand, 0 will be returned.
        /// </summary>
        /// <param name="attacker">The attacker to check</param>
        /// <returns>The DMG value or 0 if requirements are not met.</returns>
        public static int GetDoublehandDMGBonus(uint attacker)
        {
            var dmg = 0;
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, attacker);

            if (!GetIsObjectValid(rightHand) || GetIsObjectValid(leftHand))
                return 0;

            var rightHandType = GetBaseItemType(rightHand);
            if (!Item.OneHandedMeleeItemTypes.Contains(rightHandType) && 
                !Item.ThrowingWeaponBaseItemTypes.Contains(rightHandType))
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
        public static int GetPowerAttackDMGBonus(uint attacker)
        {
            if (GetActionMode(attacker, ActionMode.PowerAttack))
                return 3;
            else if (GetActionMode(attacker, ActionMode.ImprovedPowerAttack))
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
        public static int GetDoublehandDMGBonusNative(CNWSCreature attacker)
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
        public static int CalculateSavingThrowDC(
            uint attacker, 
            SavingThrow type, 
            int baseDC, 
            AbilityType abilityOverride = AbilityType.Invalid)
        {
            var ability = abilityOverride;

            if (ability == AbilityType.Invalid)
            {
                switch (type)
                {
                    case SavingThrow.Fortitude:
                        ability = AbilityType.Might;
                        break;
                    case SavingThrow.Reflex:
                        ability = AbilityType.Perception;
                        break;
                    case SavingThrow.Will:
                        ability = AbilityType.Willpower;
                        break;
                    default:
                        return baseDC;
                }
            }

            var modifier = GetAbilityModifier(ability, attacker);

            return baseDC + modifier;
        }

        /// <summary>
        /// Calculates the attack delay for a creature based on their equipped weapons.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay for.</param>
        /// <returns>Attack delay in milliseconds.</returns>
        public static int CalculateAttackDelay(uint attacker)
        {
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);
            var leftHand = GetItemInSlot(InventorySlot.LeftHand, attacker);
            
            var delay = 0;
            var rightHandDelay = 0;
            var leftHandDelay = 0;
            
            // Get delay from right hand weapon
            if (GetIsObjectValid(rightHand))
            {
                for(var ip = GetFirstItemProperty(rightHand); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(rightHand))
                {
                    if(GetItemPropertyType(ip) == ItemPropertyType.Delay)
                    {
                        var costValue = GetItemPropertyCostTableValue(ip);
                        rightHandDelay += costValue * 10;
                    }
                }
            }
            
            // Get delay from left hand weapon
            if (GetIsObjectValid(leftHand))
            {
                for (var ip = GetFirstItemProperty(leftHand); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(leftHand))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                    {
                        var costValue = GetItemPropertyCostTableValue(ip);
                        leftHandDelay += costValue * 10;
                    }
                }
            }

            delay = rightHandDelay + leftHandDelay;

            // Convert delay units to milliseconds: 60 delay units = 1 second
            var finalDelay = (int)(delay / 60f * 1000);
            
            // Apply feat-based delay reduction
            var reductionPercentage = CalculateAttackDelayReduction(attacker);
            if (reductionPercentage > 0)
            {
                var reductionAmount = (int)(finalDelay * (reductionPercentage / 100f));
                finalDelay -= reductionAmount;
            }
            
            return finalDelay;
        }

        /// <summary>
        /// Handles paralyze status effect for a creature.
        /// </summary>
        /// <param name="attacker">The creature to check for paralyze.</param>
        /// <returns>True if the creature is paralyzed and cannot act.</returns>
        public static bool HandleParalyze(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return false;

            var hasParalyze = false;
            for (var effect = GetFirstEffect(attacker); GetIsEffectValid(effect); effect = GetNextEffect(attacker))
            {
                if (GetEffectType(effect) == EffectTypeScript.Paralyze)
                {
                    hasParalyze = true;
                    break;
                }
            }

            if (hasParalyze)
            {
                var creatureName = GetName(attacker);
                Messaging.SendMessageNearbyToPlayers(attacker, $"{creatureName} is paralyzed and cannot act!");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Gets the Hasten effect level for a creature.
        /// </summary>
        /// <param name="creature">The creature to check for Hasten effect.</param>
        /// <returns>The Hasten effect level (0-3).</returns>
        private static int GetHastenLevel(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return 0;

            // Check for Hasten status effects using the StatusEffect service
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Hasten3))
                return 3;
            else if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Hasten2))
                return 2;
            else if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Hasten1))
                return 1;

            return 0;
        }

        /// <summary>
        /// Calculates the attack delay reduction percentage based on creature perks.
        /// Cumulative reductions are capped at 50% maximum.
        /// </summary>
        /// <param name="attacker">The creature to calculate delay reduction for.</param>
        /// <returns>Attack delay reduction percentage (0-50).</returns>
        public static int CalculateAttackDelayReduction(uint attacker)
        {
            if (!GetIsObjectValid(attacker))
                return 0;

            var totalReduction = 0;

            // Rapid Shot - 10% per level (up to 2 levels = 20% total)
            var rapidShotLevel = Perk.GetPerkLevel(attacker, PerkType.RapidShot);
            if (rapidShotLevel > 0)
                totalReduction += rapidShotLevel * 10;

            // Flurry Style - 20% reduction
            if (GetHasFeat(FeatType.FlurryStyle, attacker))
                totalReduction += 20;

            // Hasten - 10% per level (up to 3 levels = 30% total)
            var hastenLevel = GetHastenLevel(attacker);
            if (hastenLevel > 0)
                totalReduction += hastenLevel * 10;

            // Beast Speed - 10% per level (up to 3 levels = 30% total)
            var beastSpeedLevel = Perk.GetPerkLevel(attacker, PerkType.BeastSpeed);
            if (beastSpeedLevel > 0)
                totalReduction += beastSpeedLevel * 10;

            // Cap the total reduction at 50%
            if (totalReduction > 50)
                totalReduction = 50;

            return totalReduction;
        }
    }
}
