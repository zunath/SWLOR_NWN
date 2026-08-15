using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using MovementRate = SWLOR.NWN.API.NWScript.Enum.MovementRate;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Service
{
    public class Stat
    {
        public const int BaseHP = 70;
        public const int BaseFP = 10;
        public const int BaseSTM = 10;
        private const int FPPerWillpower = 3;
        private const int StaminaPerTwoMight = 3;
        private const float DefenseSkillMultiplier = 1.2f;
        private const float DefaultPlayerMovementSpeedIncrease = 0.25f;
        private const float DefaultCompanionMovementSpeedIncrease = 0.25f;
        private const float DefaultNPCMovementSpeedIncrease = 0.30f;
        private const int MaximumNPCHitPoints = 30000;
        private const int MaximumNPCHitPointAlignmentPasses = 4;
        public const int DefaultMeleeDeflectionChanceCap = 50;
        public const int DefaultRangedDeflectionChanceCap = 50;
        public const int MaximumDeflectionChanceCap = 100;
        public const int MaximumShieldDeflectionChance = 75;
        public const int MaximumGuardChance = 100;
        public const int MaximumCombatReadinessPercent = 15;
        public const int MaximumNPCDetection = 50;
        public const float MinimumMovementSpeedMultiplier = 0f;
        public const float MaximumMovementSpeedMultiplier = 1.5f;
        private const float DeflectionEvasionBoostDurationSeconds = 30f;
        private const float DeflectionEnmityBoostDurationSeconds = 30f;
        private const float DeflectionDefenseBoostDurationSeconds = 30f;
        private static readonly Dictionary<StatType, StatTypeAttribute> _statTypeAttributes = new();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheStatTypeAttributes();
        }

        public static StatTypeCategory GetStatTypeCategory(StatType statType)
        {
            EnsureStatTypeAttributesCached();

            return _statTypeAttributes.TryGetValue(statType, out var attribute)
                ? attribute.Category
                : StatTypeCategory.NonBeneficial;
        }

        public static StatTypeAggregation GetStatTypeAggregation(StatType statType)
        {
            EnsureStatTypeAttributesCached();

            return _statTypeAttributes.TryGetValue(statType, out var attribute)
                ? attribute.Aggregation
                : StatTypeAggregation.Additive;
        }

        public static DeflectionSource GetStatTypeDeflectionSource(StatType statType)
        {
            EnsureStatTypeAttributesCached();

            return _statTypeAttributes.TryGetValue(statType, out var attribute)
                ? attribute.DeflectionSource
                : DeflectionSource.None;
        }

        public static StatType GetGrantedDeflectionStatType(StatType sourceStatType)
        {
            return GetStatTypeDeflectionSource(sourceStatType) switch
            {
                DeflectionSource.Melee => StatType.MeleeDeflection,
                DeflectionSource.Ranged => StatType.RangedDeflection,
                DeflectionSource.Shield => StatType.ShieldDeflection,
                _ => StatType.Invalid
            };
        }

        public static int AggregateStatAdjustment(StatType statType, int current, int adjustment)
        {
            return GetStatTypeAggregation(statType) switch
            {
                StatTypeAggregation.BitwiseOr => current | adjustment,
                StatTypeAggregation.Maximum => Math.Max(current, adjustment),
                _ => current + adjustment
            };
        }

        public static bool IsBeneficialStatAdjustment(StatType statType, int value)
        {
            if (value == 0)
                return false;

            return GetStatTypeCategory(statType) switch
            {
                StatTypeCategory.BeneficialWhenPositive => value > 0,
                StatTypeCategory.BeneficialWhenNegative => value < 0,
                _ => false
            };
        }

        private static void CacheStatTypeAttributes()
        {
            _statTypeAttributes.Clear();

            foreach (var statType in Enum.GetValues(typeof(StatType)).Cast<StatType>())
            {
                _statTypeAttributes[statType] = statType.GetAttribute<StatType, StatTypeAttribute>();
            }

            Console.WriteLine($"Loaded {_statTypeAttributes.Count} stat type metadata entries.");
        }

        private static void EnsureStatTypeAttributesCached()
        {
            if (_statTypeAttributes.Count <= 0)
            {
                CacheStatTypeAttributes();
            }
        }

        public static int ScaleEffect(
            int baseAmount,
            int primaryStat,
            float primaryRate = 0.01f,
            int secondaryStat = 0,
            float secondaryRate = 0.005f)
        {
            if (baseAmount <= 0)
                return baseAmount;

            var scale = Math.Max(0f, primaryStat * primaryRate) + Math.Max(0f, secondaryStat * secondaryRate);
            var bonus = (int)Math.Ceiling(baseAmount * scale);

            return baseAmount + bonus;
        }

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyPlayerStats()
        {
            ApplyTemporaryPlayerStats();
        }

        /// <summary>
        /// When a player enters the server, apply any temporary stats which do not persist.
        /// </summary>
        private static void ApplyTemporaryPlayerStats()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            ApplyCreatureMovementRate(player);
        }

        /// <summary>
        /// Retrieves the maximum FP on a creature.
        /// For players:
        /// Each point of Willpower grants +3 to max FP.
        /// For NPCs:
        /// FP is read from their skin.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of FP</returns>
        public static int GetMaxFP(uint creature, Player dbPlayer = null)
        {
            var willpower = GetAbilityScore(creature, AbilityType.Willpower);
            var bonus = GetStatAdjustment(creature, StatType.MaxFP);
            int baseFP;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }
                baseFP = dbPlayer.MaxFP;

            }
            // NPCs
            else
            {
                var npcStats = GetNPCStats(creature);
                baseFP = npcStats.FP;
            }

            return GetMaxFP(baseFP, willpower, bonus);
        }

        public static int GetMaxFP(int baseFP, int willpower, int bonus)
        {
            return baseFP + willpower * FPPerWillpower + bonus;
        }

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        public static int GetCurrentFP(uint creature, Player dbPlayer = null)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }

                return dbPlayer.FP;
            }
            // NPCs
            else
            {
                return GetLocalInt(creature, "FP");
            }
        }

        public static int GetAdjustedRequiredFP(uint creature, int requiredFP)
        {
            if (requiredFP <= 0)
                return 0;

            var percentAdjustment = GetStatAdjustment(creature, StatType.FPCostPercentAdjustment);
            var flatAdjustment = GetStatAdjustment(creature, StatType.FPCostFlatAdjustment);
            var adjustedCost = (int)Math.Ceiling(requiredFP * (1 + percentAdjustment / 100f));

            adjustedCost += flatAdjustment;

            return Math.Max(0, adjustedCost);
        }

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// Each point of Might grants +1.5 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        public static int GetMaxStamina(uint creature, Player dbPlayer = null)
        {
            var might = GetAbilityScore(creature, AbilityType.Might);
            var bonus = GetStatAdjustment(creature, StatType.MaxStamina);
            int baseStamina;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }

                baseStamina = dbPlayer.MaxStamina;

            }
            // NPCs
            else
            {
                var npcStats = GetNPCStats(creature);
                baseStamina = npcStats.Stamina;
            }

            return GetMaxStamina(baseStamina, might, bonus);
        }

        public static int GetMaxStamina(int baseStamina, int might, int bonus)
        {
            return baseStamina + might * StaminaPerTwoMight / 2 + bonus;
        }

        /// <summary>
        /// Retrieves the current STM on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve STM from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of STM.</returns>
        public static int GetCurrentStamina(uint creature, Player dbPlayer = null)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }

                return dbPlayer.Stamina;
            }
            // NPCs
            else
            {
                return GetLocalInt(creature, "STAMINA");
            }
        }

        /// <summary>
        /// Restores a creature's FP by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of FP to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The amount of FP actually restored after modifiers and the maximum-FP cap.</returns>
        public static int RestoreFP(uint creature, int amount, Player dbPlayer = null)
        {
            if (amount <= 0) return 0;

            amount = ApplyFPRestoreAdjustment(creature, amount);
            if (amount <= 0) return 0;

            var maxFP = GetMaxFP(creature);
            var restored = 0;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }

                var current = dbPlayer.FP;
                dbPlayer.FP = Math.Min(maxFP, current + amount);
                restored = Math.Max(0, dbPlayer.FP - current);

                DB.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var current = GetLocalInt(creature, "FP");
                var fp = Math.Min(maxFP, current + amount);
                restored = Math.Max(0, fp - current);

                SetLocalInt(creature, "FP", fp);
            }

            ExecuteScript("pc_fp_adjusted", creature);
            if (restored > 0)
                Combat.ApplyFPRestoredEffects(creature);

            return restored;
        }

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        public static void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }

                dbPlayer.FP -= reduceBy;

                if (dbPlayer.FP < 0)
                    dbPlayer.FP = 0;

                DB.Set(dbPlayer);
            }
            else
            {
                var fp = GetLocalInt(creature, "FP");
                fp -= reduceBy;
                if (fp < 0)
                    fp = 0;

                SetLocalInt(creature, "FP", fp);
            }

            ExecuteScript("pc_fp_adjusted", creature);
        }

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        /// <returns>The amount of Stamina actually restored after the maximum-Stamina cap.</returns>
        public static int RestoreStamina(uint creature, int amount, Player dbPlayer = null)
        {
            if (amount <= 0) return 0;

            var maxSTM = GetMaxStamina(creature);
            var restored = 0;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }

                var current = dbPlayer.Stamina;
                dbPlayer.Stamina = Math.Min(maxSTM, current + amount);
                restored = Math.Max(0, dbPlayer.Stamina - current);

                DB.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var current = GetLocalInt(creature, "STAMINA");
                var fp = Math.Min(maxSTM, current + amount);
                restored = Math.Max(0, fp - current);

                SetLocalInt(creature, "STAMINA", fp);
            }

            ExecuteScript("pc_stm_adjusted", creature);
            if (restored > 0)
                Combat.ApplyStaminaRestoredEffects(creature);

            return restored;
        }

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        public static void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }

                dbPlayer.Stamina -= reduceBy;

                if (dbPlayer.Stamina < 0)
                    dbPlayer.Stamina = 0;

                DB.Set(dbPlayer);
            }
            else
            {
                var stamina = GetLocalInt(creature, "STAMINA");
                stamina -= reduceBy;
                if (stamina < 0)
                    stamina = 0;

                SetLocalInt(creature, "STAMINA", stamina);
            }

            ExecuteScript("pc_stm_adjusted", creature);
        }

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateStateEffect)]
        public static void ReapplyFoodHP()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ReapplyFoodHP(player);
        }

        public static void ReapplyFoodHP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Player returned after the server restarted. They no longer have the food status effect.
            // Reduce their HP by the amount tracked in the DB.
            if (dbPlayer.TemporaryFoodHP > 0 && StatusEffect.GetStatusEffect<FoodStatusEffect>(player) == null)
            {
                AdjustPlayerMaxHP(dbPlayer, player, -dbPlayer.TemporaryFoodHP);
                dbPlayer.TemporaryFoodHP = 0;
                DB.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 40 by default, so this
        /// gives 255 * 40 = 10,200 HP maximum. If the player's HP would go over this amount, it will be set to 10,200.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        public static void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy)
        {
            const int MaxHPPerLevel = 254;
            entity.MaxHP += adjustBy;
            var nwnLevelCount = GetLevelByPosition(1, player) +
                                GetLevelByPosition(2, player) +
                                GetLevelByPosition(3, player);

            var hpToApply = entity.MaxHP;

            // All levels must have at least 1 HP, so apply those right now.
            for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
            {
                hpToApply--;
                CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 1);
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
                        CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 255);
                        hpToApply -= 254;
                    }
                    else // Remaining value gets set to the level. (<255 hp)
                    {
                        CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, hpToApply + 1);
                        break;
                    }
                }
            }

            // If player's current HP is higher than max, deal the difference in damage to bring them back down to their new maximum.
            var currentHP = GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            if (currentHP > maxHP)
            {
                SetCurrentHitPoints(player, maxHP);
            }
        }

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max FP to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxFP += adjustBy;

            // Note - must call GetMaxFP here to account for ability-based increase to FP cap.
            if (entity.FP > GetMaxFP(player))
                entity.FP = GetMaxFP(player);

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
        public static void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max STM to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxStamina += adjustBy;

            // Note - must call GetMaxFP here to account for ability-based increase to STM cap.
            if (entity.Stamina > GetMaxStamina(player))
                entity.Stamina = GetMaxStamina(player);

            // Current STM, however, should never drop below zero.
            if (entity.Stamina < 0)
                entity.Stamina = 0;
        }

        public static void ApplyCreatureMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            if (isPlayer)
            {
                CreaturePlugin.SetMovementRate(creature, MovementRate.PC);
            }

            var movementRate = GetMovementSpeedMultiplier(creature);
            CreaturePlugin.SetMovementRateFactor(creature, movementRate);
        }

        public static float GetMovementSpeedMultiplier(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return 1.0f;

            if (GetStatAdjustment(creature, StatType.MovementSpeedDisabled) > 0)
                return MinimumMovementSpeedMultiplier;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            var movementRate = 1.0f + GetBaseMovementSpeedIncrease(creature, isPlayer);
            movementRate += GetStatAdjustment(creature, StatType.MovementSpeedPercentAdjustment) * 0.01f;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                float amount;
                if (type == EffectTypeScript.MovementSpeedIncrease)
                {
                    amount = GetEffectInteger(effect, 0) - 100;
                    movementRate += amount * 0.01f;
                }
                else if (type == EffectTypeScript.MovementSpeedDecrease)
                {
                    amount = GetEffectInteger(effect, 0);
                    movementRate -= amount * 0.01f;
                }
            }

            return Math.Clamp(movementRate, MinimumMovementSpeedMultiplier, MaximumMovementSpeedMultiplier);
        }

        private static float GetBaseMovementSpeedIncrease(uint creature, bool isPlayer)
        {
            if (isPlayer)
                return DefaultPlayerMovementSpeedIncrease;

            if (Droid.IsDroid(creature) || BeastMastery.IsPlayerBeast(creature))
                return DefaultCompanionMovementSpeedIncrease;

            return DefaultNPCMovementSpeedIncrease;
        }

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        public static void ApplyPlayerStat(Player entity, uint player, AbilityType ability)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (ability == AbilityType.Invalid) return;

            var racialBonus = entity.RacialStat == ability ? 1 : 0;
            var totalStat = entity.BaseStats[ability] + entity.UpgradedStats[ability] + racialBonus;
            CreaturePlugin.SetRawAbilityScore(player, ability, totalStat);
        }

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCombatReadiness(Player entity, int adjustBy)
        {
            entity.CombatReadiness += adjustBy;
        }

        public static int GetCombatReadinessPercent(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return 0;

            var combatReadiness = GetStatAdjustment(creature, StatType.CombatReadinessPercent);

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);
                combatReadiness += dbPlayer?.CombatReadiness ?? 0;
            }
            else
            {
                combatReadiness += GetNPCStats(creature).CombatReadiness;
            }

            return Math.Clamp(combatReadiness, 0, MaximumCombatReadinessPercent);
        }

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustHPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for HP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.HPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustFPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for FP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.FPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustSTMRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for STM Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.STMRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustDefense(Player entity, CombatDamageType type, int adjustBy)
        {
            if (!type.IsDefenseDamageType())
                return;

            if (!entity.Defenses.ContainsKey(type))
                entity.Defenses[type] = 0;

            entity.Defenses[type] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustEvasion(Player entity, int adjustBy)
        {
            entity.Evasion += adjustBy;
        }

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustStealth(Player entity, int adjustBy)
        {
            entity.Stealth += adjustBy;
        }

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustDetection(Player entity, int adjustBy)
        {
            entity.Detection += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustTrapBonus(Player entity, int adjustBy)
        {
            entity.TrapBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustTrapDisarm(Player entity, int adjustBy)
        {
            entity.TrapDisarm += adjustBy;
        }

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPoisonBonus(Player entity, int adjustBy)
        {
            entity.PoisonBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustLockpicking(Player entity, int adjustBy)
        {
            entity.Lockpicking += adjustBy;
        }

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustAttack(Player entity, int adjustBy)
        {
            entity.Attack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustForceAttack(Player entity, int adjustBy)
        {
            entity.ForceAttack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustControl(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Control.ContainsKey(skillType))
                entity.Control[skillType] = 0;

            entity.Control[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Craftsmanship.ContainsKey(skillType))
                entity.Craftsmanship[skillType] = 0;

            entity.Craftsmanship[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.CPBonus.ContainsKey(skillType))
                entity.CPBonus[skillType] = 0;

            entity.CPBonus[skillType] += adjustBy;
        }

        private static int CalculateEffectAttack(uint creature, int attack)
        {
            return attack + GetStatAdjustment(creature, StatType.Attack);
        }

        /// <summary>
        /// Calculates the attack for a given creature.
        /// </summary>
        /// <param name="creature">The creature to calculate.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="skillType">The type of skill to use.</param>
        /// <param name="attackBonusOverride">Overrides the attack bonus granted by equipment. Usually only used by Space combat.</param>
        /// <returns>The total Attack value of a creature.</returns>
        public static int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0)
        {
            if (attackBonusOverride < 0)
                attackBonusOverride = 0;

            var attackBonus = 0 + attackBonusOverride;
            var skillLevel = 0;
            var stat = GetAbilityScore(creature, abilityType);

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (skillType != SkillType.Invalid)
                    skillLevel = dbPlayer.Skills[skillType].Rank;

                if (attackBonusOverride <= 0)
                {
                    if (skillType == SkillType.Force)
                        attackBonus += dbPlayer.ForceAttack;
                    else
                        attackBonus += dbPlayer.Attack;
                }
            }
            else
            {
                // If a skill value is assigned for this item type, use it.
                // Otherwise fallback to the NPC's level.
                var npcStats = GetNPCStats(creature);

                skillLevel = npcStats.Skills.ContainsKey(skillType)
                    ? npcStats.Skills[skillType]
                    : npcStats.Level;

                if (attackBonusOverride <= 0)
                {
                    if (skillType == SkillType.Force)
                        attackBonus += npcStats.ForceAttack;
                    else
                        attackBonus += npcStats.Attack;
                }
            }

            attackBonus = CalculateEffectAttack(creature, attackBonus);

            var attack = GetAttack(skillLevel, stat, attackBonus);
            return ApplyPostAttackStatusModifiers(creature, attack, skillType);
        }

        public static int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid, bool useForceAttack = false)
        {
            var attackBonus = 0;
            var skillLevel = 0;
            var statType = statOverride != AbilityType.Invalid
                ? statOverride
                : Combat.GetWeaponDamageAbilityType(creature.m_idSelf, itemType);
            var stat = GetStatValueNative(creature, statType);
            var skillType = Skill.GetSkillTypeByBaseItem(itemType);

            // Force-typed attacks (e.g. Imbuement Stance retyping a weapon swing to Force) use the
            // wearer's Force Attack in place of physical Attack so the attack side matches the Force
            // Defense the hit is mitigated against. The weapon's own skill rank still governs the roll.
            var usesForceAttack = useForceAttack || skillType == SkillType.Force;

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if(skillType != SkillType.Invalid)
                        skillLevel = dbPlayer.Skills[skillType].Rank;

                    if (usesForceAttack)
                        attackBonus += dbPlayer.ForceAttack;
                    else
                        attackBonus += dbPlayer.Attack;
                }
            }
            else
            {
                // If a skill value is assigned for this item type, use it.
                // Otherwise fallback to the NPC's level.
                var npcStats = GetNPCStatsNative(creature);

                skillLevel = npcStats.Skills.ContainsKey(skillType)
                    ? npcStats.Skills[skillType]
                    : npcStats.Level;

                if (usesForceAttack)
                    attackBonus += npcStats.ForceAttack;
                else
                    attackBonus += npcStats.Attack;
            }

            attackBonus = CalculateEffectAttack(creature.m_idSelf, attackBonus);

            var attack = GetAttack(skillLevel, stat, attackBonus);
            return ApplyPostAttackStatusModifiers(creature.m_idSelf, attack, usesForceAttack ? SkillType.Force : skillType);
        }

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        public static int GetAttack(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// Physical and Force equipment bonuses live here; elemental/status mitigation lives in resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <param name="defenseBonusOverride">Overrides the defense bonus granted by equipment. Usually only used for Space combat.</param>
        /// <returns>The defense value toward a given damage type.</returns>
        public static int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0)
        {
            if (defenseBonusOverride < 0)
                defenseBonusOverride = 0;

            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetAbilityScore(creature, abilityType);
            int skillLevel;
            var defenseBonus = 0;
            var equipmentDefense = defenseBonusOverride;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                if (defenseBonusOverride <= 0 &&
                    dbPlayer.Defenses != null &&
                    dbPlayer.Defenses.TryGetValue(defenseType, out var playerDefense))
                {
                    equipmentDefense += playerDefense;
                }
            }
            else
            {
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
                if (defenseBonusOverride <= 0 &&
                    npcStats.Defenses.TryGetValue(defenseType, out var npcDefense))
                {
                    equipmentDefense += npcDefense;
                }
            }

            defenseBonus = CalculateEffectDefense(creature, defenseBonus, defenseType);
            defenseBonus += equipmentDefense;
            var defense = CalculateDefense(defenderStat, skillLevel, defenseBonus);
            return ApplyPostDefenseStatusModifiers(creature, defenseType, defense);
        }

        public static int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus)
        {
            return (int)(8 + (skillLevel * DefenseSkillMultiplier) + defenderStat + defenseBonus);
        }

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        public static int GetStatValueNative(CNWSCreature creature, AbilityType statType)
        {
            var stat = 0;
            switch (statType)
            {
                case AbilityType.Might:
                    stat = creature.m_pStats.GetSTRStat();
                    break;
                case AbilityType.Perception:
                    stat = creature.m_pStats.GetDEXStat();
                    break;
                case AbilityType.Vitality:
                    stat = creature.m_pStats.GetCONStat();
                    break;
                case AbilityType.Willpower:
                    stat = creature.m_pStats.GetWISStat();
                    break;
                case AbilityType.Agility:
                    stat = creature.m_pStats.GetINTStat();
                    break;
                case AbilityType.Social:
                    stat = creature.m_pStats.GetCHAStat();
                    break;
                default:
                    stat = 0;
                    break;
            }

            // Check for negative modifiers.  A modifier of -2 is represented as 254.
            if (stat > 128) stat -= 256;

            return stat;
        }

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        public static int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType)
        {
            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetStatValueNative(creature, abilityType);
            var skillLevel = 0;
            var defenseBonus = 0;
            var equipmentDefense = 0;

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                    if (dbPlayer.Defenses != null &&
                        dbPlayer.Defenses.TryGetValue(defenseType, out var playerDefense))
                    {
                        equipmentDefense += playerDefense;
                    }
                }
            }
            else
            {
                var npcStats = GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                if (npcStats.Defenses.TryGetValue(defenseType, out var npcDefense))
                {
                    equipmentDefense += npcDefense;
                }
            }

            defenseBonus = CalculateEffectDefense(creature.m_idSelf, defenseBonus, defenseType);
            defenseBonus += equipmentDefense;
            var defense = CalculateDefense(defenderStat, skillLevel, defenseBonus);
            return ApplyPostDefenseStatusModifiers(creature.m_idSelf, defenseType, defense);
        }

        private static int ApplyPostAttackStatusModifiers(uint creature, int attack, SkillType skillType)
        {
            var adjustment = GetStatAdjustment(creature, StatType.AttackPercentAdjustment);
            if (skillType == SkillType.Force)
            {
                adjustment += GetStatAdjustment(creature, StatType.ForceAttackPercentAdjustment);
            }

            adjustment += GetHighFPAndStaminaAttackAdjustment(creature);
            adjustment += Combat.GetNearbyStatusTargetAttackAdjustment(creature);
            adjustment += Combat.GetLowHPAttackAdjustment(creature);
            adjustment += Combat.GetLowFPAttackAdjustment(creature);
            return Math.Max(1, ApplyPercentAdjustment(attack, adjustment));
        }

        private static int GetHighFPAndStaminaAttackAdjustment(uint creature)
        {
            var threshold = GetStatAdjustment(creature, StatType.HighFPAndStaminaAttackThresholdPercent);
            var adjustment = GetStatAdjustment(creature, StatType.HighFPAndStaminaAttackPercentAdjustment);

            if (threshold <= 0 || adjustment == 0)
                return 0;

            var currentFP = GetCurrentFP(creature);
            var maxFP = GetMaxFP(creature);
            var currentStamina = GetCurrentStamina(creature);
            var maxStamina = GetMaxStamina(creature);

            if (maxFP <= 0 || maxStamina <= 0)
                return 0;

            return currentFP >= maxFP * (threshold / 100f) &&
                   currentStamina >= maxStamina * (threshold / 100f)
                ? adjustment
                : 0;
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <param name="statOverride">The stat override used to calculate accuracy. This stat will be used instead of whatever stat is defined for the weapon type.</param>
        /// <param name="skillOverride">The skill override used to calculate accuracy. This skill will be used instead of whatever skill is defined for the weapon type.</param>
        /// <param name="skillLevelOverride">Overrides the skill rank or NPC level used in the accuracy calculation.</param>
        /// <param name="ignoreWeaponAccuracyStatOverride">When true, Accuracy Stat item properties do not replace <paramref name="statOverride"/>.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public static int GetAccuracy(
            uint creature,
            uint weapon,
            AbilityType statOverride,
            SkillType skillOverride,
            int skillLevelOverride = -1,
            bool ignoreWeaponAccuracyStatOverride = false)
        {
            var accuracyBonus = 0;

            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                if (type != ItemPropertyType.AccuracyBonus &&
                    type != ItemPropertyType.EnhancementBonus &&
                    type != ItemPropertyType.AccuracyStat)
                    continue;

                var value = type == ItemPropertyType.AccuracyStat
                    ? GetItemPropertySubType(ip)
                    : GetItemPropertyCostTableValue(ip);
                (statOverride, accuracyBonus) = ApplyAccuracyItemProperty(
                    statOverride,
                    accuracyBonus,
                    type,
                    value,
                    ignoreWeaponAccuracyStatOverride);
            }


            var baseItemType = GetBaseItemType(weapon);
            var statType = statOverride == AbilityType.Invalid ?
                Combat.GetWeaponAccuracyAbilityType(creature, baseItemType) :
                statOverride;
            var stat = statType == AbilityType.Invalid ? 0 : GetAbilityScore(creature, statType);
            var skillType = skillOverride == SkillType.Invalid ? Skill.GetSkillTypeByBaseItem(baseItemType) : skillOverride;
            var skillLevel = 0;


            // Creature skill level / NPC level
            if (skillLevelOverride >= 0)
            {
                skillLevel = skillLevelOverride;
            }
            else if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (skillType != SkillType.Invalid)
                    skillLevel = dbPlayer.Skills[skillType].Rank;
            }
            else
            {
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
            }

            // Accuracy increases granted by effects
            accuracyBonus = CalculateEffectAccuracy(creature, accuracyBonus);

            // Power Attack to-hit penalty
            if (GetActionMode(creature, ActionMode.PowerAttack))
                accuracyBonus -= 5;
            else if (GetActionMode(creature, ActionMode.ImprovedPowerAttack))
                accuracyBonus -= 10;

            var accuracy = GetAccuracy(skillLevel, stat, accuracyBonus);
            return ApplyPostAccuracyStatusModifiers(creature, accuracy);
        }

        private static (AbilityType StatOverride, int AccuracyBonus) ApplyAccuracyItemProperty(
            AbilityType statOverride,
            int accuracyBonus,
            ItemPropertyType type,
            int value,
            bool ignoreWeaponAccuracyStatOverride)
        {
            if (type == ItemPropertyType.AccuracyBonus || type == ItemPropertyType.EnhancementBonus)
                return (statOverride, accuracyBonus + value);

            if (type == ItemPropertyType.AccuracyStat && !ignoreWeaponAccuracyStatOverride)
                return ((AbilityType)value, accuracyBonus);

            return (statOverride, accuracyBonus);
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public static int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon)
        {
            var accuracyBonus = 0;
            var statOverride = AbilityType.Invalid;

            if (weapon != null)
            {
                foreach (var ip in weapon.m_lstPassiveProperties)
                {
                    // Attack Bonus / Enhancement Bonus found on the weapon.
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.AccuracyBonus ||
                        ip.m_nPropertyName == (ushort)ItemPropertyType.EnhancementBonus)
                    {
                        accuracyBonus += ip.m_nCostTableValue;
                    }
                    // Accuracy Stat Override - Always "wins" even if another override was passed in.
                    else if (ip.m_nPropertyName == (ushort)ItemPropertyType.AccuracyStat)
                    {
                        statOverride = (AbilityType)ip.m_nSubType;
                    }
                }
            }

            var baseItemType = weapon == null ? BaseItem.Invalid : (BaseItem)weapon.m_nBaseItem;
            var statType = statOverride == AbilityType.Invalid ?
                Combat.GetWeaponAccuracyAbilityType(creature.m_idSelf, baseItemType) :
                statOverride;
            var skillType = Skill.GetSkillTypeByBaseItem(baseItemType);
            var stat = GetStatValueNative(creature, statType);
            var skillLevel = 0;


            // Creature skill level / NPC level
            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null && skillType != SkillType.Invalid)
                {
                    skillLevel = dbPlayer.Skills[skillType].Rank;
                }
            }
            else
            {
                var npcStats = GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
            }

            accuracyBonus = CalculateEffectAccuracyNative(creature, accuracyBonus);

            var accuracy = GetAccuracy(skillLevel, stat, accuracyBonus);
            return ApplyPostAccuracyStatusModifiers(creature.m_idSelf, accuracy);
        }

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        public static int GetAccuracy(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        private static int CalculateEffectAccuracy(uint creature, int accuracy)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (type == EffectTypeScript.AttackIncrease)
                {
                    accuracy += 5 * GetEffectInteger(effect, 0);
                }
                else if (type == EffectTypeScript.AttackDecrease)
                {
                    accuracy -= 5 * GetEffectInteger(effect, 0);
                }
            }

            accuracy += GetStatAdjustment(creature, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private static int CalculateEffectAccuracyNative(CNWSCreature creature, int accuracy)
        {
            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_nType == (ushort)EffectTrueType.AttackIncrease)
                {
                    accuracy += 5 * effect.GetInteger(0);
                }
                else if (effect.m_nType == (ushort)EffectTrueType.AttackDecrease)
                {
                    accuracy -= 5 * effect.GetInteger(0);
                }
            }

            accuracy += GetStatAdjustment(creature.m_idSelf, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Native Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private static int CalculateEffectEvasion(uint creature)
        {
            return GetStatAdjustment(creature, StatType.Evasion);
        }

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <param name="incomingSkillType">The skill type of the incoming attack for conditional evasion modifiers.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetAbilityScore(creature, AbilityType.Agility);
            int skillLevel;
            int evasionBonus;

            // Base NWN applies an AC bonus based on the DEX stat. The Perception stat is based upon this.
            // Perception should not increase AC in SWLOR, so this is subtracted from the AC.
            var dexOffset = GetAbilityModifier(AbilityType.Perception, creature);
            var ac = GetAC(creature) - dexOffset - 10; // Offset by natural 10 AC granted to all characters.
            var skillType = skillOverride == SkillType.Invalid ? SkillType.Armor : skillOverride;

            Log.Write(LogGroup.Attack, $"Evasion regular AC = {ac}");

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                skillLevel = dbPlayer.Skills[skillType].Rank;
                evasionBonus = dbPlayer.Evasion;
            }
            else
            {
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
                evasionBonus = npcStats.Evasion;
            }

            evasionBonus += CalculateEffectEvasion(creature);

            Log.Write(LogGroup.Attack, $"Effect Evasion: {evasionBonus}");

            var evasion = GetEvasion(skillLevel, stat, ac * 5 + evasionBonus);
            return ApplyPostEvasionStatusModifiers(creature, evasion, incomingSkillType);
        }

        /// <summary>
        /// Retrieves a creature's detection rating, used against Stealth in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The detection rating of a creature.</returns>
        public static int GetDetection(uint creature)
        {
            var perception = GetAbilityScore(creature, AbilityType.Perception);
            var willpower = GetAbilityScore(creature, AbilityType.Willpower);
            var equipmentBonus = 0;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                equipmentBonus = dbPlayer.Detection;
            }
            else
            {
                equipmentBonus = GetNPCSkinStat(creature, ItemPropertyType.Detection);
            }

            var detection = CalculateDetectionRating(
                perception,
                willpower,
                equipmentBonus,
                GetStatAdjustment(creature, StatType.Detection),
                GetActionMode(creature, ActionMode.Detect));

            return ApplyNPCDetectionCap(
                detection,
                !GetIsPC(creature) &&
                !GetIsDM(creature) &&
                !GetIsDMPossessed(creature));
        }

        public static int CalculateDetectionRating(
            int perception,
            int willpower,
            int equipmentBonus,
            int adjustment,
            bool detectMode)
        {
            var detectModeBonus = detectMode ? 5 : 0;
            return Math.Max(0, perception + willpower + equipmentBonus + adjustment + detectModeBonus);
        }

        public static int ApplyNPCDetectionCap(int detection, bool isNPC)
        {
            detection = Math.Max(0, detection);
            return isNPC
                ? Math.Min(MaximumNPCDetection, detection)
                : detection;
        }

        /// <summary>
        /// Retrieves a creature's stealth rating, used against Detection in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The stealth rating of a creature.</returns>
        public static int GetStealth(uint creature)
        {
            var agility = GetAbilityScore(creature, AbilityType.Agility);
            var equipmentBonus = 0;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                equipmentBonus = dbPlayer.Stealth;
            }
            else
            {
                equipmentBonus = GetNPCSkinStat(creature, ItemPropertyType.Stealth);
            }

            return CalculateStealthRating(
                agility,
                equipmentBonus,
                GetStatAdjustment(creature, StatType.Stealth));
        }

        public static int CalculateStealthRating(int agility, int equipmentBonus, int adjustment)
        {
            return Math.Max(0, agility * 2 + equipmentBonus + adjustment);
        }

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetStatValueNative(creature, AbilityType.Agility);
            var skillLevel = 0;
            var evasionBonus = 0;

            // Note: The DEX offset is unnecessary for the native call.
            var ac = creature.m_pStats.m_nACArmorBase +
                     creature.m_pStats.m_nACNaturalBase +
                     creature.m_pStats.m_nACArmorMod -
                     creature.m_pStats.m_nACArmorNeg +
                     creature.m_pStats.m_nACDeflectionMod -
                     creature.m_pStats.m_nACDeflectionNeg +
                     creature.m_pStats.m_nACDodgeMod -
                     creature.m_pStats.m_nACDodgeNeg +
                     creature.m_pStats.m_nACNaturalMod -
                     creature.m_pStats.m_nACNaturalNeg +
                     creature.m_pStats.m_nACShieldMod -
                     creature.m_pStats.m_nACShieldNeg;

            Log.Write(LogGroup.Attack, $"Native Evasion AC = {ac}");

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                    evasionBonus = dbPlayer.Evasion;
                }
            }
            else
            {
                var npcStats = GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                evasionBonus = npcStats.Evasion;
            }

            evasionBonus += CalculateEffectEvasion(creature.m_idSelf);

            var evasion = GetEvasion(skillLevel, stat, ac * 5 + evasionBonus);
            return ApplyPostEvasionStatusModifiers(creature.m_idSelf, evasion, incomingSkillType);
        }

        public static int GetMeleeDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = GetStatAdjustment(creature.m_idSelf, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature.m_idSelf));
        }

        public static int GetMeleeDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = GetStatAdjustment(creature, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature));
        }

        public static int GetRangedDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = GetStatAdjustment(creature.m_idSelf, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature.m_idSelf));
        }

        public static int GetRangedDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = GetStatAdjustment(creature, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature));
        }

        public static int GetShieldDeflectionChanceNative(CNWSCreature creature)
        {
            var shield = GetEquippedShieldNative(creature);
            if (shield == null)
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonusNative(shield) +
                         GetStatAdjustment(creature.m_idSelf, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public static int GetShieldDeflectionChance(uint creature)
        {
            var shield = GetEquippedShield(creature);
            if (!GetIsObjectValid(shield))
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonus(shield) +
                         GetStatAdjustment(creature, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public static int GetGuardChance(uint creature)
        {
            return Math.Clamp(GetStatAdjustment(creature, StatType.Guard), 0, MaximumGuardChance);
        }

        public static void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source)
        {
            var creatureId = creature.m_idSelf;
            Combat.TrackDeflection(creatureId, source);

            var staminaRestoreStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionStaminaRestore,
                StatType.ShieldDeflectionStaminaRestore);
            var staminaRestoreCooldownStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionStaminaRestoreCooldownSeconds,
                StatType.ShieldDeflectionStaminaRestoreCooldownSeconds);
            var staminaRestore = staminaRestoreStat != StatType.Invalid
                ? GetStatAdjustment(creatureId, staminaRestoreStat)
                : 0;
            var fpRestoreStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionFPRestore,
                StatType.DeflectionFPRestore);
            var fpRestore = fpRestoreStat != StatType.Invalid
                ? GetStatAdjustment(creatureId, fpRestoreStat)
                : 0;
            var fpRestoreCooldownStat = GetDeflectionStatTypeForSource(
                source,
                StatType.MeleeDeflectionFPRestoreCooldownSeconds,
                StatType.DeflectionFPRestoreCooldownSeconds);
            var staminaRestorePercent = GetStatAdjustment(creatureId, StatType.DeflectionStaminaRestorePercent);
            var staminaRestoreCooldown = staminaRestoreCooldownStat != StatType.Invalid
                ? GetStatAdjustment(creatureId, staminaRestoreCooldownStat)
                : 0;
            var fpRestoreCooldown = fpRestoreCooldownStat != StatType.Invalid
                ? GetStatAdjustment(creatureId, fpRestoreCooldownStat)
                : 0;
            var evasionBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEvasionPercentAdjustment, source);
            var evasionEnmityBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEvasionEnmityPercentAdjustment, source);
            var enmityBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionEnmityPercentAdjustment, source);
            var defenseBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionDefensePercentAdjustment, source);
            var forceDefenseBoost = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionForceDefensePercentAdjustment, source);
            var recastReductionGroup = GetRecastGroupFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionRecastReductionGroupId,
                source));
            var recastReductionSeconds = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionRecastReductionSeconds, source);
            var nextSkillAbilitySkillType = GetSkillTypeFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionNextSkillAbilitySkillType,
                source));
            var nextSkillAbilityDamageBonus = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityDamageBonus, source);
            var nextSkillAbilityCriticalRate = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityCriticalRatePercentAdjustment, source);
            var nextSkillAbilityNoDelay = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityNoDelay, source);
            var nextSkillAbilityDamageWindow = GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityDamageBonusWindowSeconds);
            var nextSkillAbilityCriticalWindow = GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityCriticalRateWindowSeconds);
            var nextSkillAbilityNoDelayWindow = GetStatAdjustment(creatureId, StatType.DeflectionNextSkillAbilityNoDelayWindowSeconds);
            var nextAutoAttackCriticalRateSkillType = GetSkillTypeFromStat(GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionNextAutoAttackCriticalRateSkillType,
                source));
            var nextAutoAttackCriticalRate = GetDeflectionStatAdjustment(creatureId, StatType.DeflectionNextAutoAttackCriticalRatePercentAdjustment, source);
            var nextAutoAttackCriticalRateWindow = GetStatAdjustment(creatureId, StatType.DeflectionNextAutoAttackCriticalRateWindowSeconds);

            if (staminaRestore > 0 &&
                Combat.TryUseStatTrigger(creatureId, staminaRestoreStat, staminaRestoreCooldown))
            {
                RestoreStamina(creatureId, staminaRestore);
            }

            if (fpRestore > 0 &&
                Combat.TryUseStatTrigger(creatureId, fpRestoreStat, fpRestoreCooldown))
            {
                RestoreFP(creatureId, fpRestore);
            }

            if (staminaRestorePercent > 0)
            {
                var amount = GameMath.PercentOf(GetMaxStamina(creatureId), staminaRestorePercent);
                RestoreStamina(creatureId, amount);
            }

            if (evasionBoost != 0 || evasionEnmityBoost != 0)
            {
                TemporaryStatModifier.Replace(
                    creatureId,
                    StatType.EvasionPercentAdjustment,
                    evasionBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
                TemporaryStatModifier.Replace(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    evasionEnmityBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
            }

            if (enmityBoost != 0)
            {
                TemporaryStatModifier.Replace(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    enmityBoost,
                    DeflectionEnmityBoostDurationSeconds,
                    StatType.DeflectionEnmityPercentAdjustment);
            }

            if (defenseBoost != 0 || forceDefenseBoost != 0)
            {
                TemporaryStatModifier.Replace(
                    creatureId,
                    StatType.PhysicalDefensePercentAdjustment,
                    defenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
                TemporaryStatModifier.Replace(
                    creatureId,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
            }

            if (recastReductionGroup != RecastGroup.Invalid && recastReductionSeconds > 0)
            {
                Recast.ReduceRecastDelay(creatureId, recastReductionGroup, recastReductionSeconds);
            }

            Combat.GrantNextSkillAbilityBonuses(
                creatureId,
                nextSkillAbilitySkillType,
                nextSkillAbilityDamageBonus,
                nextSkillAbilityCriticalRate,
                Math.Max(nextSkillAbilityDamageWindow, nextSkillAbilityCriticalWindow));

            Combat.GrantNextAutoAttackCriticalRateBonus(
                creatureId,
                nextAutoAttackCriticalRateSkillType,
                nextAutoAttackCriticalRate,
                nextAutoAttackCriticalRateWindow);

            if (nextSkillAbilityNoDelay > 0)
            {
                Combat.GrantNextAbilityNoDelay(
                    creatureId,
                    nextSkillAbilitySkillType,
                    nextSkillAbilityNoDelayWindow);
            }
        }

        private static RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        private static int GetMeleeDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.MeleeDeflectionChanceCap,
                DefaultMeleeDeflectionChanceCap);
        }

        private static int GetRangedDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.RangedDeflectionChanceCap,
                DefaultRangedDeflectionChanceCap);
        }

        private static int GetDeflectionChanceCap(uint creature, StatType capStat, int defaultCap)
        {
            var cap = defaultCap + GetStatAdjustment(creature, capStat);
            return Math.Clamp(cap, defaultCap, MaximumDeflectionChanceCap);
        }

        private static int GetDeflectionStatAdjustment(uint creature, StatType statType, DeflectionSource source)
        {
            return GetStatTypeDeflectionSource(statType) == source
                ? GetStatAdjustment(creature, statType)
                : 0;
        }

        private static StatType GetDeflectionStatTypeForSource(
            DeflectionSource source,
            StatType first,
            StatType second)
        {
            if (GetStatTypeDeflectionSource(first) == source)
                return first;

            return GetStatTypeDeflectionSource(second) == source
                ? second
                : StatType.Invalid;
        }

        private static bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature)
        {
            return HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.LeftHand);
        }

        private static bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature, EquipmentSlot slot)
        {
            var item = creature.m_pInventory.GetItemInSlot((uint)slot);
            return item != null &&
                   Skill.GetSkillTypeByBaseItem((BaseItem)item.m_nBaseItem) != SkillType.Invalid;
        }

        private static bool HasWeaponEquippedForWeaponDeflection(uint creature)
        {
            return HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.LeftHand);
        }

        private static bool HasWeaponEquippedForWeaponDeflection(uint creature, InventorySlot slot)
        {
            var item = GetItemInSlot(slot, creature);
            return GetIsObjectValid(item) &&
                   Skill.GetSkillTypeByBaseItem(GetBaseItemType(item)) != SkillType.Invalid;
        }

        private static SkillType GetMainHandSkillTypeNative(CNWSCreature creature)
        {
            var rightHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
            if (rightHandItem == null)
                return SkillType.Invalid;

            return Skill.GetSkillTypeByBaseItem((BaseItem)rightHandItem.m_nBaseItem);
        }

        private static SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        private static bool HasShieldEquippedNative(CNWSCreature creature)
        {
            return GetEquippedShieldNative(creature) != null;
        }

        private static bool HasShieldEquipped(uint creature)
        {
            return GetIsObjectValid(GetEquippedShield(creature));
        }

        private static CNWSItem GetEquippedShieldNative(CNWSCreature creature)
        {
            var leftHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            return Item.IsBaseItemType(leftHandItem, Item.ShieldBaseItemTypes)
                ? leftHandItem
                : null;
        }

        private static uint GetEquippedShield(uint creature)
        {
            var leftHandItem = GetItemInSlot(InventorySlot.LeftHand, creature);
            return Item.IsBaseItemType(leftHandItem, Item.ShieldBaseItemTypes)
                ? leftHandItem
                : OBJECT_INVALID;
        }

        private static int GetShieldDeflectionItemPropertyBonusNative(CNWSItem shield)
        {
            var bonus = 0;
            for (var index = 0; index < shield.m_lstPassiveProperties.Count; index++)
            {
                var ip = shield.GetPassiveProperty(index);
                if (ip?.m_nPropertyName == (ushort)ItemPropertyType.ShieldDeflection)
                    bonus += ip.m_nCostTableValue;
            }

            return bonus;
        }

        private static int GetShieldDeflectionItemPropertyBonus(uint shield)
        {
            var bonus = 0;
            for (var ip = GetFirstItemProperty(shield); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(shield))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.ShieldDeflection)
                    bonus += GetItemPropertyCostTableValue(ip);
            }

            return bonus;
        }

        private static int GetNPCSkinStat(uint creature, ItemPropertyType type)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
            var value = 0;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == type)
                    value += GetItemPropertyCostTableValue(ip);
            }

            return value;
        }

        private static int ApplyPostAccuracyStatusModifiers(uint creature, int accuracy)
        {
            var adjustment = GetStatAdjustment(creature, StatType.AccuracyPercentAdjustment);
            return Math.Max(1, ApplyPercentAdjustment(accuracy, adjustment));
        }

        private static int ApplyPostEvasionStatusModifiers(uint creature, int evasion, SkillType incomingSkillType)
        {
            var adjustment = GetStatAdjustment(creature, StatType.EvasionPercentAdjustment);
            if (Combat.IsRangedDamageSkill(incomingSkillType))
            {
                adjustment += GetStatAdjustment(creature, StatType.RangedEvasionPercentAdjustment);
            }

            return Math.Max(1, ApplyPercentAdjustment(evasion, adjustment));
        }

        /// <summary>
        /// Calculates defense bonuses granted by status effects, perks, and temporary stat modifiers.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="defense">The base bonus to adjust.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>A modified defense bonus.</returns>
        private static int CalculateEffectDefense(uint creature, int defense, CombatDamageType type)
        {
            return defense + GetDefenseAdjustment(creature, type);
        }

        private static int ApplyPostDefenseStatusModifiers(uint creature, CombatDamageType type, int defense)
        {
            var adjustment = GetDefensePercentAdjustment(creature, type);
            return Math.Max(1, ApplyPercentAdjustment(defense, adjustment));
        }

        /// <summary>
        /// Retrieves the total percentage adjustment applied to a creature's defense for a damage type.
        /// This combines the general defense adjustment with the type-specific adjustment, including
        /// the shield-only bonus when a shield is equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>The percentage adjustment applied to defense.</returns>
        public static int GetDefensePercentAdjustment(uint creature, CombatDamageType type)
        {
            return GetStatAdjustment(creature, StatType.DefensePercentAdjustment) + (type switch
            {
                CombatDamageType.Physical => GetStatAdjustment(creature, StatType.PhysicalDefensePercentAdjustment) +
                                             GetShieldEquippedPhysicalDefensePercentAdjustment(creature),
                CombatDamageType.Force => GetStatAdjustment(creature, StatType.ForceDefensePercentAdjustment),
                _ => 0
            });
        }

        private static int GetShieldEquippedPhysicalDefensePercentAdjustment(uint creature)
        {
            return HasShieldEquipped(creature)
                ? GetStatAdjustment(creature, StatType.ShieldEquippedPhysicalDefensePercentAdjustment)
                : 0;
        }

        private static int GetDefenseAdjustment(uint creature, CombatDamageType type)
        {
            return GetStatAdjustment(creature, StatType.Defense) + (type switch
            {
                CombatDamageType.Physical => GetStatAdjustment(creature, StatType.PhysicalDefense),
                CombatDamageType.Force => GetStatAdjustment(creature, StatType.ForceDefense),
                _ => 0
            });
        }

        public static int GetStatAdjustment(uint creature, StatType stat)
        {
            var persistentAdjustment = GetStatAdjustmentExcludingTemporaryModifiers(creature, stat);
            var temporaryAdjustment = TemporaryStatModifier.GetStatAdjustment(creature, stat);

            return AggregateStatAdjustment(stat, persistentAdjustment, temporaryAdjustment);
        }

        public static int GetStatAdjustmentExcludingTemporaryModifiers(uint creature, StatType stat)
        {
            var statusAdjustment = StatusEffect.GetCreatureStatusEffects(creature).StatGroup.Stats[stat];
            var perkAdjustment = Perk.GetStatBonus(creature, stat);
            var mimicryTraitAdjustment = Mimicry.GetStatBonus(creature, stat);

            return AggregateStatAdjustment(
                stat,
                AggregateStatAdjustment(stat, statusAdjustment, perkAdjustment),
                mimicryTraitAdjustment);
        }

        public static int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount)
        {
            if (amount <= 0 || !GetIsObjectValid(source))
                return amount;

            var statSource = BeastMastery.IsPlayerBeast(source)
                ? GetMaster(source)
                : source;
            var adjustment = GetStatAdjustment(
                statSource,
                StatType.OutgoingAbilityHealingPercentAdjustment);

            return CalculateOutgoingAbilityHealingAmount(amount, adjustment);
        }

        public static int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment)
        {
            if (amount <= 0 || adjustment <= 0)
                return amount;

            return amount + (int)Math.Ceiling(amount * (adjustment / 100f));
        }

        public static int ApplyHealingReceivedAdjustment(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var adjustment = GetStatAdjustment(creature, StatType.HealingReceivedPercentAdjustment);
            var adjustedAmount = Math.Max(1, ApplyPercentAdjustment(amount, adjustment));

            ApplyHealingReceivedStaminaRestore(creature);
            ApplyHealingReceivedAttackBoost(creature);

            return adjustedAmount;
        }

        private static void ApplyHealingReceivedStaminaRestore(uint creature)
        {
            var chance = GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChance);
            var stamina = GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestore);
            if (chance <= 0 || stamina <= 0)
                return;

            var maximumChance = GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChanceMaximum);
            var scalingAbility = GetAbilityTypeFromStatValue(GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreChanceScalingAbility));
            if (maximumChance > chance && scalingAbility != AbilityType.Invalid)
            {
                chance = Math.Min(maximumChance, chance + Math.Max(0, GetAbilityScore(creature, scalingAbility)));
            }

            if (Random.D100(1) <= chance)
            {
                RestoreStamina(creature, stamina);
            }
        }

        private static void ApplyHealingReceivedAttackBoost(uint creature)
        {
            var attackPercent = GetStatAdjustment(creature, StatType.HealingReceivedAttackPercentAdjustment);
            var duration = GetStatAdjustment(creature, StatType.HealingReceivedAttackDurationSeconds);
            if (attackPercent == 0 || duration <= 0)
                return;

            TemporaryStatModifier.Replace(
                creature,
                StatType.AttackPercentAdjustment,
                attackPercent,
                duration,
                StatType.HealingReceivedAttackPercentAdjustment);
        }

        private static AbilityType GetAbilityTypeFromStatValue(int value)
        {
            var abilityValue = value - 1;
            return Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        private static int ApplyFPRestoreAdjustment(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var adjustment = GetStatAdjustment(creature, StatType.FPRestorePercentAdjustment);
            return Math.Max(0, ApplyPercentAdjustment(amount, adjustment));
        }

        private static int ApplyPercentAdjustment(int value, int percentAdjustment)
        {
            if (percentAdjustment == 0)
                return value;

            var delta = (int)Math.Ceiling(value * (Math.Abs(percentAdjustment) / 100f));
            return percentAdjustment > 0
                ? value + delta
                : value - delta;
        }

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        public static int GetEvasion(int level, int stat, int bonus)
        {
            return 8 + (2 * level) + stat + bonus;
        }

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        public static NPCStats GetNPCStats(uint npc)
        {
            var npcStats = new NPCStats();

            var skin = GetItemInSlot(InventorySlot.CreatureArmor, npc);
            if (!GetIsObjectValid(skin))
                return npcStats;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.NPCLevel)
                {
                    npcStats.Level = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.Defense)
                {
                    var damageType = (CombatDamageType)GetItemPropertySubType(ip);
                    if (!npcStats.Defenses.ContainsKey(damageType))
                        npcStats.Defenses[damageType] = 0;

                    npcStats.Defenses[damageType] += GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.Resistance)
                {
                    var resistanceType = (ResistanceType)GetItemPropertySubType(ip);
                    if (!npcStats.Resistances.ContainsKey(resistanceType))
                        npcStats.Resistances[resistanceType] = 0;

                    npcStats.Resistances[resistanceType] += Resistance.DecodeItemPropertyCostTableValue(
                        GetItemPropertyCostTableValue(ip));
                }
                else if (type == ItemPropertyType.NPCSkill)
                {
                    var skillType = (SkillType)GetItemPropertySubType(ip);
                    npcStats.Skills[skillType] = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.Attack)
                {
                    npcStats.Attack = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.ForceAttack)
                {
                    npcStats.ForceAttack = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.Evasion)
                {
                    npcStats.Evasion = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.CombatReadiness)
                {
                    npcStats.CombatReadiness = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.Stamina)
                {
                    npcStats.Stamina = GetItemPropertyCostTableValue(ip);
                }
                else if (type == ItemPropertyType.FP)
                {
                    npcStats.FP = GetItemPropertyCostTableValue(ip);
                }
            }

            return npcStats;
        }

        public static NPCStats GetNPCStatsNative(CNWSCreature npc)
        {
            var npcStats = new NPCStats();
            var skin = npc.m_pInventory.GetItemInSlot((uint)EquipmentSlot.CreatureArmour);
            if (skin != null)
            {
                foreach (var prop in skin.m_lstPassiveProperties)
                {
                    if (prop.m_nPropertyName == (ushort)ItemPropertyType.NPCLevel)
                    {
                        npcStats.Level = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Defense)
                    {
                        var damageType = (CombatDamageType)prop.m_nSubType;

                        if (!npcStats.Defenses.ContainsKey(damageType))
                            npcStats.Defenses[damageType] = 0;

                        npcStats.Defenses[damageType] += prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Resistance)
                    {
                        var resistanceType = (ResistanceType)prop.m_nSubType;

                        if (!npcStats.Resistances.ContainsKey(resistanceType))
                            npcStats.Resistances[resistanceType] = 0;

                        npcStats.Resistances[resistanceType] += Resistance.DecodeItemPropertyCostTableValue(
                            prop.m_nCostTableValue);
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.NPCSkill)
                    {
                        var skillType = (SkillType)prop.m_nSubType;

                        npcStats.Skills[skillType] = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Attack)
                    {
                        npcStats.Attack = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.ForceAttack)
                    {
                        npcStats.ForceAttack = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Evasion)
                    {
                        npcStats.Evasion = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Stamina)
                    {
                        npcStats.Stamina = prop.m_nCostTableValue;
                    }
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.FP)
                    {
                        npcStats.FP = prop.m_nCostTableValue;
                    }
                }
            }

            return npcStats;
        }

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        public static string GetAbilityNameShort(AbilityType type)
        {
            switch (type)
            {
                default:
                case AbilityType.Invalid:
                    return "INV";
                case AbilityType.Might:
                    return "MGT";
                case AbilityType.Perception:
                    return "PER";
                case AbilityType.Vitality:
                    return "VIT";
                case AbilityType.Agility:
                    return "AGI";
                case AbilityType.Willpower:
                    return "WIL";
                case AbilityType.Social:
                    return "SOC";
            }
        }

        /// <summary>
        /// Calculates the total Control for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total control for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        public static int CalculateControl(uint player, SkillType craftingSkillType)
        {
            var skillDetail = Skill.GetSkillDetails(craftingSkillType);
            if (!skillDetail.IsShownInCraftMenu)
                throw new ArgumentException($"Unable to calculate Control because {craftingSkillType} is not a crafting skill.");

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var control = dbPlayer.Control.ContainsKey(craftingSkillType)
                ? dbPlayer.Control[craftingSkillType]
                : 0;

            var statusBonus = StatusEffect.GetCreatureStatusEffects(player)
                .StatGroup
                .CraftSkillBonuses[CraftSkillBonusType.Control][craftingSkillType];

            return control + statusBonus;
        }
        /// <summary>
        /// Calculates the total Craftsmanship for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total Craftsmanship for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        public static int CalculateCraftsmanship(uint player, SkillType craftingSkillType)
        {
            var skillDetail = Skill.GetSkillDetails(craftingSkillType);
            if (!skillDetail.IsShownInCraftMenu)
                throw new ArgumentException($"Unable to calculate Craftsmanship because {craftingSkillType} is not a crafting skill.");

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var control = dbPlayer.Craftsmanship.ContainsKey(craftingSkillType)
                ? dbPlayer.Craftsmanship[craftingSkillType]
                : 0;

            var statusBonus = StatusEffect.GetCreatureStatusEffects(player)
                .StatGroup
                .CraftSkillBonuses[CraftSkillBonusType.Craftsmanship][craftingSkillType];

            return control + statusBonus;
        }

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public static void LoadNPCStats()
        {
            LoadNPCStats(OBJECT_SELF);
        }

        public static void LoadNPCStats(uint self)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, self);

            var maxHP = 0;
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCHP)
                {
                    maxHP += GetItemPropertyCostTableValue(ip);
                }
            }

            if (maxHP > MaximumNPCHitPoints)
                maxHP = MaximumNPCHitPoints;

            if (maxHP > 0)
            {
                SetNPCMaxHitPoints(self, maxHP, true);
            }

            SetLocalInt(self, "FP", GetMaxFP(self));
            SetLocalInt(self, "STAMINA", GetMaxStamina(self));
        }

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        public static void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false)
        {
            desiredMaxHitPoints = System.Math.Clamp(desiredMaxHitPoints, 1, MaximumNPCHitPoints);
            var originalCurrentHitPoints = GetCurrentHitPoints(creature);

            // Probe with the final budget, observe the engine-derived adjustment, then
            // compensate the base value. Repeating also handles NWN's one-HP-per-level
            // floor for creatures with a negative Constitution modifier.
            var baseHitPoints = desiredMaxHitPoints;
            for (var pass = 0; pass < MaximumNPCHitPointAlignmentPasses; pass++)
            {
                ObjectPlugin.SetMaxHitPoints(creature, baseHitPoints);
                var actualMaxHitPoints = GetMaxHitPoints(creature);
                if (actualMaxHitPoints == desiredMaxHitPoints)
                    break;

                baseHitPoints = System.Math.Clamp(
                    baseHitPoints + desiredMaxHitPoints - actualMaxHitPoints,
                    1,
                    short.MaxValue);
            }

            var alignedMaxHitPoints = GetMaxHitPoints(creature);
            if (alignedMaxHitPoints != desiredMaxHitPoints)
            {
                Log.Write(
                    LogGroup.Error,
                    $"Unable to align NPC HP budget for {GetResRef(creature)}. " +
                    $"Expected {desiredMaxHitPoints}, received {alignedMaxHitPoints}.");
            }

            if (restoreToFull)
            {
                ObjectPlugin.SetCurrentHitPoints(creature, alignedMaxHitPoints);
            }
            else
            {
                ObjectPlugin.SetCurrentHitPoints(
                    creature,
                    System.Math.Min(originalCurrentHitPoints, alignedMaxHitPoints));
            }
        }

        /// <summary>
        /// Set to 1 on an NPC to disable natural regeneration: the out-of-combat
        /// 10%-per-tick HP heal and the 1-per-tick FP/STM restore. Engine-test fixtures
        /// wound casters to observe an ability's own healing and verify EXACT resource
        /// costs; a natural regen tick inside the assertion window would otherwise satisfy
        /// a healing assertion for a broken impact, or drift a pool off the exact
        /// post-deduction value.
        /// </summary>
        public const string SuppressNaturalRegenVariable = "ENGINE_TEST_SUPPRESS_NATURAL_REGEN";

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        public static void RestoreNPCStats(bool outOfCombatRegen)
        {
            var self = OBJECT_SELF;
            if (GetLocalInt(self, SuppressNaturalRegenVariable) != 0)
                return;

            var maxFP = GetMaxFP(self);
            var maxSTM = GetMaxStamina(self);
            var fp = GetLocalInt(self, "FP") + 1;
            var stm = GetLocalInt(self, "STAMINA") + 1;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, "FP", fp);
            SetLocalInt(self, "STAMINA", stm);

            if (outOfCombatRegen)
            {
                // If out of combat - restore HP at 10% per tick.
                if (!GetIsInCombat(self) &&
                    !GetIsObjectValid(Enmity.GetHighestEnmityTarget(self)) &&
                    GetCurrentHitPoints(self) < GetMaxHitPoints(self))
                {
                    var hpToHeal = GetMaxHitPoints(self) * 0.1f;
                    ApplyEffectToObject(DurationType.Instant, EffectHeal((int)hpToHeal), self);
                }
            }
        }
    }
}
