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
        private readonly ILogger _logger;
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<CombatDamageType> _allValidDamageTypes = new();
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IStatService> _statService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IPerkService> _perkService;

        private readonly IStatServiceNew _statServiceNew;
        private readonly IMessagingService _messaging;

        public CombatService(
            ILogger logger, 
            IDatabaseService db, 
            IRandomService random, 
            IServiceProvider serviceProvider,
            IStatServiceNew statServiceNew,
            IMessagingService messagingService)
        {
            _logger = logger;
            _db = db;
            _random = random;
            _serviceProvider = serviceProvider;
            _statServiceNew = statServiceNew;
            _messaging = messagingService;

            // Initialize lazy services
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _statService = new Lazy<IStatService>(() => _serviceProvider.GetRequiredService<IStatService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
        }

        // Lazy-loaded services to break circular dependencies
        private IAbilityService AbilityService => _abilityService.Value;
        private IStatService StatService => _statService.Value;
        private IItemService ItemService => _itemService.Value;
        private IPerkService PerkService => _perkService.Value;

        /// <summary>
        /// When the module loads, add all valid damage types to the cache.
        /// </summary>
        public void LoadDamageTypes()
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
        public void AddDamageTypeDefenses()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var foundNewType = false;
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
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
                _db.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Retrieves all valid damage types available in the system.
        /// </summary>
        /// <returns>A list of damage types</returns>
        public List<CombatDamageType> GetAllDamageTypes()
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
        public (int, int) CalculateDamageRange(
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

            _logger.Write<AttackLogGroup>($"attackerAttack = {attackerAttack}, attackerDMG = {attackerDMG}, attackerStat = {attackerStat}, defenderDefense = {defenderDefense}, defenderStat = {defenderStat}, critical = {critical}");
            _logger.Write<AttackLogGroup>($"statDelta = {statDelta}, baseDamage = {baseDamage}, ratio = {ratio}, minDamage = {minDamage}, maxDamage = {maxDamage}");

            // Criticals - 25% bonus to damage range per multiplier point.
            if (critical > 0)
            {
                minDamage = maxDamage;
                maxDamage *= ((critical - 1) / 4.0f) + 1.0f;
                _logger.Write<AttackLogGroup>($"Critical Multiplier: {critical}, minDamage = {minDamage}, maxDamage = {maxDamage}");
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
        public int CalculateHitRate(
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
        public int CalculateCriticalRate(int attackerPER, int defenderMGT, int criticalModifier)
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
        public int CalculateDamage(
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

            return (int)_random.NextFloat(minDamage, maxDamage);
        }

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
                var npcStats = StatService.GetNPCStats(creature);
                level = npcStats.Level;
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
        /// On module heartbeat, clear a PC's saved combat facing if they are no longer in combat.
        /// </summary>
        public void ClearCombatState()
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
        public string BuildCombatLogMessage(
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
        public string BuildCombatLogMessageNative(
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
            return GetAbilityScore(attacker, ItemService.GetWeaponDamageAbilityType(weaponType));
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

        public bool HandleParalyze(uint creature)
        {
            var paralysis = _statServiceNew.CalculateParalysis(creature);
            if (paralysis <= 0)
                return false;

            var isParalyzed = _random.D100(1) <= paralysis;
            if (isParalyzed)
            {
                SendParalysisMessage(creature);
                return true;
            }

            return false;
        }

        private void SendParalysisMessage(uint creature)
        {
            var name = GetName(creature);
            _messaging.SendMessageNearbyToPlayers(creature, $"{name} is paralyzed and cannot act!");
        }
    }
}
