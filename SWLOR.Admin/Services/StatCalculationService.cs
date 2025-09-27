using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Admin.Services
{
    public class StatCalculationService
    {
        // Base values from Stat.cs
        public const int BaseHP = 70;
        public const int BaseFP = 10;
        public const int BaseSTM = 10;

        /// <summary>
        /// Calculates the ability modifier for a given stat
        /// </summary>
        /// <param name="stat">The raw stat value</param>
        /// <returns>The ability modifier</returns>
        public static int CalculateAbilityModifier(int stat)
        {
            return (stat - 10) / 2;
        }

        /// <summary>
        /// Gets the ability modifier for a specific ability type from player stats
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="abilityType">The ability type</param>
        /// <returns>The ability modifier</returns>
        public static int GetAbilityModifier(Player player, AbilityType abilityType)
        {
            if (player?.BaseStats == null || !player.BaseStats.ContainsKey(abilityType))
                return 0;

            var stat = player.BaseStats[abilityType];
            return CalculateAbilityModifier(stat);
        }

        /// <summary>
        /// Gets the raw ability score for a specific ability type
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="abilityType">The ability type</param>
        /// <returns>The raw ability score</returns>
        public static int GetAbilityScore(Player player, AbilityType abilityType)
        {
            if (player?.BaseStats == null || !player.BaseStats.ContainsKey(abilityType))
                return 0;

            return player.BaseStats[abilityType];
        }

        /// <summary>
        /// Gets the skill level for a specific skill type
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="skillType">The skill type</param>
        /// <returns>The skill level</returns>
        public static int GetSkillLevel(Player player, SkillType skillType)
        {
            if (player?.Skills == null || !player.Skills.ContainsKey(skillType))
                return 0;

            return player.Skills[skillType].Rank;
        }

        /// <summary>
        /// Calculates max FP using the formula: BaseFP + (Willpower Modifier × 10) + Food Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="foodBonus">Optional food bonus (default 0)</param>
        /// <returns>The calculated max FP</returns>
        public static int CalculateMaxFP(Player player, int foodBonus = 0)
        {
            if (player == null) return BaseFP;

            var willpowerMod = GetAbilityModifier(player, AbilityType.Willpower);
            return BaseFP + (willpowerMod * 10) + foodBonus;
        }

        /// <summary>
        /// Calculates max Stamina using the formula: BaseSTM + (Agility Modifier × 5) + Food Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="foodBonus">Optional food bonus (default 0)</param>
        /// <returns>The calculated max Stamina</returns>
        public static int CalculateMaxStamina(Player player, int foodBonus = 0)
        {
            if (player == null) return BaseSTM;

            var agilityMod = GetAbilityModifier(player, AbilityType.Agility);
            return BaseSTM + (agilityMod * 5) + foodBonus;
        }

        /// <summary>
        /// Calculates max HP using the formula: 70 + FLOOR(((Vitality - 10) / 2)) * 40 + MaxHP
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="foodBonus">Optional food bonus (default 0)</param>
        /// <returns>The calculated max HP value</returns>
        public static int CalculateMaxHP(Player player, int foodBonus = 0)
        {
            if (player == null) return BaseHP;
            
            var vitalityStat = GetAbilityScore(player, AbilityType.Vitality);
            var vitalityModifier = CalculateAbilityModifier(vitalityStat);
            var vitalityBonus = vitalityModifier * 40;
            
            return BaseHP + vitalityBonus + foodBonus;
        }

        /// <summary>
        /// Gets the highest combat skill level from OneHanded, TwoHanded, Ranged, and Force
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <returns>The highest combat skill level</returns>
        public static int GetHighestCombatSkillLevel(Player player)
        {
            if (player?.Skills == null) return 0;

            var combatSkills = new[] { SkillType.OneHanded, SkillType.TwoHanded, SkillType.Ranged, SkillType.Force };
            return combatSkills.Max(skill => GetSkillLevel(player, skill));
        }

        /// <summary>
        /// Gets the highest combat stat from Might, Perception, and Willpower
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <returns>The highest combat stat</returns>
        public static int GetHighestCombatStat(Player player)
        {
            if (player?.BaseStats == null) return 0;

            var combatStats = new[] { AbilityType.Might, AbilityType.Perception, AbilityType.Willpower };
            return combatStats.Max(stat => GetAbilityScore(player, stat));
        }

        /// <summary>
        /// Calculates attack using the formula: 8 + (2 × Skill Level) + Stat + Equipment Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="skillType">The skill type to use for calculation</param>
        /// <param name="abilityType">The ability type to use for calculation</param>
        /// <param name="equipmentBonus">The equipment bonus</param>
        /// <returns>The calculated attack value</returns>
        public static int CalculateAttack(Player player, SkillType skillType, AbilityType abilityType, int equipmentBonus)
        {
            if (player == null) return 8;

            var skillLevel = GetSkillLevel(player, skillType);
            var stat = GetAbilityScore(player, abilityType);
            return 8 + (2 * skillLevel) + stat + equipmentBonus;
        }

        /// <summary>
        /// Calculates defense using the formula: 8 + (Vitality Stat × 1.5) + Armor Skill + Equipment Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="equipmentBonus">The equipment bonus</param>
        /// <returns>The calculated defense value</returns>
        public static int CalculateDefense(Player player, int equipmentBonus)
        {
            if (player == null) return 8;

            var vitalityStat = GetAbilityScore(player, AbilityType.Vitality);
            var armorSkillLevel = GetSkillLevel(player, SkillType.Armor);
            return 8 + (int)(vitalityStat * 1.5) + armorSkillLevel + equipmentBonus;
        }

        /// <summary>
        /// Calculates evasion using the formula: (Agility Stat × 3) + Armor Skill + Equipment Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="equipmentBonus">The equipment bonus</param>
        /// <returns>The calculated evasion value</returns>
        public static int CalculateEvasion(Player player, int equipmentBonus)
        {
            if (player == null) return 0;

            var agilityStat = GetAbilityScore(player, AbilityType.Agility);
            var armorSkillLevel = GetSkillLevel(player, SkillType.Armor);
            return (agilityStat * 3) + armorSkillLevel + equipmentBonus;
        }

        /// <summary>
        /// Gets the total defense bonus from all equipment
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <returns>The total defense bonus</returns>
        public static int GetTotalDefenseBonus(Player player)
        {
            if (player?.Defenses == null) return 0;
            return player.Defenses.Values.Sum();
        }

        /// <summary>
        /// Calculates accuracy using the formula: 8 + (2 × Skill Level) + Stat + Equipment Bonus
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="skillType">The skill type to use for calculation</param>
        /// <param name="abilityType">The ability type to use for calculation</param>
        /// <param name="equipmentBonus">The equipment bonus</param>
        /// <returns>The calculated accuracy value</returns>
        public static int CalculateAccuracy(Player player, SkillType skillType, AbilityType abilityType, int equipmentBonus)
        {
            if (player == null) return 8;

            var skillLevel = GetSkillLevel(player, skillType);
            var stat = GetAbilityScore(player, abilityType);
            return 8 + (2 * skillLevel) + stat + equipmentBonus;
        }

        /// <summary>
        /// Calculates base saving throw using the formula: 8 + (Stat Modifier × 2) + Level
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <param name="abilityType">The ability type to use for calculation</param>
        /// <param name="level">The character level</param>
        /// <returns>The calculated saving throw value</returns>
        public static int CalculateBaseSavingThrow(Player player, AbilityType abilityType, int level)
        {
            if (player == null) return 8;

            var statMod = GetAbilityModifier(player, abilityType);
            return 8 + (statMod * 2) + level;
        }

        /// <summary>
        /// Gets a comprehensive stat breakdown for display purposes
        /// </summary>
        /// <param name="player">The player entity</param>
        /// <returns>A dictionary containing all calculated stats</returns>
        public static Dictionary<string, object> GetComprehensiveStatBreakdown(Player player)
        {
            if (player == null) return new Dictionary<string, object>();

            var breakdown = new Dictionary<string, object>();

            // HP calculation
            breakdown["MaxHP"] = new
            {
                Base = BaseHP,
                Current = player.MaxHP,
                Formula = "Uses native NWN GetMaxHitPoints() function"
            };

            // FP calculation
            var willpowerMod = GetAbilityModifier(player, AbilityType.Willpower);
            var fpBonus = willpowerMod * 10;
            var calculatedFP = BaseFP + fpBonus;
            breakdown["MaxFP"] = new
            {
                Base = BaseFP,
                WillpowerModifier = willpowerMod,
                FPBonus = fpBonus,
                Calculated = calculatedFP,
                Stored = player.MaxFP,
                Formula = "BaseFP + (Willpower Modifier × 10) + Food Bonus"
            };

            // Stamina calculation
            var agilityMod = GetAbilityModifier(player, AbilityType.Agility);
            var stmBonus = agilityMod * 5;
            var calculatedSTM = BaseSTM + stmBonus;
            breakdown["MaxStamina"] = new
            {
                Base = BaseSTM,
                AgilityModifier = agilityMod,
                StaminaBonus = stmBonus,
                Calculated = calculatedSTM,
                Stored = player.MaxStamina,
                Formula = "BaseSTM + (Agility Modifier × 5) + Food Bonus"
            };

            // Attack calculation
            var attackBase = 8;
            var attackSkillLevel = GetHighestCombatSkillLevel(player);
            var attackStat = GetHighestCombatStat(player);
            var calculatedAttack = attackBase + (2 * attackSkillLevel) + attackStat + player.Attack;
            breakdown["Attack"] = new
            {
                Base = attackBase,
                SkillLevel = attackSkillLevel,
                StatBonus = attackStat,
                EquipmentBonus = player.Attack,
                Calculated = calculatedAttack,
                Stored = player.Attack,
                Formula = "8 + (2 × Skill Level) + Stat + Equipment Bonus"
            };

            // Defense calculation
            var defenseBase = 8;
            var defenseStat = GetAbilityScore(player, AbilityType.Vitality);
            var defenseSkillLevel = GetSkillLevel(player, SkillType.Armor);
            var defenseBonus = GetTotalDefenseBonus(player);
            var calculatedDefense = defenseBase + (int)(defenseStat * 1.5) + defenseSkillLevel + defenseBonus;
            breakdown["Defense"] = new
            {
                Base = defenseBase,
                VitalityStat = defenseStat,
                ArmorSkillLevel = defenseSkillLevel,
                EquipmentBonus = defenseBonus,
                Calculated = calculatedDefense,
                Formula = "8 + (Vitality Stat × 1.5) + Armor Skill + Equipment Bonus"
            };

            // Evasion calculation
            var evasionStat = GetAbilityScore(player, AbilityType.Agility);
            var evasionSkillLevel = GetSkillLevel(player, SkillType.Armor);
            var calculatedEvasion = (evasionStat * 3) + evasionSkillLevel + player.Evasion;
            breakdown["Evasion"] = new
            {
                AgilityStat = evasionStat,
                ArmorSkillLevel = evasionSkillLevel,
                EquipmentBonus = player.Evasion,
                Calculated = calculatedEvasion,
                Stored = player.Evasion,
                Formula = "(Agility Stat × 3) + Armor Skill + Equipment Bonus"
            };

            return breakdown;
        }
    }
} 