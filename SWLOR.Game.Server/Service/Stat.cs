using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Stats: the FP and Stamina pools, the player records fed by worn gear, walk mode and movement rate, the Attack,
    /// Accuracy, Defense and Evasion ratings, deflection and healing reactions, and NPC stat skins. The logic lives in
    /// <see cref="IStatService"/>; the event handlers stay here and every member forwards to the container's instance.
    /// <see cref="StatRules"/> holds the stat type metadata and the formulas the offline tools read, for code that has
    /// no container.
    /// </summary>
    public static class Stat
    {
        public const int BaseHP = Domain.Character.StatService.BaseHP;
        public const int BaseFP = Domain.Character.StatService.BaseFP;
        public const int BaseSTM = Domain.Character.StatService.BaseSTM;
        public const float BeastNaturalStaminaRegenDelaySeconds = Domain.Character.StatService.BeastNaturalStaminaRegenDelaySeconds;
        public const int BeastNaturalRegenStatDivisor = Domain.Character.StatService.BeastNaturalRegenStatDivisor;
        public const int DefaultMeleeDeflectionChanceCap = Domain.Character.StatService.DefaultMeleeDeflectionChanceCap;
        public const int DefaultRangedDeflectionChanceCap = Domain.Character.StatService.DefaultRangedDeflectionChanceCap;
        public const int MaximumDeflectionChanceCap = Domain.Character.StatService.MaximumDeflectionChanceCap;
        public const int MaximumShieldDeflectionChance = Domain.Character.StatService.MaximumShieldDeflectionChance;
        public const int MaximumGuardChance = Domain.Character.StatService.MaximumGuardChance;
        public const int MaximumCombatReadinessPercent = Domain.Character.StatService.MaximumCombatReadinessPercent;
        public const int MaximumNPCDetection = Domain.Character.StatService.MaximumNPCDetection;
        public const float MinimumMovementSpeedMultiplier = Domain.Character.StatService.MinimumMovementSpeedMultiplier;
        public const float MaximumMovementSpeedMultiplier = Domain.Character.StatService.MaximumMovementSpeedMultiplier;

        /// <summary>
        /// Set to 1 on an NPC to disable natural regeneration: the out-of-combat
        /// 10%-per-tick HP heal and the 1-per-tick FP/STM restore. Engine-test fixtures
        /// wound casters to observe an ability's own healing and verify EXACT resource
        /// costs; a natural regen tick inside the assertion window would otherwise satisfy
        /// a healing assertion for a broken impact, or drift a pool off the exact
        /// post-deduction value.
        /// </summary>
        public const string SuppressNaturalRegenVariable = Domain.Character.StatService.SuppressNaturalRegenVariable;

        private static IStatService Instance => FacadeAccessor.Resolve<IStatService>();

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData() => Instance.CacheData();

        public static StatTypeCategory GetStatTypeCategory(StatType statType) => Instance.GetStatTypeCategory(statType);

        public static StatTypeAggregation GetStatTypeAggregation(StatType statType)
            => Instance.GetStatTypeAggregation(statType);

        public static DeflectionSource GetStatTypeDeflectionSource(StatType statType)
            => Instance.GetStatTypeDeflectionSource(statType);

        public static StatType GetGrantedDeflectionStatType(StatType sourceStatType)
            => Instance.GetGrantedDeflectionStatType(sourceStatType);

        public static int AggregateStatAdjustment(StatType statType, int current, int adjustment)
            => Instance.AggregateStatAdjustment(statType, current, adjustment);

        public static bool IsBeneficialStatAdjustment(StatType statType, int value)
            => Instance.IsBeneficialStatAdjustment(statType, value);

        /// <summary>
        /// Registers what equipping an item property does to its wearer. <c>EquipmentStats.RegisterStatActions</c> fills
        /// this when the module loads; until then equipping applies nothing.
        /// </summary>
        public static void RegisterEquipmentStatAction(ItemPropertyType type, StatEquipmentAction action)
            => Instance.RegisterEquipmentStatAction(type, action);

        /// <summary>
        /// Finds what equipping an item property does to its wearer.
        /// </summary>
        public static bool TryGetEquipmentStatAction(ItemPropertyType type, out StatEquipmentAction action)
            => Instance.TryGetEquipmentStatAction(type, out action);

        /// <summary>
        /// The item property types equipping adds to the wearer.
        /// </summary>
        public static IReadOnlyCollection<ItemPropertyType> EquipmentStatActionTypes => Instance.EquipmentStatActionTypes;

        public static int ScaleEffect(
            int baseAmount,
            int primaryStat,
            float primaryRate = 0.01f,
            int secondaryStat = 0,
            float secondaryRate = 0.005f)
            => Instance.ScaleEffect(baseAmount, primaryStat, primaryRate, secondaryStat, secondaryRate);

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyPlayerStats() => Instance.ApplyPlayerStats();

        /// <summary>
        /// The engine keeps a player's local variables across a relog until the server resets,
        /// but the forced walk set through NWNX belongs to the old creature object.
        /// Reapply it so a player who logged out walking still walks.
        /// </summary>
        public static void RestoreWalkMode(uint player) => Instance.RestoreWalkMode(player);

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
        public static int GetMaxFP(uint creature, Player dbPlayer = null) => Instance.GetMaxFP(creature, dbPlayer);

        public static int GetMaxFP(int baseFP, int willpower, int bonus) => Instance.GetMaxFP(baseFP, willpower, bonus);

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        public static int GetCurrentFP(uint creature, Player dbPlayer = null)
            => Instance.GetCurrentFP(creature, dbPlayer);

        public static int GetAdjustedRequiredFP(uint creature, int requiredFP)
            => Instance.GetAdjustedRequiredFP(creature, requiredFP);

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// Each point of Might grants +1.5 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        public static int GetMaxStamina(uint creature, Player dbPlayer = null)
            => Instance.GetMaxStamina(creature, dbPlayer);

        public static int GetMaxStamina(int baseStamina, int might, int bonus)
            => Instance.GetMaxStamina(baseStamina, might, bonus);

        /// <summary>
        /// Retrieves the current STM on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve STM from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of STM.</returns>
        public static int GetCurrentStamina(uint creature, Player dbPlayer = null)
            => Instance.GetCurrentStamina(creature, dbPlayer);

        /// <summary>
        /// Restores a creature's FP by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of FP to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of FP actually restored after modifiers and the maximum-FP cap.</returns>
        public static int RestoreFP(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true)
            => Instance.RestoreFP(creature, amount, dbPlayer, sendFeedback);

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        public static void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null)
            => Instance.ReduceFP(creature, reduceBy, dbPlayer);

        /// <summary>
        /// Stealth's stamina drain is its only time limit, so a stealthed player gains no Stamina
        /// from any source: natural regeneration, healing over time, field tools, or stances.
        /// </summary>
        public static bool IsStaminaRestorationBlocked(bool isPlayer, bool inStealth)
            => Instance.IsStaminaRestorationBlocked(isPlayer, inStealth);

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of Stamina actually restored after the maximum-Stamina cap.</returns>
        public static int RestoreStamina(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true)
            => Instance.RestoreStamina(creature, amount, dbPlayer, sendFeedback);

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        public static void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null)
            => Instance.ReduceStamina(creature, reduceBy, dbPlayer);

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateStateEffect)]
        public static void ReapplyFoodHP() => Instance.ReapplyFoodHP();

        public static void ReapplyFoodHP(uint player) => Instance.ReapplyFoodHP(player);

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
            => Instance.AdjustPlayerMaxHP(entity, player, adjustBy);

        /// <summary>
        /// Sets a player's stored maximum HP to the sum of its sources: the base, the HP bonuses on equipped items,
        /// and active food. Equipment and food add and remove their HP as changes, so one missed, failed or repeated
        /// change leaves the record off for good. A record below the native floor of one HP per level then hides
        /// gear HP until enough is worn to climb past that floor.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <returns>True if the record was corrected</returns>
        public static bool RefreshPlayerMaxHP(uint player) => Instance.RefreshPlayerMaxHP(player);

        public static int GetEquippedHPBonus(uint creature) => Instance.GetEquippedHPBonus(creature);

        /// <summary>
        /// Sets every player record summed from worn gear (see <see cref="EquipmentRecordStats"/>) to what the worn gear
        /// grants. Like max HP, these records are kept by adding and removing each equipment change, so one missed,
        /// failed or repeated change leaves them off for good.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <param name="stats">The records to rebuild</param>
        /// <returns>The records that were corrected</returns>
        public static EquipmentRecordStats RefreshPlayerEquipmentStats(uint player, EquipmentRecordStats stats = EquipmentRecordStats.All)
            => Instance.RefreshPlayerEquipmentStats(player, stats);

        /// <summary>
        /// Sums the record stats the worn gear grants, counting the same properties equipping does.
        /// </summary>
        public static EquipmentStatTotals GetEquippedStatTotals(uint creature)
            => Instance.GetEquippedStatTotals(creature);

        /// <summary>
        /// Writes the gear totals into the selected records: Defense and Resistance for every type, maximum FP and STM
        /// above their bases, Control, Craftsmanship and CP Bonus for every crafting skill, and each summed number
        /// such as Evasion, Attack or Stealth. Pools are left alone.
        /// </summary>
        /// <returns>The records that changed</returns>
        public static EquipmentRecordStats ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats)
            => Instance.ApplyEquipmentStatTotals(entity, totals, stats);

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player)
            => Instance.AdjustPlayerMaxFP(entity, adjustBy, player);

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player)
            => Instance.AdjustPlayerMaxSTM(entity, adjustBy, player);

        public static bool IsWalkModeEnabled(uint creature) => Instance.IsWalkModeEnabled(creature);

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public static void ClearWalkModeBeforeDMPossessionChange() => Instance.ClearWalkModeBeforeDMPossessionChange();

        public static bool ToggleWalkMode(uint player) => Instance.ToggleWalkMode(player);

        public static void ApplyCreatureMovementRate(uint creature) => Instance.ApplyCreatureMovementRate(creature);

        /// <summary>
        /// Out-of-combat movement speed depends on the combat state, which has no event of its own, so a player
        /// holding that stat has their movement rate re-applied on the heartbeat that sees the state change.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void RefreshOutOfCombatMovementRate() => Instance.RefreshOutOfCombatMovementRate();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetOutOfCombatMovementState() => Instance.ForgetOutOfCombatMovementState();

        public static float GetMovementSpeedMultiplier(uint creature) => Instance.GetMovementSpeedMultiplier(creature);

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        public static void ApplyPlayerStat(Player entity, uint player, AbilityType ability)
            => Instance.ApplyPlayerStat(entity, player, ability);

        public static void RestorePlayerAttributes(uint player) => Instance.RestorePlayerAttributes(player);

        internal static void RestorePlayerAttributes(Player entity, Action<AbilityType, int> setAbilityScore)
            => Instance.RestorePlayerAttributes(entity, setAbilityScore);

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCombatReadiness(Player entity, int adjustBy)
            => Instance.AdjustCombatReadiness(entity, adjustBy);

        public static int GetCombatReadinessPercent(uint creature) => Instance.GetCombatReadinessPercent(creature);

        /// <summary>
        /// Reconciles the equipment cache after login or a completed equipment change.
        /// Keep this outside ability calculations: one scan can serve every target.
        /// </summary>
        public static void RefreshCombatReadinessEquipment(uint creature)
            => Instance.RefreshCombatReadinessEquipment(creature);

        public static int GetEquippedCombatReadiness(uint creature) => Instance.GetEquippedCombatReadiness(creature);

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustHPRegen(Player entity, int adjustBy) => Instance.AdjustHPRegen(entity, adjustBy);

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustFPRegen(Player entity, int adjustBy) => Instance.AdjustFPRegen(entity, adjustBy);

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustSTMRegen(Player entity, int adjustBy) => Instance.AdjustSTMRegen(entity, adjustBy);

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustDefense(Player entity, CombatDamageType type, int adjustBy)
            => Instance.AdjustDefense(entity, type, adjustBy);

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustEvasion(Player entity, int adjustBy) => Instance.AdjustEvasion(entity, adjustBy);

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustStealth(Player entity, int adjustBy) => Instance.AdjustStealth(entity, adjustBy);

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustDetection(Player entity, int adjustBy) => Instance.AdjustDetection(entity, adjustBy);

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustTrapBonus(Player entity, int adjustBy) => Instance.AdjustTrapBonus(entity, adjustBy);

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustTrapDisarm(Player entity, int adjustBy) => Instance.AdjustTrapDisarm(entity, adjustBy);

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustPoisonBonus(Player entity, int adjustBy)
            => Instance.AdjustPoisonBonus(entity, adjustBy);

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustLockpicking(Player entity, int adjustBy)
            => Instance.AdjustLockpicking(entity, adjustBy);

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustAttack(Player entity, int adjustBy) => Instance.AdjustAttack(entity, adjustBy);

        /// <summary>
        /// Modifies a player's accuracy granted by non-weapon equipment.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustAccuracy(Player entity, int adjustBy) => Instance.AdjustAccuracy(entity, adjustBy);

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustForceAttack(Player entity, int adjustBy)
            => Instance.AdjustForceAttack(entity, adjustBy);

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustControl(Player entity, SkillType skillType, int adjustBy)
            => Instance.AdjustControl(entity, skillType, adjustBy);

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy)
            => Instance.AdjustCraftsmanship(entity, skillType, adjustBy);

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public static void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy)
            => Instance.AdjustCPBonus(entity, skillType, adjustBy);

        /// <summary>
        /// Calculates the attack for a given creature.
        /// </summary>
        /// <param name="creature">The creature to calculate.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="skillType">The type of skill to use.</param>
        /// <param name="attackBonusOverride">Overrides the attack bonus granted by equipment. Usually only used by Space combat.</param>
        /// <returns>The total Attack value of a creature.</returns>
        public static int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0)
            => Instance.GetAttack(creature, abilityType, skillType, attackBonusOverride);

        public static int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid,
            bool useForceAttack = false, SkillType skillOverride = SkillType.Invalid)
            => Instance.GetAttackNative(creature, itemType, statOverride, useForceAttack, skillOverride);

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        public static int GetAttack(int level, int stat, int bonus) => Instance.GetAttack(level, stat, bonus);

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
            => Instance.GetDefense(creature, type, abilityType, defenseBonusOverride);

        public static int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus)
            => Instance.CalculateDefense(defenderStat, skillLevel, defenseBonus);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        public static int GetStatValueNative(CNWSCreature creature, AbilityType statType)
            => Instance.GetStatValueNative(creature, statType);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        public static int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType)
            => Instance.GetDefenseNative(creature, type, abilityType);

        public static int GetAttackPercentAdjustment(uint creature, SkillType skillType)
            => Instance.GetAttackPercentAdjustment(creature, skillType);

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
            => Instance.GetAccuracy(creature, weapon, statOverride, skillOverride, skillLevelOverride, ignoreWeaponAccuracyStatOverride);

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        public static int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, SkillType skillOverride = SkillType.Invalid,
            AbilityType accuracyAbilityOverride = AbilityType.Invalid)
            => Instance.GetAccuracyNative(creature, weapon, skillOverride, accuracyAbilityOverride);

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        public static int GetAccuracy(int level, int stat, int bonus) => Instance.GetAccuracy(level, stat, bonus);

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <param name="incomingSkillType">The skill type of the incoming attack for conditional evasion modifiers.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid)
            => Instance.GetEvasion(creature, skillOverride, incomingSkillType);

        /// <summary>
        /// Retrieves a creature's detection rating, used against Stealth in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The detection rating of a creature.</returns>
        public static int GetDetection(uint creature) => Instance.GetDetection(creature);

        public static int CalculateDetectionRating(
            int perception,
            int willpower,
            int equipmentBonus,
            int adjustment,
            bool detectMode)
            => Instance.CalculateDetectionRating(perception, willpower, equipmentBonus, adjustment, detectMode);

        public static int ApplyNPCDetectionCap(int detection, bool isNPC)
            => Instance.ApplyNPCDetectionCap(detection, isNPC);

        /// <summary>
        /// Retrieves a creature's stealth rating, used against Detection in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The stealth rating of a creature.</returns>
        /// <summary>
        /// Retrieves a creature's lockpicking: equipment plus perk, status, and temporary adjustments.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The lockpicking rating of a creature.</returns>
        public static int GetLockpicking(uint creature) => Instance.GetLockpicking(creature);

        public static int GetStealth(uint creature) => Instance.GetStealth(creature);

        public static int CalculateStealthRating(int agility, int equipmentBonus, int adjustment)
            => Instance.CalculateStealthRating(agility, equipmentBonus, adjustment);

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public static int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid)
            => Instance.GetEvasionNative(creature, incomingSkillType);

        public static int GetMeleeDeflectionChanceNative(CNWSCreature creature)
            => Instance.GetMeleeDeflectionChanceNative(creature);

        public static int GetMeleeDeflectionChance(uint creature) => Instance.GetMeleeDeflectionChance(creature);

        public static int GetRangedDeflectionChanceNative(CNWSCreature creature)
            => Instance.GetRangedDeflectionChanceNative(creature);

        public static int GetRangedDeflectionChance(uint creature) => Instance.GetRangedDeflectionChance(creature);

        public static int GetShieldDeflectionChanceNative(CNWSCreature creature)
            => Instance.GetShieldDeflectionChanceNative(creature);

        public static int GetShieldDeflectionChance(uint creature) => Instance.GetShieldDeflectionChance(creature);

        public static int GetGuardChance(uint creature) => Instance.GetGuardChance(creature);

        public static void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source)
            => Instance.ApplyDeflectionEffectsNative(creature, source);

        /// <summary>
        /// Retrieves the total percentage adjustment applied to a creature's defense for a damage type.
        /// This combines the general defense adjustment with the type-specific adjustment, including
        /// the shield-only bonus when a shield is equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>The percentage adjustment applied to defense.</returns>
        public static int GetDefensePercentAdjustment(uint creature, CombatDamageType type)
            => Instance.GetDefensePercentAdjustment(creature, type);

        public static IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
            => Instance.GetStatSources(creature, payloadStat);

        public static int GetStatAdjustment(uint creature, StatType stat) => Instance.GetStatAdjustment(creature, stat);

        public static int GetStatAdjustmentExcludingTemporaryModifiers(uint creature, StatType stat)
            => Instance.GetStatAdjustmentExcludingTemporaryModifiers(creature, stat);

        public static int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount)
            => Instance.ApplyOutgoingAbilityHealingAdjustment(source, amount);

        public static int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment)
            => Instance.CalculateOutgoingAbilityHealingAmount(amount, adjustment);

        /// <param name="isInstantAbilityHeal">True for a heal an ability lands when it resolves. Only these can restore Stamina through the healing-received trigger.</param>
        public static int ApplyHealingReceivedAdjustment(uint creature, int amount, bool applyReceivedEffects = true, bool isInstantAbilityHeal = false)
            => Instance.ApplyHealingReceivedAdjustment(creature, amount, applyReceivedEffects, isInstantAbilityHeal);

        public static int CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP)
            => Instance.CalculateEffectiveHealingAmount(amount, currentHP, maxHP);

        /// <param name="isInstantAbilityHeal">Heals over time, lifesteal, drains and regeneration pass false and never restore Stamina.</param>
        public static void ApplyHealingReceivedEffects(uint creature, int amount, bool isInstantAbilityHeal = false)
            => Instance.ApplyHealingReceivedEffects(creature, amount, isInstantAbilityHeal);

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        public static int GetEvasion(int level, int stat, int bonus) => Instance.GetEvasion(level, stat, bonus);

        /// <summary>
        /// NPC stat skins carry the complete Evasion budget. Native armor must
        /// not add another budget through inherited equipment, feats or effects.
        /// Stat-based Evasion adjustments remain part of bonus for all creatures.
        /// </summary>
        public static int GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget)
            => Instance.GetEvasion(level, stat, bonus, nativeArmorClass, hasNpcStatBudget);

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        public static NPCStats GetNPCStats(uint npc) => Instance.GetNPCStats(npc);

        /// <summary>
        /// Reads the NPC stats a stat skin grants. Works on any skin item, equipped or not.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        /// <returns>An NPCStats object.</returns>
        public static NPCStats GetNPCSkinStats(uint skin) => Instance.GetNPCSkinStats(skin);

        public static NPCStats GetNPCStatsNative(CNWSCreature npc) => Instance.GetNPCStatsNative(npc);

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        public static string GetAbilityNameShort(AbilityType type) => Instance.GetAbilityNameShort(type);

        /// <summary>
        /// Calculates the total Control for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total control for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        public static int CalculateControl(uint player, SkillType craftingSkillType)
            => Instance.CalculateControl(player, craftingSkillType);

        /// <summary>
        /// Calculates the total Craftsmanship for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total Craftsmanship for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        public static int CalculateCraftsmanship(uint player, SkillType craftingSkillType)
            => Instance.CalculateCraftsmanship(player, craftingSkillType);

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public static void LoadNPCStats() => Instance.LoadNPCStats();

        public static void LoadNPCStats(uint self) => Instance.LoadNPCStats(self);

        /// <summary>
        /// Returns the maximum HP a stat skin grants, or 0 when it grants none.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        public static int GetNPCSkinMaxHP(uint skin) => Instance.GetNPCSkinMaxHP(skin);

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        public static void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false)
            => Instance.SetNPCMaxHitPoints(creature, desiredMaxHitPoints, restoreToFull);

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        public static void RestoreNPCStats(bool outOfCombatRegen) => Instance.RestoreNPCStats(outOfCombatRegen);

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        public static void RestoreBeastStats() => Instance.RestoreBeastStats();

        public static bool IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks)
            => Instance.IsNaturalStaminaRegenerationAvailable(availableAtTicks, currentTicks);

        /// <summary>
        /// Amount of FP or STM a beast restores per heartbeat, scaled by the governing attribute
        /// (WIL for FP, MGT for STM).
        /// </summary>
        public static int GetBeastNaturalRegenAmount(int attribute) => Instance.GetBeastNaturalRegenAmount(attribute);
    }
}
