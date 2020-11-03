using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class Stat
    {
        /// <summary>
        /// Retrieves the maximum hit points on a player.
        /// This will include any base NWN calculations used when determining max HP.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <returns>The max amount of HP</returns>
        public static int GetMaxHP(uint player)
        {
            return GetMaxHitPoints(player);
        }

        /// <summary>
        /// Retrieves the maximum FP on a player.
        /// INT and WIS modifiers will be checked. The higher one is used for calculations.
        /// Each modifier grants +2 to max FP.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made.</param>
        /// <returns>The max amount of FP</returns>
        public static int GetMaxFP(uint player, Player dbPlayer = null)
        {
            if (dbPlayer == null)
            {
                var playerId = GetObjectUUID(player);
                dbPlayer = DB.Get<Player>(playerId);
            }
            var baseFP = dbPlayer.MaxFP;
            var intModifier = GetAbilityModifier(AbilityType.Intelligence, player);
            var wisModifier = GetAbilityModifier(AbilityType.Wisdom, player);
            var modifier = intModifier > wisModifier ? intModifier : wisModifier;

            return baseFP + (modifier * 2);
        }

        /// <summary>
        /// Retrieves the maximum STM on a player.
        /// CON modifier will be checked. Each modifier grants +2 to max STM.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made.</param>
        /// <returns>The max amount of STM</returns>
        public static int GetMaxStamina(uint player, Player dbPlayer = null)
        {
            if (dbPlayer == null)
            {
                var playerId = GetObjectUUID(player);
                dbPlayer = DB.Get<Player>(playerId);
            }

            var baseStamina = dbPlayer.MaxStamina;
            var conModifier = GetAbilityModifier(AbilityType.Constitution, player);

            return baseStamina + (conModifier * 2);
        }

        /// <summary>
        /// Restores an entity's FP by a specified amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="amount">The amount of FP to restore.</param>
        public static void RestoreFP(uint player, Player entity, int amount)
        {
            if (amount <= 0) return;

            var maxMP = GetMaxFP(player);
            entity.FP += amount;

            if (entity.FP > maxMP)
                entity.FP = maxMP;
        }

        /// <summary>
        /// Reduces an entity's FP by a specified amount.
        /// If player would fall below 0 FP, they will be reduced to 0 instead.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        public static void ReduceMP(Player entity, int reduceBy)
        {
            if (reduceBy <= 0) return;

            entity.FP -= reduceBy;

            if (entity.FP < 0)
                entity.FP = 0;
        }

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        public static void RestoreStamina(uint player, Player entity, int amount)
        {
            if (amount <= 0) return;

            var maxStamina = GetMaxStamina(player, entity);
            entity.Stamina += amount;

            if (entity.Stamina > maxStamina)
                entity.Stamina = maxStamina;
        }

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If player would fall below 0 stamina, they will be reduced to 0 instead.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        public static void ReduceStamina(Player entity, int reduceBy)
        {
            if (reduceBy <= 0) return;

            entity.Stamina -= reduceBy;

            if (entity.Stamina < 0)
                entity.Stamina = 0;
        }

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 5 by default, so this
        /// gives 255 * 5 = 1275 HP maximum. If the player's HP would go over this amount, it will be set to 1275.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        public static void AdjustMaxHP(Player entity, uint player, int adjustBy)
        {
            const int MaxHPPerLevel = 255;
            entity.MaxHP += adjustBy;
            var nwnLevelCount = GetLevelByPosition(1, player) +
                                GetLevelByPosition(2, player) +
                                GetLevelByPosition(3, player);

            var hpToApply = entity.MaxHP;

            // All levels must have at least 1 HP, so apply those right now.
            for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
            {
                hpToApply--;
                Creature.SetMaxHitPointsByLevel(player, nwnLevel, 1);
            }

            // It's possible for the MaxHP value to be a negative if builders misuse item properties, etc.
            // Players cannot go under 'nwnLevel' HP, so we apply that first. If our HP to apply is zero, we don't want to
            // do any more logic with HP application.
            if (hpToApply > 0)
            {
                // Apply the remaining HP.
                for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
                {
                    if (hpToApply > MaxHPPerLevel) // Levels can only contain a max of 255 HP
                    {
                        Creature.SetMaxHitPointsByLevel(player, nwnLevel, 255);
                        hpToApply -= 254;
                    }
                    else // Remaining value gets set to the level. (<255 hp)
                    {
                        Creature.SetMaxHitPointsByLevel(player, nwnLevel, hpToApply + 1);
                        break;
                    }
                }
            }

            // If player's current HP is higher than max, deal the difference in damage to bring them back down to their new maximum.
            var currentHP = GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            if (currentHP > maxHP)
            {
                var damage = EffectDamage(currentHP - maxHP);
                ApplyEffectToObject(DurationType.Instant, damage, player);
            }
        }

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustMaxMP(Player entity, int adjustBy)
        {
            // Note: It's possible for Max FP to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxFP += adjustBy;

            if (entity.FP > entity.MaxFP)
                entity.FP = entity.MaxFP;

            // Current FP, however, should never drop below zero.
            if (entity.FP < 0)
                entity.FP = 0;
        }

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustMaxSTM(Player entity, int adjustBy)
        {
            // Note: It's possible for Max STM to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxStamina += adjustBy;

            if (entity.Stamina > entity.MaxStamina)
                entity.Stamina = entity.MaxStamina;

            // Current STM, however, should never drop below zero.
            if (entity.Stamina < 0)
                entity.Stamina = 0;
        }

        /// <summary>
        /// Modifies a player's Base Attack Bonus (BAB) by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="player">The player to modify.</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustBAB(Player entity, uint player, int adjustBy)
        {
            entity.BAB += adjustBy;

            if (entity.BAB < 1)
                entity.BAB = 1;

            Creature.SetBaseAttackBonus(player, entity.BAB);
        }

        /// <summary>
        /// Modifies a player's attribute by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify.</param>
        /// <param name="player">The player to modify.</param>
        /// <param name="ability">The ability to modify.</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        public static void AdjustAttribute(Player entity, uint player, AbilityType ability, float adjustBy)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (ability == AbilityType.Invalid) return;
            if (adjustBy == 0f) return;

            entity.AdjustedStats[ability] += adjustBy;

            var totalStat = (int)(entity.BaseStats[ability] + entity.AdjustedStats[ability]);
            Creature.SetRawAbilityScore(player, ability, totalStat);
        }

        /// <summary>
        /// This will manually recalculate all stats.
        /// This should be used sparingly because it can be a heavy call.
        /// This method will not persist the changes so be sure your call DB.Set after calling this.
        /// </summary>
        public static void RecalculateAllStats(uint player, Player dbPlayer)
        {
            // Reset all adjusted stat values.
            foreach (var adjustedStat in dbPlayer.AdjustedStats)
            {
                dbPlayer.AdjustedStats[adjustedStat.Key] = 0.0f;
            }

            // Apply adjusted stat increases based on the player's skill ranks.
            // We use a pre-filtered list of skills for this to cut down on the number of iterations.
            var skills = Skill.GetAllSkillsWhichIncreaseStats();
            foreach (var (type, value) in skills)
            {
                var primaryIncrease = dbPlayer.Skills[type].Rank * Skill.PrimaryStatIncrease;
                var secondaryIncrease = dbPlayer.Skills[type].Rank * Skill.SecondaryStatIncrease;

                if (value.PrimaryStat == AbilityType.Strength)
                    dbPlayer.AdjustedStats[AbilityType.Strength] += primaryIncrease;
                if (value.PrimaryStat == AbilityType.Dexterity)
                    dbPlayer.AdjustedStats[AbilityType.Dexterity] += primaryIncrease;
                if (value.PrimaryStat == AbilityType.Constitution)
                    dbPlayer.AdjustedStats[AbilityType.Constitution] += primaryIncrease;
                if (value.PrimaryStat == AbilityType.Wisdom)
                    dbPlayer.AdjustedStats[AbilityType.Wisdom] += primaryIncrease;
                if (value.PrimaryStat == AbilityType.Intelligence)
                    dbPlayer.AdjustedStats[AbilityType.Intelligence] += primaryIncrease;
                if (value.PrimaryStat == AbilityType.Charisma)
                    dbPlayer.AdjustedStats[AbilityType.Charisma] += primaryIncrease;

                if (value.SecondaryStat == AbilityType.Strength)
                    dbPlayer.AdjustedStats[AbilityType.Strength] += secondaryIncrease;
                if (value.SecondaryStat == AbilityType.Dexterity)
                    dbPlayer.AdjustedStats[AbilityType.Dexterity] += secondaryIncrease;
                if (value.SecondaryStat == AbilityType.Constitution)
                    dbPlayer.AdjustedStats[AbilityType.Constitution] += secondaryIncrease;
                if (value.SecondaryStat == AbilityType.Wisdom)
                    dbPlayer.AdjustedStats[AbilityType.Wisdom] += secondaryIncrease;
                if (value.SecondaryStat == AbilityType.Intelligence)
                    dbPlayer.AdjustedStats[AbilityType.Intelligence] += secondaryIncrease;
                if (value.SecondaryStat == AbilityType.Charisma)
                    dbPlayer.AdjustedStats[AbilityType.Charisma] += secondaryIncrease;
            }

            // We now have all of the correct values. Apply them to the player object.
            foreach (var (ability, amount) in dbPlayer.AdjustedStats)
            {
                var totalStat = (int)(dbPlayer.BaseStats[ability] + amount);
                Creature.SetRawAbilityScore(player, ability, totalStat);
            }
        }
    }
}
