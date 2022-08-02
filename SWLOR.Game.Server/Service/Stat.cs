﻿using System.Collections.Generic;
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

namespace SWLOR.Game.Server.Service
{
    public class Stat
    {
        private static readonly Dictionary<uint, Dictionary<CombatDamageType, int>> _npcDefenses = new();

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
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }
                var baseFP = dbPlayer.MaxFP;
                var modifier = GetAbilityModifier(AbilityType.Willpower, creature);
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
                var foodBonus = 0;

                if (foodEffect != null)
                {
                    foodBonus = foodEffect.FP;
                }

                return baseFP + modifier * 10 + foodBonus;
            }
            // NPCs
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);

                var ep = 0;
                for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.NPCEP)
                    {
                        ep += GetItemPropertyCostTableValue(ip);
                    }
                }

                return ep;
            }
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
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = DB.Get<Player>(playerId);
                }

                var baseStamina = dbPlayer.MaxStamina;
                var modifier = GetAbilityModifier(AbilityType.Agility, creature);
                var foodEffect = StatusEffect.GetEffectData<FoodEffectData>(creature, StatusEffectType.Food);
                var foodBonus = 0;

                if (foodEffect != null)
                {
                    foodBonus = foodEffect.STM;
                }

                return baseStamina + modifier * 5 + foodBonus;
            }
            // NPCs
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);

                var stm = 0;
                for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.NPCSTM)
                    {
                        stm += GetItemPropertyCostTableValue(ip);
                    }
                }

                return stm;
            }
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
                Stat.AdjustPlayerMaxHP(dbPlayer, player, -dbPlayer.TemporaryFoodHP);
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
        /// When a creature spawns, load its relevant defense information based on their equipment.
        /// </summary>
        [NWNEventHandler("crea_spawn_bef")]
        public static void LoadNPCDefense()
        {
            var creature = OBJECT_SELF;
            _npcDefenses[creature] = new Dictionary<CombatDamageType, int>();

            foreach (var type in Combat.GetAllDamageTypes())
            {
                _npcDefenses[creature][type] = 0;
            }

            // Pull defense values off skin.
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Defense)
                {
                    var damageType = (CombatDamageType)GetItemPropertySubType(ip);
                    if (damageType == CombatDamageType.Invalid)
                        continue;

                    _npcDefenses[creature][damageType] += GetItemPropertyCostTableValue(ip);
                }
            }
        }

        [NWNEventHandler("crea_death_aft")]
        public static void ClearNPCDefense()
        {
            if (_npcDefenses.ContainsKey(OBJECT_SELF))
                _npcDefenses.Remove(OBJECT_SELF);
        }

        /// <summary>
        /// Modifies defense value based on effects found on creature.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="defense">The current defense value which will be modified.</param>
        /// <returns>A modified defense value.</returns>
        private static int CalculateEffectDefense(uint creature, int defense)
        {
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.IronShell))
                defense += 20;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding1))
                defense += 5;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding2))
                defense += 10;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding3))
                defense += 15;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.Shielding4))
                defense += 20;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceValor1))
                defense += 10;

            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceValor2))
                defense += 20;

            return defense;
        }

        private static int CalculateEffectAttack(uint creature, int attack)
        {
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceRage1))
                attack += 10;
            if (StatusEffect.HasStatusEffect(creature, StatusEffectType.ForceRage2))
                attack += 20;

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
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
            }

            attackBonus = CalculateEffectAttack(creature, attackBonus);

            return 8 + (2 * skillLevel) + stat + attackBonus;
        }

        public static int GetAttackNative(CNWSCreature creature, BaseItem itemType)
        {
            var attackBonus = 0;
            var skillLevel = 0;
            var statType = Item.GetWeaponDamageAbilityType(itemType);
            var stat = GetStatValueNative(creature, statType);

            if (creature.m_bPlayerCharacter == 1)
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = DB.Get<Player>(playerId);
                var skillType = Skill.GetSkillTypeByBaseItem(itemType);

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
                var npcStats = GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
            }

            attackBonus = CalculateEffectAttack(creature.m_idSelf, attackBonus);

            return 8 + (2 * skillLevel) + stat + attackBonus;
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

                if (_npcDefenses.ContainsKey(creature) && defenseBonusOverride <= 0)
                {
                    equipmentDefense += _npcDefenses[creature][type];
                }

                skillLevel = npcStats.Level;
            }

            if (type == CombatDamageType.Physical)
            {
                defenseBonus = CalculateEffectDefense(creature, defenseBonus);
            }

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
                    stat = creature.m_pStats.m_nStrengthBase;
                    break;
                case AbilityType.Perception:
                    stat = creature.m_pStats.m_nDexterityBase;
                    break;
                case AbilityType.Vitality:
                    stat = creature.m_pStats.m_nConstitutionBase;
                    break;
                case AbilityType.Willpower:
                    stat = creature.m_pStats.m_nWisdomBase;
                    break;
                case AbilityType.Agility:
                    stat = creature.m_pStats.m_nIntelligenceBase;
                    break;
                case AbilityType.Social:
                    stat = creature.m_pStats.m_nCharismaBase;
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

                if (_npcDefenses.ContainsKey(creature.m_idSelf))
                {
                    equipmentDefense += _npcDefenses[creature.m_idSelf][type];
                }

                skillLevel = npcStats.Level;
            }

            if (type == CombatDamageType.Physical)
            {
                defenseBonus = CalculateEffectDefense(creature.m_idSelf, defenseBonus);
            }

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
            var baseItemType = GetBaseItemType(weapon);
            var statType = statOverride == AbilityType.Invalid ? 
                Item.GetWeaponAccuracyAbilityType(baseItemType) :
                statOverride;
            var stat = statType == AbilityType.Invalid ? 0 : GetAbilityScore(creature, statType);
            var skillType = skillOverride == SkillType.Invalid ? Skill.GetSkillTypeByBaseItem(baseItemType) : skillOverride;
            var skillLevel = 0;
            var accuracyBonus = 0;

            // Attack Bonus / Enhancement Bonus found on the weapon.
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.AttackBonus ||
                    type == ItemPropertyType.EnhancementBonus)
                {
                    accuracyBonus += GetItemPropertyCostTableValue(ip) * 2;
                }
            }

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

            return stat * 3 + skillLevel + accuracyBonus;
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
            var baseItemType = weapon == null ? BaseItem.Invalid : (BaseItem)weapon.m_nBaseItem;
            var statType = statOverride == AbilityType.Invalid ? 
                Item.GetWeaponAccuracyAbilityType(baseItemType) :
                statOverride;
            var skillType = Skill.GetSkillTypeByBaseItem(baseItemType);
            var stat = GetStatValueNative(creature, statType);
            var skillLevel = 0;
            var accuracyBonus = 0;

            // Attack Bonus / Enhancement Bonus found on the weapon.
            if (weapon != null)
            {
                foreach (var ip in weapon.m_lstPassiveProperties)
                {
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.AttackBonus ||
                        ip.m_nPropertyName == (ushort)ItemPropertyType.EnhancementBonus)
                    {
                        accuracyBonus += ip.m_nCostTableValue * 2;
                    }
                }
            }

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

            return stat * 3 + skillLevel + accuracyBonus;
        }

        private static int CalculateEffectAccuracy(uint creature, int accuracy)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (type == EffectTypeScript.AttackIncrease)
                {
                    accuracy += 3 * GetEffectInteger(effect, 1);
                }
                else if (type == EffectTypeScript.AttackDecrease)
                {
                    accuracy -= 3 * GetEffectInteger(effect, 1);
                }
            }

            Log.Write(LogGroup.Attack, $"Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private static int CalculateEffectAccuracyNative(CNWSCreature creature, int accuracy)
        {
            foreach (var effect in creature.m_appliedEffects)
            {
                if (effect.m_nType == (ushort)EffectTrueType.AttackIncrease)
                {
                    accuracy += 3 * effect.GetInteger(1);
                }
                else if (effect.m_nType == (ushort)EffectTrueType.AttackDecrease)
                {
                    accuracy -= 3 * effect.GetInteger(1);
                }
            }

            Log.Write(LogGroup.Attack, $"Native Effect Accuracy: {accuracy}");

            return accuracy;
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
            var evasionBonus = 0;

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
            }

            Log.Write(LogGroup.Attack, $"Effect Evasion: {evasionBonus}");

            return stat * 3 + skillLevel + ac * 5 + evasionBonus;
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
            }

            return stat * 3 + skillLevel + ac * 5 + evasionBonus;
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
                }
            }

            return npcStats;
        }

        /// <summary>
        /// Applies the total number of attacks per round to a player.
        /// If a valid weapon is passed in the associated mastery perk will also be checked.
        /// </summary>
        /// <param name="player">The player to apply attacks to</param>
        /// <param name="rightHandWeapon">The weapon equipped to the right hand.</param>
        public static void ApplyAttacksPerRound(uint player, uint rightHandWeapon)
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

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var itemType = GetBaseItemType(rightHandWeapon);
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
            }
            // Ranged
            else if (Item.PistolBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.PistolMastery;
                numberOfAttacks += GetRapidShotBonus(player);
            }
            else if (Item.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.ThrowingWeaponMastery;
                numberOfAttacks += GetRapidShotBonus(player);
            }
            else if (Item.RifleBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.RifleMastery;
                numberOfAttacks += GetRapidShotBonus(player);
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

            var effectiveMasteryLevel = Perk.GetEffectivePerkLevel(player, perkType);
            numberOfAttacks += effectiveMasteryLevel;

            var bab = GetBABForAttacks(numberOfAttacks);
            CreaturePlugin.SetBaseAttackBonus(player, bab);
        }

    }
}
