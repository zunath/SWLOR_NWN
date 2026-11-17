using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using MovementRate = SWLOR.NWN.API.NWScript.Enum.MovementRate;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Stats: the FP and Stamina pools, the player records fed by worn gear, walk mode and movement rate, and NPC stat skins.
    /// The Attack, Accuracy, Defense and Evasion ratings and the deflection and Guard chances are <c>CombatRatingService</c>'s
    /// (Combat, P5-4c); what a deflection sets off is <c>CombatDeflectionReactionService</c>'s, the healing adjustments are
    /// <c>CombatResourcesService</c>'s, and what a restore grants is the <see cref="IResourceRestoredListener"/>'s (Combat,
    /// P5-4d). The logic that used to be static in the facade <c>Stat</c> is moved here unchanged; the event handlers run on
    /// the container's instance, and static callers reach it through <c>GameServices.Stat</c>. Calls into <c>Skill</c>,
    /// <c>Perk</c>, <c>TemporaryStatModifier</c>, <c>TemporaryHitPointEffects</c>, <c>Droid</c> and the restore listener sit
    /// behind one-line <c>protected virtual</c> methods whose bodies are the original calls. The stat type metadata and the
    /// formulas the offline tools read live in <see cref="StatRules"/>, for code that has no container.
    /// </summary>
    public class StatService : IStatService
    {
        public const int BaseHP = 70;
        public const int BaseFP = 10;
        public const int BaseSTM = 10;
        private const int FPPerWillpower = 3;
        private const int StaminaPerTwoMight = 3;
        private const float DefaultPlayerMovementSpeedIncrease = 0.25f;
        private const float DefaultCompanionMovementSpeedIncrease = 0.25f;
        private const float DefaultNPCMovementSpeedIncrease = 0.30f;
        private const int MaximumNPCHitPoints = 30000;
        private const int MaximumNPCHitPointAlignmentPasses = 4;
        public const float BeastNaturalStaminaRegenDelaySeconds = 6f;
        public const int BeastNaturalRegenStatDivisor = 10;
        private const string BeastNaturalStaminaRegenAvailableAtVariable = "BEAST_STAMINA_REGEN_AVAILABLE_AT";
        public const int MaximumCombatReadinessPercent = 15;
        public const int MaximumNPCDetection = 50;
        public const float MinimumMovementSpeedMultiplier = 0f;
        public const float MaximumMovementSpeedMultiplier = 1.5f;
        private const string WalkToggleVariable = "WALK_TOGGLE";
        private const string WalkRestoreMovementRateVariable = "WALK_RESTORE_MOVEMENT_RATE";
        private const string WalkRestoreBaseMovementRateVariable = "WALK_RESTORE_BASE_MOVEMENT_RATE";
        private const string WalkRestoreMaximumMovementRateVariable = "WALK_RESTORE_MAX_MOVEMENT_RATE";
        private const float PendingWalkMovementRestore = -1.0f;
        private const float MinimumNativeMovementSpeedMultiplier = 0.125f;
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, bool> _outOfCombatMovementCombatState = new();
        private readonly Dictionary<ItemPropertyType, StatEquipmentAction> _equipmentStatActions = new();

        private readonly IDBService _db;
        private readonly ICharacterRecording _combatRecorder;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly IStatusEffectService _statusEffect;
        private readonly IMimicryTraitStatsService _mimicryTraitStats;
        private readonly ICompanionLookup _companions;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly IResourceRestoredListener _resourceRestored;

        public StatService(
            IDBService db,
            ICharacterRecording combatRecorder,
            IPlayerFeedbackService playerFeedback,
            IStatusEffectService statusEffect,
            IMimicryTraitStatsService mimicryTraitStats,
            ICompanionLookup companions,
            IPerkService perk,
            ISkillService skill,
            ITemporaryStatModifierService temporaryStatModifier,
            IResourceRestoredListener resourceRestored)
        {
            _db = db;
            _combatRecorder = combatRecorder;
            _playerFeedback = playerFeedback;
            _statusEffect = statusEffect;
            _mimicryTraitStats = mimicryTraitStats;
            _companions = companions;
            _perk = perk;
            _skill = skill;
            _temporaryStatModifier = temporaryStatModifier;
            _resourceRestored = resourceRestored;
        }

        /// <summary>
        /// Builds the stat type metadata, which announces itself on the console the first time it is read. At boot this
        /// is the first read; recorded boots print the line once, here.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            StatRules.LoadStatTypeMetadata();
        }

        public StatTypeCategory GetStatTypeCategory(StatType statType) => StatRules.GetStatTypeCategory(statType);

        public StatTypeAggregation GetStatTypeAggregation(StatType statType) => StatRules.GetStatTypeAggregation(statType);

        public DeflectionSource GetStatTypeDeflectionSource(StatType statType) => StatRules.GetStatTypeDeflectionSource(statType);

        public StatType GetGrantedDeflectionStatType(StatType sourceStatType) => StatRules.GetGrantedDeflectionStatType(sourceStatType);

        public int AggregateStatAdjustment(StatType statType, int current, int adjustment) =>
            StatRules.AggregateStatAdjustment(statType, current, adjustment);

        public bool IsBeneficialStatAdjustment(StatType statType, int value) => StatRules.IsBeneficialStatAdjustment(statType, value);

        /// <summary>
        /// Registers what equipping an item property does to its wearer. <c>EquipmentStats.RegisterStatActions</c> fills
        /// this when the module loads; until then equipping applies nothing.
        /// </summary>
        public void RegisterEquipmentStatAction(ItemPropertyType type, StatEquipmentAction action)
        {
            _equipmentStatActions[type] = action;
        }

        /// <summary>
        /// Finds what equipping an item property does to its wearer.
        /// </summary>
        public bool TryGetEquipmentStatAction(ItemPropertyType type, out StatEquipmentAction action) =>
            _equipmentStatActions.TryGetValue(type, out action);

        /// <summary>
        /// The item property types equipping adds to the wearer.
        /// </summary>
        public IReadOnlyCollection<ItemPropertyType> EquipmentStatActionTypes => _equipmentStatActions.Keys;

        public int ScaleEffect(
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
        [PlayerSession(PlayerReload.LoginOnly,
            "The movement rate and walk setting stay in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyPlayerStats()
        {
            ApplyTemporaryPlayerStats();
        }

        /// <summary>
        /// When a player enters the server, apply any temporary stats which do not persist.
        /// </summary>
        private void ApplyTemporaryPlayerStats()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            RestoreWalkMode(player);
        }

        /// <summary>
        /// The engine keeps a player's local variables across a relog until the server resets,
        /// but the forced walk set through NWNX belongs to the old creature object.
        /// Reapply it so a player who logged out walking still walks.
        /// </summary>
        public void RestoreWalkMode(uint player)
        {
            PlayerPlugin.SetAlwaysWalk(player, IsWalkModeEnabled(player));
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
        public int GetMaxFP(uint creature, Player dbPlayer = null)
        {
            var willpower = GetAbilityScore(creature, AbilityType.Willpower);
            var bonus = GetStatAdjustment(creature, StatType.MaxFP);
            int baseFP;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = _db.Get<Player>(playerId);
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

        public int GetMaxFP(int baseFP, int willpower, int bonus)
        {
            return baseFP + willpower * FPPerWillpower + bonus;
        }

        /// <summary>
        /// Retrieves the current FP on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve FP from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of FP.</returns>
        public int GetCurrentFP(uint creature, Player dbPlayer = null)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = _db.Get<Player>(playerId);
                }

                return dbPlayer.FP;
            }
            // NPCs
            else
            {
                return GetLocalInt(creature, "FP");
            }
        }

        public int GetAdjustedRequiredFP(uint creature, int requiredFP)
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
        public int GetMaxStamina(uint creature, Player dbPlayer = null)
        {
            var might = GetAbilityScore(creature, AbilityType.Might);
            var bonus = GetStatAdjustment(creature, StatType.MaxStamina);
            int baseStamina;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = _db.Get<Player>(playerId);
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

        public int GetMaxStamina(int baseStamina, int might, int bonus)
        {
            return baseStamina + might * StaminaPerTwoMight / 2 + bonus;
        }

        /// <summary>
        /// Retrieves the current STM on a creature.
        /// </summary>
        /// <param name="creature">The creature to retrieve STM from.</param>
        /// <param name="dbPlayer">The player entity. If this is not set, a call to the DB will be made. Leave null for NPCs.</param>
        /// <returns>The current amount of STM.</returns>
        public int GetCurrentStamina(uint creature, Player dbPlayer = null)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                if (dbPlayer == null)
                {
                    var playerId = GetObjectUUID(creature);
                    dbPlayer = _db.Get<Player>(playerId);
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
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of FP actually restored after modifiers and the maximum-FP cap.</returns>
        public int RestoreFP(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true)
        {
            if (amount <= 0) return 0;

            amount = ApplyFPRestoreAdjustment(creature, amount);
            if (amount <= 0) return 0;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            if (isPlayer)
            {
                dbPlayer ??= _db.Get<Player>(GetObjectUUID(creature));
                // A status timer can run before the character's persisted data is available.
                if (dbPlayer == null) return 0;
            }

            var maxFP = GetMaxFP(creature, dbPlayer);
            var restored = 0;

            // Players
            if (isPlayer)
            {
                var current = dbPlayer.FP;
                dbPlayer.FP = Math.Min(maxFP, current + amount);
                restored = Math.Max(0, dbPlayer.FP - current);

                _db.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var current = GetLocalInt(creature, "FP");
                var fp = Math.Min(maxFP, current + amount);
                restored = Math.Max(0, fp - current);

                SetLocalInt(creature, "FP", fp);
            }

            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordResourceChange(creature, CombatRecordResource.FP, restored, GetCurrentFP(creature, dbPlayer));

            ExecuteScript("pc_fp_adjusted", creature);
            if (restored > 0)
                ApplyFPRestoredEffects(creature);

            if (sendFeedback)
                _playerFeedback.SendResourceRestored(creature, restored, "FP");

            return restored;
        }

        /// <summary>
        /// Reduces a creature's FP by a specified amount.
        /// If creature would fall below 0 FP, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature whose FP will be reduced.</param>
        /// <param name="reduceBy">The amount of FP to reduce by.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        public void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordResourceReduction(creature, CombatRecordResource.FP, reduceBy, GetCurrentFP(creature, dbPlayer));

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = _db.Get<Player>(playerId);
                }

                dbPlayer.FP -= reduceBy;

                if (dbPlayer.FP < 0)
                    dbPlayer.FP = 0;

                _db.Set(dbPlayer);
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
        /// Stealth's stamina drain is its only time limit, so a stealthed player gains no Stamina
        /// from any source: natural regeneration, healing over time, field tools, or stances.
        /// </summary>
        public bool IsStaminaRestorationBlocked(bool isPlayer, bool inStealth)
        {
            return isPlayer && inStealth;
        }

        /// <summary>
        /// Restores an entity's Stamina by a specified amount.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="amount">The amount of Stamina to restore.</param>
        /// <param name="dbPlayer">The player entity to modify. If this is not set, a DB call will be made. Leave null for NPCs.</param>
        /// <param name="sendFeedback">Whether to report the actual gain in diagnostic environments. Disable for natural regeneration and rest.</param>
        /// <returns>The amount of Stamina actually restored after the maximum-Stamina cap.</returns>
        public int RestoreStamina(uint creature, int amount, Player dbPlayer = null, bool sendFeedback = true)
        {
            if (amount <= 0) return 0;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            if (IsStaminaRestorationBlocked(isPlayer, isPlayer && GetActionMode(creature, ActionMode.Stealth)))
                return 0;

            if (isPlayer)
            {
                dbPlayer ??= _db.Get<Player>(GetObjectUUID(creature));
                // A status timer can run before the character's persisted data is available.
                if (dbPlayer == null) return 0;
            }

            var maxSTM = GetMaxStamina(creature, dbPlayer);
            var restored = 0;

            // Players
            if (isPlayer)
            {
                var current = dbPlayer.Stamina;
                dbPlayer.Stamina = Math.Min(maxSTM, current + amount);
                restored = Math.Max(0, dbPlayer.Stamina - current);

                _db.Set(dbPlayer);
            }
            // NPCs
            else
            {
                var current = GetLocalInt(creature, "STAMINA");
                var fp = Math.Min(maxSTM, current + amount);
                restored = Math.Max(0, fp - current);

                SetLocalInt(creature, "STAMINA", fp);
            }

            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordResourceChange(creature, CombatRecordResource.Stamina, restored, GetCurrentStamina(creature, dbPlayer));

            ExecuteScript("pc_stm_adjusted", creature);
            if (restored > 0)
                ApplyStaminaRestoredEffects(creature);

            if (sendFeedback)
                _playerFeedback.SendResourceRestored(creature, restored, "STM");

            return restored;
        }

        /// <summary>
        /// Reduces an entity's Stamina by a specified amount.
        /// If creature would fall below 0 stamina, they will be reduced to 0 instead.
        /// </summary>
        /// <param name="creature">The creature to modify.</param>
        /// <param name="reduceBy">The amount of Stamina to reduce by.</param>
        /// <param name="dbPlayer">The entity to modify</param>
        public void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (_combatRecorder.IsRecording)
                _combatRecorder.RecordResourceReduction(creature, CombatRecordResource.Stamina, reduceBy, GetCurrentStamina(creature, dbPlayer));

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = _db.Get<Player>(playerId);
                }

                dbPlayer.Stamina -= reduceBy;

                if (dbPlayer.Stamina < 0)
                    dbPlayer.Stamina = 0;

                _db.Set(dbPlayer);
            }
            else
            {
                var currentStamina = GetLocalInt(creature, "STAMINA");
                var stamina = currentStamina;
                stamina -= reduceBy;
                if (stamina < 0)
                    stamina = 0;

                SetLocalInt(creature, "STAMINA", stamina);

                if (stamina < currentStamina && _companions.IsPlayerBeast(creature))
                {
                    var availableAt = Clock.UtcNow.AddSeconds(BeastNaturalStaminaRegenDelaySeconds);
                    SetLocalString(
                        creature,
                        BeastNaturalStaminaRegenAvailableAtVariable,
                        availableAt.Ticks.ToString(CultureInfo.InvariantCulture));
                }
            }

            ExecuteScript("pc_stm_adjusted", creature);
        }

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateStateEffect)]
        public void ReapplyFoodHP()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ReapplyFoodHP(player);
        }

        public void ReapplyFoodHP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            // Player returned after the server restarted. They no longer have the food status effect.
            // Reduce their HP by the amount tracked in the DB.
            if (dbPlayer.TemporaryFoodHP > 0 && _statusEffect.GetStatusEffect<IFoodStatusEffect>(player) == null)
            {
                AdjustPlayerMaxHP(dbPlayer, player, -dbPlayer.TemporaryFoodHP);
                dbPlayer.TemporaryFoodHP = 0;
                _db.Set(dbPlayer);
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
        public void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy)
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

            // If player's current HP is higher than max, bring it back down to their new maximum. The native
            // getter includes temporary HP, so compare base HP only or a shield would heal the player.
            var currentHP = ObjectPlugin.GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            if (currentHP > maxHP)
            {
                SetCurrentHitPoints(player, maxHP);
            }

            ScheduleTemporaryHitPointLimitCheck(player);
        }

        /// <summary>
        /// Sets a player's stored maximum HP to the sum of its sources: the base, the HP bonuses on equipped items,
        /// and active food. Equipment and food add and remove their HP as changes, so one missed, failed or repeated
        /// change leaves the record off for good. A record below the native floor of one HP per level then hides
        /// gear HP until enough is worn to climb past that floor.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <returns>True if the record was corrected</returns>
        public bool RefreshPlayerMaxHP(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) ||
                GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return false;

            // TemporaryFoodHP is the food HP the record holds. ReapplyFoodHP removes it once the food is gone.
            var maxHP = BaseHP + GetEquippedHPBonus(player) + Math.Max(0, dbPlayer.TemporaryFoodHP);
            if (dbPlayer.MaxHP == maxHP)
                return false;

            AdjustPlayerMaxHP(dbPlayer, player, maxHP - dbPlayer.MaxHP);
            _db.Set(dbPlayer);
            return true;
        }

        public int GetEquippedHPBonus(uint creature)
        {
            var amount = 0;
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item))
                    continue;

                var copies = PowerGloveStatCopies.For(GetBaseItemType(item));
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.HPBonus)
                        amount += GetItemPropertyCostTableValue(ip) * copies;
                }
            }

            return amount;
        }

        /// <summary>
        /// Sets every player record summed from worn gear (see <see cref="EquipmentRecordStats"/>) to what the worn gear
        /// grants. Like max HP, these records are kept by adding and removing each equipment change, so one missed,
        /// failed or repeated change leaves them off for good.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <param name="stats">The records to rebuild</param>
        /// <returns>The records that were corrected</returns>
        public EquipmentRecordStats RefreshPlayerEquipmentStats(uint player, EquipmentRecordStats stats = EquipmentRecordStats.All)
        {
            if (stats == EquipmentRecordStats.None ||
                !GetIsObjectValid(player) || !GetIsPC(player) ||
                GetIsDM(player) || GetIsDMPossessed(player))
                return EquipmentRecordStats.None;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return EquipmentRecordStats.None;

            var corrected = ApplyEquipmentStatTotals(dbPlayer, GetEquippedStatTotals(player), stats);
            if (corrected == EquipmentRecordStats.None)
                return corrected;

            // A lower maximum caps the current pool, as removing gear does.
            if (corrected.HasFlag(EquipmentRecordStats.FP))
                dbPlayer.FP = Math.Clamp(dbPlayer.FP, 0, Math.Max(0, GetMaxFP(player, dbPlayer)));
            if (corrected.HasFlag(EquipmentRecordStats.Stamina))
                dbPlayer.Stamina = Math.Clamp(dbPlayer.Stamina, 0, Math.Max(0, GetMaxStamina(player, dbPlayer)));
            _db.Set(dbPlayer);
            return corrected;
        }

        /// <summary>
        /// Sums the record stats the worn gear grants, counting the same properties equipping does.
        /// </summary>
        public EquipmentStatTotals GetEquippedStatTotals(uint creature)
        {
            var totals = new EquipmentStatTotals();
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item) || GetBaseItemType(item) == BaseItem.CompanionPistol)
                    continue;

                var baseItem = GetBaseItemType(item);
                var copies = PowerGloveStatCopies.For(baseItem);
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    for (var copy = 0; copy < copies; copy++)
                        totals.Add(GetItemPropertyType(ip), GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip), baseItem);
                }
            }

            return totals;
        }

        /// <summary>
        /// Writes the gear totals into the selected records: Defense and Resistance for every type, maximum FP and STM
        /// above their bases, Control, Craftsmanship and CP Bonus for every crafting skill, and each summed number
        /// such as Evasion, Attack or Stealth. Pools are left alone.
        /// </summary>
        /// <returns>The records that changed</returns>
        public EquipmentRecordStats ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats)
        {
            var corrected = EquipmentRecordStats.None;

            if (stats.HasFlag(EquipmentRecordStats.Defense))
            {
                // Legacy records can lack the map, and login can run before another handler fills it.
                entity.Defenses ??= new Dictionary<CombatDamageType, int>();
                foreach (var type in GetDefenseDamageTypes())
                {
                    var amount = totals.GetDefense(type);
                    if (entity.Defenses.TryGetValue(type, out var current) && current == amount)
                        continue;

                    entity.Defenses[type] = amount;
                    corrected |= EquipmentRecordStats.Defense;
                }
            }

            if (stats.HasFlag(EquipmentRecordStats.Evasion) && entity.Evasion != totals.Evasion)
            {
                entity.Evasion = totals.Evasion;
                corrected |= EquipmentRecordStats.Evasion;
            }

            if (stats.HasFlag(EquipmentRecordStats.FP) && entity.MaxFP != BaseFP + totals.FP)
            {
                entity.MaxFP = BaseFP + totals.FP;
                corrected |= EquipmentRecordStats.FP;
            }

            if (stats.HasFlag(EquipmentRecordStats.Stamina) && entity.MaxStamina != BaseSTM + totals.Stamina)
            {
                entity.MaxStamina = BaseSTM + totals.Stamina;
                corrected |= EquipmentRecordStats.Stamina;
            }

            if (stats.HasFlag(EquipmentRecordStats.Control))
            {
                entity.Control ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.Control, totals.GetControl))
                    corrected |= EquipmentRecordStats.Control;
            }

            if (stats.HasFlag(EquipmentRecordStats.Craftsmanship))
            {
                entity.Craftsmanship ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.Craftsmanship, totals.GetCraftsmanship))
                    corrected |= EquipmentRecordStats.Craftsmanship;
            }

            if (stats.HasFlag(EquipmentRecordStats.CPBonus))
            {
                entity.CPBonus ??= new Dictionary<SkillType, int>();
                if (ApplyCraftStatTotals(entity.CPBonus, totals.GetCPBonus))
                    corrected |= EquipmentRecordStats.CPBonus;
            }

            if (stats.HasFlag(EquipmentRecordStats.Resistance))
            {
                entity.Resistances ??= new Dictionary<ResistanceType, int>();
                foreach (var type in DamageTableDefaults.EnumerateResistanceTypes())
                {
                    var amount = totals.GetResistance(type);
                    if ((entity.Resistances.TryGetValue(type, out var current) ? current : 0) == amount)
                        continue;

                    entity.Resistances[type] = amount;
                    corrected |= EquipmentRecordStats.Resistance;
                }
            }

            foreach (var (record, get, set) in _summedEquipmentRecords)
            {
                var amount = totals.GetTotal(record);
                if (!stats.HasFlag(record) || get(entity) == amount)
                    continue;

                set(entity, amount);
                corrected |= record;
            }

            return corrected;
        }

        /// <summary>
        /// The records that hold one number summed from worn gear, with how to read and write each.
        /// </summary>
        private readonly (EquipmentRecordStats Record, Func<Player, int> Get, Action<Player, int> Set)[] _summedEquipmentRecords =
        {
            (EquipmentRecordStats.FPRegen, entity => entity.FPRegen, (entity, amount) => entity.FPRegen = amount),
            (EquipmentRecordStats.STMRegen, entity => entity.STMRegen, (entity, amount) => entity.STMRegen = amount),
            (EquipmentRecordStats.Attack, entity => entity.Attack, (entity, amount) => entity.Attack = amount),
            (EquipmentRecordStats.Accuracy, entity => entity.Accuracy, (entity, amount) => entity.Accuracy = amount),
            (EquipmentRecordStats.ForceAttack, entity => entity.ForceAttack, (entity, amount) => entity.ForceAttack = amount),
            (EquipmentRecordStats.Stealth, entity => entity.Stealth, (entity, amount) => entity.Stealth = amount),
            (EquipmentRecordStats.Detection, entity => entity.Detection, (entity, amount) => entity.Detection = amount),
            (EquipmentRecordStats.TrapBonus, entity => entity.TrapBonus, (entity, amount) => entity.TrapBonus = amount),
            (EquipmentRecordStats.TrapDisarm, entity => entity.TrapDisarm, (entity, amount) => entity.TrapDisarm = amount),
            (EquipmentRecordStats.PoisonBonus, entity => entity.PoisonBonus, (entity, amount) => entity.PoisonBonus = amount),
            (EquipmentRecordStats.Lockpicking, entity => entity.Lockpicking, (entity, amount) => entity.Lockpicking = amount),
        };

        /// <summary>
        /// Sets each crafting skill's record to what the gear grants. A missing entry already reads as zero.
        /// </summary>
        /// <returns>True if any skill changed</returns>
        private bool ApplyCraftStatTotals(Dictionary<SkillType, int> record, Func<SkillType, int> getTotal)
        {
            var changed = false;
            foreach (var skill in EquipmentStatTotals.CraftSkills)
            {
                var amount = getTotal(skill);
                if ((record.TryGetValue(skill, out var current) ? current : 0) == amount)
                    continue;

                record[skill] = amount;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player)
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
        public void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player)
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

        public bool IsWalkModeEnabled(uint creature) =>
            GetLocalInt(creature, WalkToggleVariable) == 1 &&
            (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature));

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public void ClearWalkModeBeforeDMPossessionChange()
        {
            var creature = OBJECT_SELF;
            if (GetIsDMPossessed(creature) && IsWalkModeEnabled(creature))
                ToggleWalkMode(creature);
        }

        public bool ToggleWalkMode(uint player)
        {
            var isWalking = !IsWalkModeEnabled(player);
            var isStaff = GetIsDM(player) || GetIsDMPossessed(player);
            if (isWalking && isStaff && GetLocalFloat(player, WalkRestoreMovementRateVariable) == 0f)
            {
                var calculatedMovementRate = GetMovementSpeedMultiplier(player);
                if (calculatedMovementRate <= MinimumMovementSpeedMultiplier)
                {
                    // The native disabled factor cannot establish the staff member's normal speed.
                    SetLocalFloat(player, WalkRestoreMovementRateVariable, PendingWalkMovementRestore);
                }
                else
                {
                    var nativeMovementRate = CreaturePlugin.GetMovementRateFactor(player);
                    var followsCalculatedRate = Math.Abs(nativeMovementRate -
                        Math.Max(MinimumNativeMovementSpeedMultiplier, calculatedMovementRate)) < 0.001f;
                    var baseMovementRate = followsCalculatedRate
                        ? 1.0f + GetBaseMovementSpeedIncrease(player, false)
                        : nativeMovementRate - GetMovementSpeedAdjustment(player);
                    SetLocalFloat(player, WalkRestoreMovementRateVariable, nativeMovementRate);
                    SetLocalFloat(player, WalkRestoreBaseMovementRateVariable, baseMovementRate);
                    SetLocalFloat(player, WalkRestoreMaximumMovementRateVariable,
                        followsCalculatedRate ? MaximumMovementSpeedMultiplier : float.MaxValue);
                }
            }

            SetLocalInt(player, WalkToggleVariable, isWalking ? 1 : 0);
            PlayerPlugin.SetAlwaysWalk(player, isWalking);
            if (isWalking || !isStaff || GetLocalFloat(player, WalkRestoreMovementRateVariable) != 0f)
                ApplyCreatureMovementRate(player);
            return isWalking;
        }

        public void ApplyCreatureMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return;

            if (GetLocalInt(creature, WalkToggleVariable) == 1 && !IsWalkModeEnabled(creature))
                DeleteLocalInt(creature, WalkToggleVariable);

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            if (isPlayer)
            {
                CreaturePlugin.SetMovementRate(creature, MovementRate.PC);
            }

            var movementRate = GetMovementSpeedMultiplier(creature);
            CreaturePlugin.SetMovementRateFactor(creature, movementRate);
            if (!IsWalkModeEnabled(creature) && movementRate > MinimumMovementSpeedMultiplier &&
                GetLocalFloat(creature, WalkRestoreMovementRateVariable) != 0f)
            {
                DeleteLocalFloat(creature, WalkRestoreMovementRateVariable);
                DeleteLocalFloat(creature, WalkRestoreBaseMovementRateVariable);
                DeleteLocalFloat(creature, WalkRestoreMaximumMovementRateVariable);
            }
        }

        /// <summary>
        /// Out-of-combat movement speed depends on the combat state, which has no event of its own, so a player
        /// holding that stat has their movement rate re-applied on the heartbeat that sees the state change.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public void RefreshOutOfCombatMovementRate()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if (GetStatAdjustment(player, StatType.OutOfCombatMovementSpeedPercentAdjustment) == 0)
            {
                _outOfCombatMovementCombatState.Remove(player);
                return;
            }

            var inCombat = GetIsInCombat(player);
            if (_outOfCombatMovementCombatState.TryGetValue(player, out var wasInCombat) && wasInCombat == inCombat)
                return;

            _outOfCombatMovementCombatState[player] = inCombat;
            ApplyCreatureMovementRate(player);
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets a value the next movement refresh recomputes.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ForgetOutOfCombatMovementState()
        {
            _outOfCombatMovementCombatState.Remove(GetExitingObject());
        }

        public float GetMovementSpeedMultiplier(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return 1.0f;

            if (GetStatAdjustment(creature, StatType.MovementSpeedDisabled) > 0)
                return MinimumMovementSpeedMultiplier;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            var isWalking = IsWalkModeEnabled(creature);
            var restoreMovementRate = GetLocalFloat(creature, WalkRestoreMovementRateVariable);
            var hasMovementRateRestore = restoreMovementRate > 0f;
            var baseMovementRate = hasMovementRateRestore
                ? GetLocalFloat(creature, WalkRestoreBaseMovementRateVariable)
                : 1.0f + GetBaseMovementSpeedIncrease(creature, isPlayer);
            if (isWalking)
                baseMovementRate = hasMovementRateRestore ? Math.Min(1.0f, baseMovementRate) : 1.0f;

            var movementRate = baseMovementRate + GetMovementSpeedAdjustment(creature);
            var maximumMovementRate = isWalking
                ? 1.0f
                : hasMovementRateRestore
                    ? GetLocalFloat(creature, WalkRestoreMaximumMovementRateVariable)
                    : MaximumMovementSpeedMultiplier;
            return Math.Clamp(movementRate, MinimumMovementSpeedMultiplier, maximumMovementRate);
        }

        private float GetMovementSpeedAdjustment(uint creature)
        {
            var adjustment = GetStatAdjustment(creature, StatType.MovementSpeedPercentAdjustment) * 0.01f;
            var outOfCombatAdjustment = GetStatAdjustment(creature, StatType.OutOfCombatMovementSpeedPercentAdjustment);
            if (outOfCombatAdjustment != 0 && !GetIsInCombat(creature))
                adjustment += outOfCombatAdjustment * 0.01f;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                float amount;
                if (type == EffectTypeScript.MovementSpeedIncrease)
                {
                    amount = GetEffectInteger(effect, 0) - 100;
                    adjustment += amount * 0.01f;
                }
                else if (type == EffectTypeScript.MovementSpeedDecrease)
                {
                    amount = GetEffectInteger(effect, 0);
                    adjustment -= amount * 0.01f;
                }
            }

            return adjustment;
        }

        private float GetBaseMovementSpeedIncrease(uint creature, bool isPlayer)
        {
            if (isPlayer)
                return DefaultPlayerMovementSpeedIncrease;

            if (IsDroid(creature) || _companions.IsPlayerBeast(creature))
                return DefaultCompanionMovementSpeedIncrease;

            return DefaultNPCMovementSpeedIncrease;
        }

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        public void ApplyPlayerStat(Player entity, uint player, AbilityType ability)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (ability == AbilityType.Invalid) return;

            CreaturePlugin.SetRawAbilityScore(player, ability, GetPlayerAttributeScore(entity, ability));
            if (ability == AbilityType.Vitality)
                ScheduleTemporaryHitPointLimitCheck(player);
        }

        private int GetPlayerAttributeScore(Player entity, AbilityType ability)
        {
            var racialBonus = entity.RacialStat == ability ? 1 : 0;
            return entity.BaseStats[ability] + entity.UpgradedStats[ability] + racialBonus;
        }

        public void RestorePlayerAttributes(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;

            var entity = _db.Get<Player>(GetObjectUUID(player));
            RestorePlayerAttributes(entity,
                (ability, score) => CreaturePlugin.SetRawAbilityScore(player, ability, score));
        }

        public void RestorePlayerAttributes(Player entity, Action<AbilityType, int> setAbilityScore)
        {
            // A crash can leave the character file behind the saved AP investments.
            // Assign totals so reconnecting never drops or doubles purchased points.
            foreach (var ability in entity.BaseStats.Keys)
            {
                setAbilityScore(ability, GetPlayerAttributeScore(entity, ability));
            }
        }

        /// <summary>
        /// Modifies the combat readiness of a player by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCombatReadiness(Player entity, int adjustBy)
        {
            entity.CombatReadiness += adjustBy;
        }

        public int GetCombatReadinessPercent(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return 0;

            var combatReadiness = GetStatAdjustment(creature, StatType.CombatReadinessPercent);

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                combatReadiness += dbPlayer?.CombatReadiness ?? 0;
            }
            else
            {
                combatReadiness += GetNPCStats(creature).CombatReadiness;
            }

            return Math.Clamp(combatReadiness, 0, MaximumCombatReadinessPercent);
        }

        /// <summary>
        /// Reconciles the equipment cache after login or a completed equipment change.
        /// Keep this outside ability calculations: one scan can serve every target.
        /// </summary>
        public void RefreshCombatReadinessEquipment(uint creature)
        {
            if (!GetIsObjectValid(creature) || !GetIsPC(creature) ||
                GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var playerId = GetObjectUUID(creature);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            var amount = GetEquippedCombatReadiness(creature);
            if (dbPlayer.CombatReadiness == amount)
                return;

            // Store the uncapped equipment contribution. Perks and food are stat
            // adjustments, and the combined cap belongs to the read calculation.
            dbPlayer.CombatReadiness = amount;
            _db.Set(dbPlayer);
        }

        public int GetEquippedCombatReadiness(uint creature)
        {
            var amount = 0;
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item) || GetBaseItemType(item) == BaseItem.CompanionPistol)
                    continue;

                var copies = PowerGloveStatCopies.For(GetBaseItemType(item));
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.CombatReadiness)
                        amount += GetItemPropertyCostTableValue(ip) * copies;
                }
            }

            return amount;
        }

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustHPRegen(Player entity, int adjustBy)
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
        public void AdjustFPRegen(Player entity, int adjustBy)
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
        public void AdjustSTMRegen(Player entity, int adjustBy)
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
        public void AdjustDefense(Player entity, CombatDamageType type, int adjustBy)
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
        public void AdjustEvasion(Player entity, int adjustBy)
        {
            entity.Evasion += adjustBy;
        }

        /// <summary>
        /// Modifies a player's stealth by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustStealth(Player entity, int adjustBy)
        {
            entity.Stealth += adjustBy;
        }

        /// <summary>
        /// Modifies a player's detection by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustDetection(Player entity, int adjustBy)
        {
            entity.Detection += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustTrapBonus(Player entity, int adjustBy)
        {
            entity.TrapBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's trap disarm by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustTrapDisarm(Player entity, int adjustBy)
        {
            entity.TrapDisarm += adjustBy;
        }

        /// <summary>
        /// Modifies a player's poison bonus by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPoisonBonus(Player entity, int adjustBy)
        {
            entity.PoisonBonus += adjustBy;
        }

        /// <summary>
        /// Modifies a player's lockpicking by a certain amount.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustLockpicking(Player entity, int adjustBy)
        {
            entity.Lockpicking += adjustBy;
        }

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustAttack(Player entity, int adjustBy)
        {
            entity.Attack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's accuracy granted by non-weapon equipment.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustAccuracy(Player entity, int adjustBy)
        {
            entity.Accuracy += adjustBy;
        }

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustForceAttack(Player entity, int adjustBy)
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
        public void AdjustControl(Player entity, SkillType skillType, int adjustBy)
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
        public void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy)
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
        public void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.CPBonus.ContainsKey(skillType))
                entity.CPBonus[skillType] = 0;

            entity.CPBonus[skillType] += adjustBy;
        }

        /// <summary>
        /// Retrieves a creature's detection rating, used against Stealth in the opposed stealth detection check.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The detection rating of a creature.</returns>
        public int GetDetection(uint creature)
        {
            var perception = GetAbilityScore(creature, AbilityType.Perception);
            var willpower = GetAbilityScore(creature, AbilityType.Willpower);
            var equipmentBonus = 0;

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

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

        public int CalculateDetectionRating(
            int perception,
            int willpower,
            int equipmentBonus,
            int adjustment,
            bool detectMode)
        {
            var detectModeBonus = detectMode ? 5 : 0;
            return Math.Max(0, perception + willpower + equipmentBonus + adjustment + detectModeBonus);
        }

        public int ApplyNPCDetectionCap(int detection, bool isNPC)
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
        /// <summary>
        /// Retrieves a creature's lockpicking: equipment plus perk, status, and temporary adjustments.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The lockpicking rating of a creature.</returns>
        public int GetLockpicking(uint creature)
        {
            int equipmentBonus;
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                equipmentBonus = dbPlayer.Lockpicking;
            }
            else
            {
                equipmentBonus = GetNPCSkinStat(creature, ItemPropertyType.Lockpicking);
            }

            return equipmentBonus + GetStatAdjustment(creature, StatType.Lockpicking);
        }

        public int GetStealth(uint creature)
        {
            var agility = GetAbilityScore(creature, AbilityType.Agility);
            var equipmentBonus = 0;

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

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

        public int CalculateStealthRating(int agility, int equipmentBonus, int adjustment)
        {
            return Math.Max(0, agility * 2 + equipmentBonus + adjustment);
        }

        private int GetNPCSkinStat(uint creature, ItemPropertyType type)
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

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            var perks = GetPerkStatSources(creature, payloadStat);
            var effects = _statusEffect.GetStatSources(creature, payloadStat);
            var temporary = GetTemporaryStatSources(creature, payloadStat);
            var traits = _mimicryTraitStats.GetStatSources(creature, payloadStat, this);
            var count = perks.Count + effects.Count + temporary.Count + traits.Count;
            if (count == 0) return Array.Empty<StatAdjustmentSource>();
            if (count == perks.Count) return perks;
            if (count == effects.Count) return effects;
            if (count == temporary.Count) return temporary;
            if (count == traits.Count) return traits;

            var sources = new StatAdjustmentSource[count];
            CopyStatSources(perks, sources, 0);
            CopyStatSources(effects, sources, perks.Count);
            CopyStatSources(temporary, sources, perks.Count + effects.Count);
            CopyStatSources(traits, sources, perks.Count + effects.Count + temporary.Count);
            return sources;
        }

        private void CopyStatSources(IReadOnlyList<StatAdjustmentSource> source, StatAdjustmentSource[] destination, int offset)
        {
            for (var index = 0; index < source.Count; index++)
                destination[offset + index] = source[index];
        }

        public int GetStatAdjustment(uint creature, StatType stat)
        {
            var profile = FrameProfiler.Start();
            var persistentAdjustment = GetStatAdjustmentExcludingTemporaryModifiers(creature, stat);
            var temporaryAdjustment = GetTemporaryStatAdjustment(creature, stat);
            FrameProfiler.Stop(FrameMetric.StatAdjustment, profile);

            return AggregateStatAdjustment(stat, persistentAdjustment, temporaryAdjustment);
        }

        public int GetStatAdjustmentExcludingTemporaryModifiers(uint creature, StatType stat)
        {
            var statusAdjustment = _statusEffect.GetStatAdjustment(creature, stat);
            var perkAdjustment = GetPerkStatBonus(creature, stat);
            var mimicryTraitAdjustment = _mimicryTraitStats.GetStatBonus(creature, stat, this);

            return AggregateStatAdjustment(
                stat,
                AggregateStatAdjustment(stat, statusAdjustment, perkAdjustment),
                mimicryTraitAdjustment);
        }

        private int ApplyFPRestoreAdjustment(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var adjustment = GetStatAdjustment(creature, StatType.FPRestorePercentAdjustment);
            return Math.Max(0, StatRules.ApplyPercentAdjustment(amount, adjustment));
        }

        /// <summary>
        /// Retrieves the stats of an NPC. This is determined by several item properties located on the NPC's skin.
        /// If no skin is equipped or the item properties do not exist, an empty NPCStats object will be returned.
        /// </summary>
        /// <returns>An NPCStats object.</returns>
        public NPCStats GetNPCStats(uint npc)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, npc);
            return GetIsObjectValid(skin)
                ? GetNPCSkinStats(skin)
                : new NPCStats();
        }

        /// <summary>
        /// Reads the NPC stats a stat skin grants. Works on any skin item, equipped or not.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        /// <returns>An NPCStats object.</returns>
        public NPCStats GetNPCSkinStats(uint skin)
        {
            var npcStats = new NPCStats();

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

                    npcStats.Resistances[resistanceType] += ResistanceRules.DecodeItemPropertyCostTableValue(
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
                else if (type == ItemPropertyType.Accuracy)
                {
                    npcStats.Accuracy = GetItemPropertyCostTableValue(ip);
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

        public NPCStats GetNPCStatsNative(CNWSCreature npc)
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

                        npcStats.Resistances[resistanceType] += ResistanceRules.DecodeItemPropertyCostTableValue(
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
                    else if (prop.m_nPropertyName == (ushort)ItemPropertyType.Accuracy)
                    {
                        npcStats.Accuracy = prop.m_nCostTableValue;
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
        public string GetAbilityNameShort(AbilityType type)
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
        public int CalculateControl(uint player, SkillType craftingSkillType)
        {
            var skillDetail = GetSkillDetails(craftingSkillType);
            if (!skillDetail.IsShownInCraftMenu)
                throw new ArgumentException($"Unable to calculate Control because {craftingSkillType} is not a crafting skill.");

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var control = dbPlayer.Control.ContainsKey(craftingSkillType)
                ? dbPlayer.Control[craftingSkillType]
                : 0;

            var statusBonus = _statusEffect.GetCreatureStatusEffects(player)
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
        public int CalculateCraftsmanship(uint player, SkillType craftingSkillType)
        {
            var skillDetail = GetSkillDetails(craftingSkillType);
            if (!skillDetail.IsShownInCraftMenu)
                throw new ArgumentException($"Unable to calculate Craftsmanship because {craftingSkillType} is not a crafting skill.");

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var control = dbPlayer.Craftsmanship.ContainsKey(craftingSkillType)
                ? dbPlayer.Craftsmanship[craftingSkillType]
                : 0;

            var statusBonus = _statusEffect.GetCreatureStatusEffects(player)
                .StatGroup
                .CraftSkillBonuses[CraftSkillBonusType.Craftsmanship][craftingSkillType];

            return control + statusBonus;
        }

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public void LoadNPCStats()
        {
            LoadNPCStats(OBJECT_SELF);
        }

        public void LoadNPCStats(uint self)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, self);
            var maxHP = GetNPCSkinMaxHP(skin);

            if (maxHP > 0)
            {
                SetNPCMaxHitPoints(self, maxHP, true);
            }

            SetLocalInt(self, "FP", GetMaxFP(self));
            SetLocalInt(self, "STAMINA", GetMaxStamina(self));
        }

        /// <summary>
        /// Returns the maximum HP a stat skin grants, or 0 when it grants none.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        public int GetNPCSkinMaxHP(uint skin)
        {
            var maxHP = 0;
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCHP)
                {
                    maxHP += GetItemPropertyCostTableValue(ip);
                }
            }

            return Math.Min(maxHP, MaximumNPCHitPoints);
        }

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        public void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false)
        {
            desiredMaxHitPoints = System.Math.Clamp(desiredMaxHitPoints, 1, MaximumNPCHitPoints);
            // Base HP only: the native getter includes temporary HP, which must not become base HP.
            var originalCurrentHitPoints = ObjectPlugin.GetCurrentHitPoints(creature);

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

            ScheduleTemporaryHitPointLimitCheck(creature);
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
        /// <param name="outOfCombatRegen">Also heals a tenth of maximum HP while the NPC is out of combat with nobody on its enmity table.</param>
        /// <param name="enmity">Who the NPC hates most; read only when <paramref name="outOfCombatRegen"/> is true, so other callers pass null.</param>
        public void RestoreNPCStats(bool outOfCombatRegen, IEnmityTargetQuery enmity)
        {
            RestoreNPCStats(outOfCombatRegen, false, enmity);
        }

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        public void RestoreBeastStats()
        {
            RestoreNPCStats(false, true, null);
        }

        public bool IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks)
        {
            return availableAtTicks <= 0 || currentTicks >= availableAtTicks;
        }

        /// <summary>
        /// Amount of FP or STM a beast restores per heartbeat, scaled by the governing attribute
        /// (WIL for FP, MGT for STM).
        /// </summary>
        public int GetBeastNaturalRegenAmount(int attribute)
        {
            return 1 + Math.Max(0, attribute) / BeastNaturalRegenStatDivisor;
        }

        private void RestoreNPCStats(bool outOfCombatRegen, bool isBeast, IEnmityTargetQuery enmity)
        {
            var self = OBJECT_SELF;
            if (GetLocalInt(self, SuppressNaturalRegenVariable) != 0)
                return;

            using var recorderAttribution = _combatRecorder.BeginAttribution(self, CombatRecordSourceKind.Regeneration);
            var maxFP = GetMaxFP(self);
            var maxSTM = GetMaxStamina(self);
            var previousFP = GetLocalInt(self, "FP");
            var previousSTM = GetLocalInt(self, "STAMINA");
            var fpRegen = isBeast
                ? GetBeastNaturalRegenAmount(GetAbilityScore(self, AbilityType.Willpower))
                : 1;
            var stmRegen = isBeast
                ? GetBeastNaturalRegenAmount(GetAbilityScore(self, AbilityType.Might))
                : 1;
            var fp = previousFP + fpRegen;
            var stm = previousSTM;
            var canRestoreStamina = !isBeast || CanRestoreBeastStamina(self);
            if (canRestoreStamina)
                stm += stmRegen;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, "FP", fp);
            SetLocalInt(self, "STAMINA", stm);
            if (_combatRecorder.IsRecording)
            {
                _combatRecorder.RecordResourceChange(self, CombatRecordResource.FP, fp - previousFP, fp);
                _combatRecorder.RecordResourceChange(self, CombatRecordResource.Stamina, stm - previousSTM, stm);
            }

            if (fp != previousFP)
                ExecuteScript("pc_fp_adjusted", self);
            if (stm != previousSTM)
                ExecuteScript("pc_stm_adjusted", self);

            if (outOfCombatRegen)
            {
                // If out of combat - restore HP at 10% per tick.
                if (!GetIsInCombat(self) &&
                    !GetIsObjectValid(enmity.GetHighestEnmityTarget(self)) &&
                    GetCurrentHitPoints(self) < GetMaxHitPoints(self))
                {
                    var hpToHeal = GetMaxHitPoints(self) * 0.1f;
                    ApplyEffectToObject(DurationType.Instant, EffectHeal((int)hpToHeal), self);
                }
            }
        }

        private bool CanRestoreBeastStamina(uint beast)
        {
            var availableAtValue = GetLocalString(beast, BeastNaturalStaminaRegenAvailableAtVariable);
            if (string.IsNullOrWhiteSpace(availableAtValue))
                return true;

            if (!long.TryParse(
                    availableAtValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var availableAtTicks))
            {
                DeleteLocalString(beast, BeastNaturalStaminaRegenAvailableAtVariable);
                return true;
            }

            if (!IsNaturalStaminaRegenerationAvailable(availableAtTicks, Clock.UtcNow.Ticks))
                return false;

            DeleteLocalString(beast, BeastNaturalStaminaRegenAvailableAtVariable);
            return true;
        }

        // The calls that are not injected, one line each, so a test can answer them. The bodies are the original calls. The
        // hot reads among them (the perk and temporary stat reads) add one virtual dispatch and no allocation.

        protected virtual IReadOnlyList<StatAdjustmentSource> GetPerkStatSources(uint creature, StatType payloadStat) =>
            _perk.GetStatSources(creature, payloadStat);

        protected virtual int GetPerkStatBonus(uint creature, StatType stat) => _perk.GetStatBonus(creature, stat);

        protected virtual IReadOnlyList<StatAdjustmentSource> GetTemporaryStatSources(uint creature, StatType payloadStat) =>
            _temporaryStatModifier.GetStatSources(creature, payloadStat);

        protected virtual int GetTemporaryStatAdjustment(uint creature, StatType stat) =>
            _temporaryStatModifier.GetStatAdjustment(creature, stat);

        protected virtual void ScheduleTemporaryHitPointLimitCheck(uint creature) =>
            TemporaryHitPointEffects.ScheduleLimitCheck(creature);

        protected virtual bool IsDroid(uint creature) => _companions.IsDroid(creature);

        protected virtual SkillAttribute GetSkillDetails(SkillType skillType) => _skill.GetSkillDetails(skillType);

        protected virtual ImmutableArray<CombatDamageType> GetDefenseDamageTypes() => DamageTableDefaults.GetDefenseDamageTypes();

        protected virtual void ApplyFPRestoredEffects(uint creature) => _resourceRestored.ApplyFPRestoredEffects(creature, this);

        protected virtual void ApplyStaminaRestoredEffects(uint creature) => _resourceRestored.ApplyStaminaRestoredEffects(creature, this);
    }
}
