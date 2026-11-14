using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Stats: the FP and Stamina pools, the player records fed by worn gear, walk mode and movement rate, the Attack,
    /// Accuracy, Defense and Evasion ratings, deflection and healing reactions, and NPC stat skins.
    /// </summary>
    public interface IStatService
    {
        void CacheData();

        StatTypeCategory GetStatTypeCategory(StatType statType);

        StatTypeAggregation GetStatTypeAggregation(StatType statType);

        DeflectionSource GetStatTypeDeflectionSource(StatType statType);

        StatType GetGrantedDeflectionStatType(StatType sourceStatType);

        int AggregateStatAdjustment(StatType statType, int current, int adjustment);

        bool IsBeneficialStatAdjustment(StatType statType, int value);

        /// <summary>
        /// Registers what equipping an item property does to its wearer. <c>EquipmentStats.RegisterStatActions</c> fills
        /// this when the module loads; until then equipping applies nothing.
        /// </summary>
        void RegisterEquipmentStatAction(ItemPropertyType type, StatEquipmentAction action);

        /// <summary>
        /// Finds what equipping an item property does to its wearer.
        /// </summary>
        bool TryGetEquipmentStatAction(ItemPropertyType type, out StatEquipmentAction action);

        /// <summary>
        /// The item property types equipping adds to the wearer.
        /// </summary>
        IReadOnlyCollection<ItemPropertyType> EquipmentStatActionTypes { get; }

        int ScaleEffect(
            int baseAmount,
            int primaryStat,
            float primaryRate = 0.01f,
            int secondaryStat = 0,
            float secondaryRate = 0.005f);

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        void ApplyPlayerStats();

        /// <summary>
        /// The engine keeps a player's local variables across a relog until the server resets,
        /// but the forced walk set through NWNX belongs to the old creature object.
        /// Reapply it so a player who logged out walking still walks.
        /// </summary>
        void RestoreWalkMode(uint player);

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
        int GetMaxFP(uint creature, Player dbPlayer = null);

        int GetMaxFP(int baseFP, int willpower, int bonus);

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        int GetCurrentFP(uint creature, Player dbPlayer = null);

        int GetAdjustedRequiredFP(uint creature, int requiredFP);

        /// <summary>
        /// Retrieves the maximum STM on a creature.
        /// Each point of Might grants +1.5 to max STM.
        /// </summary>
        /// <param name="creature">The creature object</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The max amount of STM</returns>
        int GetMaxStamina(uint creature, Player dbPlayer = null);

        int GetMaxStamina(int baseStamina, int might, int bonus);

        /// <summary>
        /// Retrieves the current STM on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve STM from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of STM.</returns>
        int GetCurrentStamina(uint creature, Player dbPlayer = null);

        /// <summary>
        /// Restores a creature's FP by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of FP to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of FP actually restored after modifiers and the maximum-FP cap.</returns>
        int RestoreFP(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true);

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null);

        /// <summary>
        /// Stealth's stamina drain is its only time limit, so a stealthed player gains no Stamina
        /// from any source: natural regeneration, healing over time, field tools, or stances.
        /// </summary>
        bool IsStaminaRestorationBlocked(bool isPlayer, bool inStealth);

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of Stamina actually restored after the maximum-Stamina cap.</returns>
        int RestoreStamina(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true);

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null);

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        void ReapplyFoodHP();

        void ReapplyFoodHP(uint player);

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 40 by default, so this
        /// gives 255 * 40 = 10,200 HP maximum. If the player's HP would go over this amount, it will be set to 10,200.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy);

        /// <summary>
        /// Sets a player's stored maximum HP to the sum of its sources: the base, the HP bonuses on equipped items,
        /// and active food. Equipment and food add and remove their HP as changes, so one missed, failed or repeated
        /// change leaves the record off for good. A record below the native floor of one HP per level then hides
        /// gear HP until enough is worn to climb past that floor.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <returns>True if the record was corrected</returns>
        bool RefreshPlayerMaxHP(uint player);

        int GetEquippedHPBonus(uint creature);

        /// <summary>
        /// Sets every player record summed from worn gear (see <see cref="EquipmentRecordStats"/>) to what the worn gear
        /// grants. Like max HP, these records are kept by adding and removing each equipment change, so one missed,
        /// failed or repeated change leaves them off for good.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <param name="stats">The records to rebuild</param>
        /// <returns>The records that were corrected</returns>
        EquipmentRecordStats RefreshPlayerEquipmentStats(uint player, EquipmentRecordStats stats = EquipmentRecordStats.All);

        /// <summary>
        /// Sums the record stats the worn gear grants, counting the same properties equipping does.
        /// </summary>
        EquipmentStatTotals GetEquippedStatTotals(uint creature);

        /// <summary>
        /// Writes the gear totals into the selected records: Defense and Resistance for every type, maximum FP and STM
        /// above their bases, Control, Craftsmanship and CP Bonus for every crafting skill, and each summed number
        /// such as Evasion, Attack or Stealth. Pools are left alone.
        /// </summary>
        /// <returns>The records that changed</returns>
        EquipmentRecordStats ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats);

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player);

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player);

        bool IsWalkModeEnabled(uint creature);

        void ClearWalkModeBeforeDMPossessionChange();

        bool ToggleWalkMode(uint player);

        void ApplyCreatureMovementRate(uint creature);

        /// <summary>
        /// Out-of-combat movement speed depends on the combat state, which has no event of its own, so a player
        /// holding that stat has their movement rate re-applied on the heartbeat that sees the state change.
        /// </summary>
        void RefreshOutOfCombatMovementRate();

        void ForgetOutOfCombatMovementState();

        float GetMovementSpeedMultiplier(uint creature);

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        void ApplyPlayerStat(Player entity, uint player, AbilityType ability);

        void RestorePlayerAttributes(uint player);

        void RestorePlayerAttributes(Player entity, Action<AbilityType, int> setAbilityScore);

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCombatReadiness(Player entity, int adjustBy);

        int GetCombatReadinessPercent(uint creature);

        /// <summary>
        /// Reconciles the equipment cache after login or a completed equipment change.
        /// Keep this outside ability calculations: one scan can serve every target.
        /// </summary>
        void RefreshCombatReadinessEquipment(uint creature);

        int GetEquippedCombatReadiness(uint creature);

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustHPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustFPRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustSTMRegen(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustDefense(Player entity, CombatDamageType type, int adjustBy);

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustEvasion(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustStealth(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustDetection(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustTrapBonus(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustTrapDisarm(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustPoisonBonus(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustLockpicking(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's accuracy granted by non-weapon equipment.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustAccuracy(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustForceAttack(Player entity, int adjustBy);

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustControl(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy);

        /// <summary>
        /// Calculates the attack for a given creature.
        /// </summary>
        /// <param name="creature">The creature to calculate.</param>
        /// <param name="abilityType">The type of ability to use.</param>
        /// <param name="skillType">The type of skill to use.</param>
        /// <param name="attackBonusOverride">Overrides the attack bonus granted by equipment. Usually only used by Space combat.</param>
        /// <returns>The total Attack value of a creature.</returns>
        int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0);

        int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid,
            bool useForceAttack = false, SkillType skillOverride = SkillType.Invalid);

        /// <summary>
        /// Retrieves the raw attack based on the level, stat, and any bonuses.
        /// </summary>
        /// <param name="level">The level (NPC or skill)</param>
        /// <param name="stat">The raw stat points</param>
        /// <param name="bonus">The amount of bonus attack or force attack</param>
        /// <returns></returns>
        int GetAttack(int level, int stat, int bonus);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// Physical and Force equipment bonuses live here; elemental/status mitigation lives in resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <param name="defenseBonusOverride">Overrides the defense bonus granted by equipment. Usually only used for Space combat.</param>
        /// <returns>The defense value toward a given damage type.</returns>
        int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0);

        int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        int GetStatValueNative(CNWSCreature creature, AbilityType statType);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// This is specifically for use with Native code and should not be referenced outside of there.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <returns>The defense value toward a given damage type.</returns>
        int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType);

        int GetAttackPercentAdjustment(uint creature, SkillType skillType);

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
        int GetAccuracy(
            uint creature,
            uint weapon,
            AbilityType statOverride,
            SkillType skillOverride,
            int skillLevelOverride = -1,
            bool ignoreWeaponAccuracyStatOverride = false);

        /// <summary>
        /// Retrieves the accuracy rating of a creature from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="weapon">The weapon being used.</param>
        /// <returns>The accuracy rating for a creature using a specific weapon.</returns>
        int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, SkillType skillOverride = SkillType.Invalid,
            AbilityType accuracyAbilityOverride = AbilityType.Invalid);

        /// <summary>
        /// Gets the calculated accuracy for a given level, stat, and bonus.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw accuracy stat amount</param>
        /// <param name="bonus">The amount of bonus accuracy.</param>
        /// <returns>The calculated accuracy result.</returns>
        int GetAccuracy(int level, int stat, int bonus);

        /// <summary>
        /// Retrieves a creature's evasion.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="skillOverride">The skill override to use instead of Armor for the purposes of calculating evasion.</param>
        /// <param name="incomingSkillType">The skill type of the incoming attack for conditional evasion modifiers.</param>
        /// <returns>The evasion rating of a creature.</returns>
        int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid);

        /// <summary>
        /// Retrieves a creature's detection rating, used against Stealth in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The detection rating of a creature.</returns>
        int GetDetection(uint creature);

        int CalculateDetectionRating(
            int perception,
            int willpower,
            int equipmentBonus,
            int adjustment,
            bool detectMode);

        int ApplyNPCDetectionCap(int detection, bool isNPC);

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
        int GetLockpicking(uint creature);

        int GetStealth(uint creature);

        int CalculateStealthRating(int agility, int equipmentBonus, int adjustment);

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid);

        int GetMeleeDeflectionChanceNative(CNWSCreature creature);

        int GetMeleeDeflectionChance(uint creature);

        int GetRangedDeflectionChanceNative(CNWSCreature creature);

        int GetRangedDeflectionChance(uint creature);

        int GetShieldDeflectionChanceNative(CNWSCreature creature);

        int GetShieldDeflectionChance(uint creature);

        int GetGuardChance(uint creature);

        void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source);

        /// <summary>
        /// Retrieves the total percentage adjustment applied to a creature's defense for a damage type.
        /// This combines the general defense adjustment with the type-specific adjustment, including
        /// the shield-only bonus when a shield is equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="type">The damage type.</param>
        /// <returns>The percentage adjustment applied to defense.</returns>
        int GetDefensePercentAdjustment(uint creature, CombatDamageType type);

        IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat);

        int GetStatAdjustment(uint creature, StatType stat);

        int GetStatAdjustmentExcludingTemporaryModifiers(uint creature, StatType stat);

        int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount);

        int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment);

        /// <param name="isInstantAbilityHeal">True for a heal an ability lands when it resolves. Only these can restore Stamina through the healing-received trigger.</param>
        int ApplyHealingReceivedAdjustment(uint creature, int amount, bool applyReceivedEffects = true, bool isInstantAbilityHeal = false);

        int CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP);

        /// <param name="isInstantAbilityHeal">Heals over time, lifesteal, drains and regeneration pass false and never restore Stamina.</param>
        void ApplyHealingReceivedEffects(uint creature, int amount, bool isInstantAbilityHeal = false);

        /// <summary>
        /// Gets the evasion based on level, stat, and bonuses.
        /// </summary>
        /// <param name="level">The level (skill/NPC)</param>
        /// <param name="stat">The raw agility stat</param>
        /// <param name="bonus">The amount of bonus evasion</param>
        /// <returns></returns>
        int GetEvasion(int level, int stat, int bonus);

        /// <summary>
        /// NPC stat skins carry the complete Evasion budget. Native armor must
        /// not add another budget through inherited equipment, feats or effects.
        /// Stat-based Evasion adjustments remain part of bonus for all creatures.
        /// </summary>
        int GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget);

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        NPCStats GetNPCStats(uint npc);

        /// <summary>
        /// Reads the NPC stats a stat skin grants. Works on any skin item, equipped or not.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        /// <returns>An NPCStats object.</returns>
        NPCStats GetNPCSkinStats(uint skin);

        NPCStats GetNPCStatsNative(CNWSCreature npc);

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        string GetAbilityNameShort(AbilityType type);

        /// <summary>
        /// Calculates the total Control for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total control for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        int CalculateControl(uint player, SkillType craftingSkillType);

        /// <summary>
        /// Calculates the total Craftsmanship for a player in a given crafting skill.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="craftingSkillType">The skill to check</param>
        /// <returns>The total Craftsmanship for a player</returns>
        /// <exception cref="ArgumentException">Thrown if a non-crafting skill is passed in.</exception>
        int CalculateCraftsmanship(uint player, SkillType craftingSkillType);

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        void LoadNPCStats();

        void LoadNPCStats(uint self);

        /// <summary>
        /// Returns the maximum HP a stat skin grants, or 0 when it grants none.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        int GetNPCSkinMaxHP(uint skin);

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false);

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        void RestoreNPCStats(bool outOfCombatRegen);

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        void RestoreBeastStats();

        bool IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks);

        /// <summary>
        /// Amount of FP or STM a beast restores per heartbeat, scaled by the governing attribute
        /// (WIL for FP, MGT for STM).
        /// </summary>
        int GetBeastNaturalRegenAmount(int attribute);
    }
}
