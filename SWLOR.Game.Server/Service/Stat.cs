using System;
using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Feature.StatusEffectDefinition.StatusEffectData;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.Game.Server.Core.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.Game.Server.Core.NWScript.Enum.InventorySlot;
using SavingThrow = SWLOR.Game.Server.Core.NWScript.Enum.SavingThrow;
using System.Buffers.Text;
using System.Collections;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    public class Stat
    {
        public const int BaseHP = 70;
        public const int BaseFP = 10;
        public const int BaseSTM = 10;

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        [NWNEventHandler("mod_enter")]
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

            ApplyPlayerMovementRate(player);
        }

        /// <summary>
        /// Retrieves the maximum FP on a creature.
        /// For players:
        /// Each Vitality modifier grants +2 to max FP.
        /// For NPCs:
        /// FP is read from their skin.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of FP</returns>
        public static int GetMaxFP(uint creature, Player dbPlayer = null)
        {
            var modifier = GetAbilityModifier(AbilityType.Willpower, creature);
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
            var foodBonus = 0;
            int baseFP;

            if (foodEffect != null)
            {
                foodBonus = foodEffect.FP;
            }

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

            return GetMaxFP(baseFP, modifier, foodBonus);
        }

        public static int GetMaxFP(int baseFP, int modifier, int bonus)
        {
            return baseFP + modifier * 10 + bonus;
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

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// CON modifier will be checked. Each modifier grants +2 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        public static int GetMaxStamina(uint creature, Player dbPlayer = null)
        {
            var modifier = GetAbilityModifier(AbilityType.Agility, creature);
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
            var foodBonus = 0;
            int baseStamina;

            if (foodEffect != null)
            {
                foodBonus = foodEffect.STM;
            }

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

            return GetMaxStamina(baseStamina, modifier, foodBonus);
        }

        public static int GetMaxStamina(int baseFP, int modifier, int bonus)
        {
            return baseFP + modifier * 5 + bonus;
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
        public static void RestoreFP(uint creature, int amount, Player dbPlayer = null)
        {
            if (amount <= 0) return;

            var maxFP = GetMaxFP(creature);
            
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }
                
                dbPlayer.FP += amount;

                if (dbPlayer.FP > maxFP)
                    dbPlayer.FP = maxFP;
                
                DB.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var fp = GetLocalInt(creature, "FP");
                fp += amount;

                if (fp > maxFP)
                    fp = maxFP;

                SetLocalInt(creature, "FP", fp);
            }
            
            ExecuteScript("pc_fp_adjusted", creature);
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
        public static void RestoreStamina(uint creature, int amount, Player dbPlayer = null)
        {
            if (amount <= 0) return;

            var maxSTM = GetMaxStamina(creature);

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = DB.Get<Player>(playerId);
                }

                dbPlayer.Stamina += amount;

                if (dbPlayer.Stamina > maxSTM)
                    dbPlayer.Stamina = maxSTM;

                DB.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var fp = GetLocalInt(creature, "STAMINA");
                fp += amount;

                if (fp > maxSTM)
                    fp = maxSTM;

                SetLocalInt(creature, "STAMINA", fp);
            }

            ExecuteScript("pc_stm_adjusted", creature);
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
        [NWNEventHandler("assoc_stateffect")]
        public static void ReapplyFoodHP()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            // Player returned after the server restarted. They no longer have the food status effect.
            // Reduce their HP by the amount tracked in the DB.
            if (dbPlayer.TemporaryFoodHP > 0 && !StatusEffect.HasStatusEffect(player, StatusEffectType.Food))
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
        
        public static void ApplyPlayerMovementRate(uint player)
        {
            var movementRate = 1.0f;
            if (Ability.IsAbilityToggled(player, AbilityToggleType.Dash))
            {
                var level = Perk.GetEffectivePerkLevel(player, PerkType.Dash);
                switch (level)
                {
                    case 1:
                        movementRate += 0.1f; // 10%
                        break;
                    case 2:
                        movementRate += 0.25f; // 25%
                        break;
                }
            }

            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
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

            CreaturePlugin.SetMovementRateFactor(player, movementRate);
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

            var totalStat = entity.BaseStats[ability] + entity.UpgradedStats[ability];
            CreaturePlugin.SetRawAbilityScore(player, ability, totalStat);
        }

        /// <summary>
        /// Modifies the ability recast reduction of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPlayerRecastReduction(Player entity, int adjustBy)
        {
            entity.AbilityRecastReduction += adjustBy;
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
        
        /// <summary>
        /// Modifies defense value based on effects found on creature.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="defense">The current defense value which will be modified.</param>
        /// <param name="type">The type of defense to check.</param>
        /// <returns>A modified defense value.</returns>
        private static int CalculateEffectDefense(uint creature, int defense, CombatDamageType type)
        {
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);

            if (type == CombatDamageType.Physical)
            {
                // Iron Shell
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.IronShell))
                    defense += 20;

                // Shielding
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding1))
                    defense += 5;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding2))
                    defense += 10;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding3))
                    defense += 15;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding4))
                    defense += 20;

                // Force Valor
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceValor1))
                    defense += 10;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceValor2))
                    defense += 20;

                // Bolster Armor
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterArmor1))
                    defense += 5;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterArmor2))
                    defense += 10;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterArmor3))
                    defense += 15;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterArmor4))
                    defense += 20;
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterArmor5))
                    defense += 25;

                // Frenzied Shout
                if (StatusEffect.HasStatusEffect(creature, StatusEffectType.FrenziedShout))
                {
                    var source = StatusEffect.GetEffectData<uint>(creature, StatusEffectType.FrenziedShout);
                    if (GetIsObjectValid(source))
                    {
                        var sourceSOC = GetAbilityScore(source, AbilityType.Social);
                        var perkLevel = Perk.GetEffectivePerkLevel(source, PerkType.FrenziedShout);
                        switch (perkLevel)
                        {
                            case 1:
                                defense -= sourceSOC;
                                break;
                            case 2:
                                defense -= (int)(sourceSOC * 1.5f);
                                break;
                            case 3:
                                defense -= sourceSOC * 2;
                                break;
                        }
                    }
                }

                // Food Effects
                if(foodEffect != null)
                    defense += foodEffect.DefensePhysical;
            }
            else if (type == CombatDamageType.Force)
            {
                if (foodEffect != null)
                    defense += foodEffect.DefenseForce;
            }
            else if (type == CombatDamageType.Poison)
            {
                if (foodEffect != null)
                    defense += foodEffect.DefensePoison;
            }
            else if (type == CombatDamageType.Fire)
            {
                if (foodEffect != null)
                    defense += foodEffect.DefenseFire;
            }
            else if (type == CombatDamageType.Ice)
            {
                if (foodEffect != null)
                    defense += foodEffect.DefenseIce;
            }
            else if (type == CombatDamageType.Electrical)
            {
                if (foodEffect != null)
                    defense += foodEffect.DefenseElectrical;
            }

            return defense;
        }

        private static int CalculateEffectAttack(uint creature, int attack)
        {
            // Force Rage
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceRage1))
                attack += 10;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceRage2))
                attack += 20;

            // Soldiers Strike
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.SoldiersStrike))
            {
                var source = StatusEffect.GetEffectData<uint>(creature, StatusEffectType.SoldiersStrike);
                if (GetIsObjectValid(source))
                {
                    var perkLevel = Perk.GetEffectivePerkLevel(source, PerkType.SoldiersStrike);
                    var sourceSOC = GetAbilityScore(source, AbilityType.Social);

                    switch (perkLevel)
                    {
                        case 1:
                            attack += sourceSOC;
                            break;
                        case 2:
                            attack += (int)(sourceSOC * 1.5f);
                            break;
                        case 3:
                            attack += sourceSOC * 2;
                            break;
                    }
                }
            }

            // Food Effects
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
            if (foodEffect != null)
            {
                attack += foodEffect.Attack;
            }

            // Bolster Attack
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterAttack1))
                attack += 5;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterAttack2))
                attack += 10;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterAttack3))
                attack += 15;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterAttack4))
                attack += 20;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.BolsterAttack5))
                attack += 25;

            return attack;
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

            return GetAttack(skillLevel, stat, attackBonus);
        }

        public static int GetAttackNative(CNWSCreature creature, BaseItem itemType)
        {
            var attackBonus = 0;
            var skillLevel = 0;
            var statType = Item.GetWeaponDamageAbilityType(itemType);
            var stat = GetStatValueNative(creature, statType);
            var skillType = Skill.GetSkillTypeByBaseItem(itemType);

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if(skillType != SkillType.Invalid)
                        skillLevel = dbPlayer.Skills[skillType].Rank;

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
                var npcStats = GetNPCStatsNative(creature);

                skillLevel = npcStats.Skills.ContainsKey(skillType) 
                    ? npcStats.Skills[skillType] 
                    : npcStats.Level;

                if (skillType == SkillType.Force)
                    attackBonus += npcStats.ForceAttack;
                else
                    attackBonus += npcStats.Attack;
            }

            attackBonus = CalculateEffectAttack(creature.m_idSelf, attackBonus);
            
            return GetAttack(skillLevel, stat, attackBonus);
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
        /// Retrieves the total defense toward a specific type of damage.
        /// Physical and Force types include effect bonuses, stats, etc.
        /// Fire/Poison/Electrical/Ice include effect bonuses, stats, etc. at 70% of physical.
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

            var defenseBonus = 0;
            var defenderStat = GetAbilityScore(creature, abilityType);
            int skillLevel;
            var equipmentDefense = 0 + defenseBonusOverride;
            var rate = 1.0f;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = DB.Get<Player>(playerId);

                if (type == CombatDamageType.Fire ||
                    type == CombatDamageType.Poison ||
                    type == CombatDamageType.Electrical ||
                    type == CombatDamageType.Ice)
                {
                    rate = 0.7f;
                }

                skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;

                if(defenseBonusOverride <= 0)
                    equipmentDefense += dbPlayer.Defenses[type];
            }
            else
            {
                var npcStats = GetNPCStats(creature);

                if (type == CombatDamageType.Fire ||
                    type == CombatDamageType.Poison ||
                    type == CombatDamageType.Electrical ||
                    type == CombatDamageType.Ice)
                {
                    rate = 0.7f;
                }
                
                if (defenseBonusOverride <= 0)
                {
                    equipmentDefense += npcStats.Defenses.ContainsKey(type) 
                        ? npcStats.Defenses[type]
                        : 0;
                }

                skillLevel = npcStats.Level;
            }

            defenseBonus = CalculateEffectDefense(creature, defenseBonus, type);
            defenseBonus = (int)(defenseBonus * rate) + equipmentDefense;
            return CalculateDefense(defenderStat, skillLevel, defenseBonus);
        }

        public static int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus)
        {
            return (int)(8 + (defenderStat * 1.5f) + skillLevel + defenseBonus);
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
        /// Retrieves the total defense toward a specific type of damage.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        public static int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType)
        {
            var defenseBonus = 0;
            var defenderStat = GetStatValueNative(creature, abilityType);
            var skillLevel = 0;
            var equipmentDefense = 0;
            var rate = 1.0f;

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if (type == CombatDamageType.Fire ||
                        type == CombatDamageType.Poison ||
                        type == CombatDamageType.Electrical ||
                        type == CombatDamageType.Ice)
                    {
                        rate = 0.7f;
                    }

                    skillLevel = dbPlayer.Skills[SkillType.Armor].Rank;
                    equipmentDefense += dbPlayer.Defenses[type];
                }
            }
            else
            {
                var npcStats = GetNPCStatsNative(creature);
                if (type == CombatDamageType.Fire ||
                    type == CombatDamageType.Poison ||
                    type == CombatDamageType.Electrical ||
                    type == CombatDamageType.Ice)
                {
                    rate = 0.7f;
                }

                equipmentDefense += npcStats.Defenses.ContainsKey(type)
                    ? npcStats.Defenses[type]
                    : 0;

                skillLevel = npcStats.Level;
            }
            
            defenseBonus = CalculateEffectDefense(creature.m_idSelf, defenseBonus, type);
            defenseBonus = (int)(defenseBonus * rate) + equipmentDefense;
            return (int)(8 + (defenderStat * 1.5f) + skillLevel + defenseBonus);
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <param name="statOverride">The stat override used to calculate accuracy. This stat will be used instead of whatever stat is defined for the weapon type.</param>
        /// <param name="skillOverride">The skill override used to calculate accuracy. This skill will be used instead of whatever skill is defined for the weapon type.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public static int GetAccuracy(uint creature, uint weapon, AbilityType statOverride, SkillType skillOverride)
        {
            var accuracyBonus = 0;

            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);

                // Attack Bonus / Enhancement Bonus found on the weapon.
                if (type == ItemPropertyType.AccuracyBonus ||
                    type == ItemPropertyType.EnhancementBonus)
                {
                    accuracyBonus += GetItemPropertyCostTableValue(ip);
                }
                // Accuracy Stat Override - Always "wins" even if another override was passed in.
                else if (type == ItemPropertyType.AccuracyStat)
                {
                    statOverride = (AbilityType)GetItemPropertySubType(ip);
                }
            }


            var baseItemType = GetBaseItemType(weapon);
            var statType = statOverride == AbilityType.Invalid ? 
                Item.GetWeaponAccuracyAbilityType(baseItemType) :
                statOverride;
            var stat = statType == AbilityType.Invalid ? 0 : GetAbilityScore(creature, statType);
            var skillType = skillOverride == SkillType.Invalid ? Skill.GetSkillTypeByBaseItem(baseItemType) : skillOverride;
            var skillLevel = 0;


            // Creature skill level / NPC level
            if (GetIsPC(creature) && !GetIsDM(creature))
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

            return GetAccuracy(skillLevel, stat, accuracyBonus);
        }

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <param name="statOverride">The stat override used to calculate accuracy. This stat will be used instead of whatever stat is defined for the weapon type.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public static int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, AbilityType statOverride)
        {
            var accuracyBonus = 0;

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
                Item.GetWeaponAccuracyAbilityType(baseItemType) :
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
            
            return GetAccuracy(skillLevel, stat, accuracyBonus);
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
            return stat * 3 + level + bonus;
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

            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
            if (foodEffect != null)
            {
                accuracy += foodEffect.Accuracy;
            }

            accuracy += GetSoldierPrecisionAccuracyBonus(creature);

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

            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature.m_idSelf, StatusEffectType.Food);
            if (foodEffect != null)
            {
                accuracy += foodEffect.Accuracy;
            }

            accuracy += GetSoldierPrecisionAccuracyBonus(creature.m_idSelf);

            Log.Write(LogGroup.Attack, $"Native Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private static int CalculateEffectEvasion(uint creature)
        {
            var evasionBonus = 0;
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);

            // Soldiers Speed
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.SoldiersSpeed))
            {
                var source = StatusEffect.GetEffectData<uint>(creature, StatusEffectType.SoldiersSpeed);
                if (GetIsObjectValid(source))
                {
                    var sourceSOC = GetAbilityScore(creature, AbilityType.Social);
                    var perkLevel = Perk.GetEffectivePerkLevel(creature, PerkType.SoldiersSpeed);

                    switch (perkLevel)
                    {
                        case 1:
                            evasionBonus += sourceSOC / 2;
                            break;
                        case 2:
                            evasionBonus += sourceSOC;
                            break;
                        case 3:
                            evasionBonus += (int)(sourceSOC * 1.5f);
                            break;
                    }

                }
            }

            // Food Effects
            if (foodEffect != null)
            {
                evasionBonus += foodEffect.Evasion;
            }

            // Evasive Maneuver
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.EvasiveManeuver1))
                evasionBonus += 5;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.EvasiveManeuver2))
                evasionBonus += 10;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.EvasiveManeuver3))
                evasionBonus += 15;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.EvasiveManeuver4))
                evasionBonus += 20;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.EvasiveManeuver5))
                evasionBonus += 25;

            // Assault
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Assault))
                evasionBonus += 10;

            return evasionBonus;
        }

        private static int GetSoldierPrecisionAccuracyBonus(uint creature)
        {
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.SoldiersPrecision))
            {
                var source = StatusEffect.GetEffectData<uint>(creature, StatusEffectType.SoldiersPrecision);

                if (GetIsObjectValid(source))
                {
                    var sourceSOC = GetAbilityScore(source, AbilityType.Social);
                    var perkLevel = Perk.GetEffectivePerkLevel(source, PerkType.SoldiersPrecision);

                    switch (perkLevel)
                    {
                        case 1:
                            return sourceSOC / 2;
                        case 2:
                            return sourceSOC;
                        case 3:
                            return (int)(sourceSOC * 1.5f);
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasion(uint creature, SkillType skillOverride)
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

            return GetEvasion(skillLevel, stat, ac * 5 + evasionBonus);
        }

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasionNative(CNWSCreature creature)
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
            
            return GetEvasion(skillLevel, stat, ac * 5 + evasionBonus);
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
            return stat * 3 + level + bonus;
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
                    npcStats.Defenses[damageType] = GetItemPropertyCostTableValue(ip);
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

        private static NPCStats GetNPCStatsNative(CNWSCreature npc)
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
        /// Applies the total number of attacks per round to a player.
        /// If a valid weapon is passed in the associated mastery perk will also be checked.
        /// </summary>
        /// <param name="creature">The player to apply attacks to</param>
        /// <param name="rightHandWeapon">The weapon equipped to the right hand.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        public static void ApplyAttacksPerRound(uint creature, uint rightHandWeapon, uint offHandItem = OBJECT_INVALID)
        {
            static int GetBABForAttacks(int attacks)
            {
                switch (attacks)
                {
                    case 1:
                        return 1;
                    case 2:
                        return 6;
                    case 3:
                        return 11;
                    case 4:
                        return 16;
                    case 5:
                        return 21;
                    case 6:
                        return 26;
                    case 7:
                        return 31;
                    case 8:
                        return 36;
                    case 9:
                        return 41;
                }

                return 1;
            }

            static int GetRapidShotBonus(uint pc)
            {
                return Perk.GetEffectivePerkLevel(pc, PerkType.RapidShot);
            }

            static int GetFlurryBonus(uint pc)
            {
                return Perk.GetEffectivePerkLevel(pc, PerkType.FlurryStyle);
            }

            static int GetShieldBonus(uint pc)
            {
                return Perk.GetEffectivePerkLevel(pc, PerkType.ShieldMaster);
            }

            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var itemType = GetBaseItemType(rightHandWeapon);
            var offHandType = GetBaseItemType(offHandItem);
            var numberOfAttacks = 1;
            var perkType = PerkType.Invalid;

            // Martial Arts
            if (Item.KatarBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.KatarMastery;
            }
            else if (Item.StaffBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.StaffMastery;
                numberOfAttacks += GetFlurryBonus(creature);
            }
            // Ranged (Pistol & Rifle only. Throwing is intentionally excluded from Rapid Shot because they get Doublehand)
            else if (Item.PistolBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.PistolMastery;
                numberOfAttacks += GetRapidShotBonus(creature);
            }
            else if (Item.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.ThrowingWeaponMastery;
            }
            else if (Item.RifleBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.RifleMastery;
            }
            // One-Handed
            else if (Item.VibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.VibrobladeMastery;
            }
            else if (Item.FinesseVibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.FinesseVibrobladeMastery;
            }
            else if (Item.LightsaberBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.LightsaberMastery;
            }
            // Two-Handed
            else if (Item.HeavyVibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.HeavyVibrobladeMastery;
            }
            else if (Item.PolearmBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.PolearmMastery;
            }
            else if (Item.TwinBladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.TwinBladeMastery;
            }
            else if (Item.SaberstaffBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.SaberstaffMastery;
            }

            if (Item.ShieldBaseItemTypes.Contains(offHandType)) 
                numberOfAttacks += GetShieldBonus(creature);

            var effectiveMasteryLevel = Perk.GetEffectivePerkLevel(creature, perkType);
            numberOfAttacks += effectiveMasteryLevel;

            // Beast Speed (1-3)
            numberOfAttacks += Perk.GetEffectivePerkLevel(creature, PerkType.BeastSpeed);

            var bab = GetBABForAttacks(numberOfAttacks);
            CreaturePlugin.SetBaseAttackBonus(creature, bab);
        }

        public static void ApplyCritModifier(uint player, uint rightHandWeapon)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var critMod = 0;
            var itemType = GetBaseItemType(rightHandWeapon);
            var offhandType = GetBaseItemType(GetItemInSlot(InventorySlot.LeftHand, player));
            if (Item.OneHandedMeleeItemTypes.Contains(itemType) || Item.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                if (Item.OneHandedMeleeItemTypes.Contains(offhandType))
                    critMod += Perk.GetEffectivePerkLevel(player, PerkType.WailingBlows) * 3; // 15% for WB
                else if(offhandType == BaseItem.Invalid || Item.ShieldBaseItemTypes.Contains(offhandType))
                    critMod += Perk.GetEffectivePerkLevel(player, PerkType.Duelist);
            }

            if(Item.ThrowingWeaponBaseItemTypes.Contains(itemType) || Item.PistolBaseItemTypes.Contains(itemType))
            {
                critMod += Perk.GetEffectivePerkLevel(player, PerkType.DirtyBlow) * 2; // 10% for DB
            }

            critMod += Perk.GetEffectivePerkLevel(player, PerkType.InnerStrength);

            CreaturePlugin.SetCriticalRangeModifier(player, -critMod, 0, true);
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
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
            if (foodEffect != null)
            {
                control += foodEffect.Control[craftingSkillType];
            }

            return control;
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
            var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(player, StatusEffectType.Food);
            if (foodEffect != null)
            {
                control += foodEffect.Craftsmanship[craftingSkillType];
            }

            return control;
        }

        /// <summary>
        /// Calculates the base value for a particular type of saving throw.
        /// This does not factor in stat modifiers.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of saving throw.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        /// <returns>The base saving throw value</returns>
        public static int CalculateBaseSavingThrow(uint player, SavingThrow type, uint offHandItem = OBJECT_INVALID)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var offHandType = GetBaseItemType(offHandItem);
            var amount = 0;

            if (Item.ShieldBaseItemTypes.Contains(offHandType))
            {
                amount += Perk.GetEffectivePerkLevel(player, PerkType.ShieldResistance);
            }

            return amount;
        }

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public static void LoadNPCStats()
        {
            var self = OBJECT_SELF;
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, self);

            var maxHP = 0;
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCHP)
                {
                    maxHP += GetItemPropertyCostTableValue(ip);
                }
            }

            if (maxHP > 30000)
                maxHP = 30000;

            if (maxHP > 0)
            {
                ObjectPlugin.SetMaxHitPoints(self, maxHP);
                ObjectPlugin.SetCurrentHitPoints(self, maxHP);
            }

            SetLocalInt(self, "FP", GetMaxFP(self));
            SetLocalInt(self, "STAMINA", GetMaxStamina(self));
        }

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        public static void RestoreNPCStats(bool outOfCombatRegen)
        {
            var self = OBJECT_SELF;
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
