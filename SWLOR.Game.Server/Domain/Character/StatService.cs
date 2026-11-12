using System.Collections.Generic;
using System.Globalization;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
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
    /// Stats: the FP and Stamina pools, the player records fed by worn gear, walk mode and movement rate, the Attack,
    /// Accuracy, Defense and Evasion ratings, deflection and healing reactions, and NPC stat skins. The logic that used to
    /// be static in the facade <c>Stat</c> is moved here unchanged; the event handlers run on the container's instance,
    /// and static callers reach it through <c>GameServices.Stat</c>. Calls into <c>Combat</c>, <c>Skill</c>, <c>Perk</c>,
    /// <c>TemporaryStatModifier</c>, <c>TemporaryHitPointEffects</c> and <c>Droid</c> sit behind one-line
    /// <c>protected virtual</c> methods whose bodies are the original calls. The stat type metadata and the formulas the
    /// offline tools read live in <see cref="StatRules"/>, for code that has no container.
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
        public const int DefaultMeleeDeflectionChanceCap = 50;
        public const int DefaultRangedDeflectionChanceCap = 50;
        public const int MaximumDeflectionChanceCap = 100;
        public const int MaximumShieldDeflectionChance = 75;
        public const int MaximumGuardChance = 100;
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
        private const float DeflectionEvasionBoostDurationSeconds = 30f;
        private const float DeflectionEnmityBoostDurationSeconds = 30f;
        private const float DeflectionDefenseBoostDurationSeconds = 30f;
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, bool> _outOfCombatMovementCombatState = new();
        private readonly Dictionary<ItemPropertyType, StatEquipmentAction> _equipmentStatActions = new();

        private readonly IDBService _db;
        private readonly ICombatRecorderService _combatRecorder;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly IStatusEffectService _statusEffect;
        private readonly IMimicryService _mimicry;
        private readonly ITwinPistolEquipmentService _twinPistolEquipment;
        private readonly IPowerGloveEquipmentService _powerGloveEquipment;
        private readonly IItemService _item;
        private readonly IRecastService _recast;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IEnmityService _enmity;
        private readonly IResistanceService _resistance;
        private readonly IPvpCombatService _pvpCombat;
        private readonly IPerkService _perk;
        private readonly ISkillService _skill;
        private readonly ITemporaryStatModifierService _temporaryStatModifier;
        private readonly Lazy<ICombatAttackTimingService> _attackTiming;
        private readonly ICombatDamageCatalogService _damageCatalog;
        private readonly Lazy<ICombatDamageModifiersService> _damageModifiers;
        private readonly Lazy<ICombatDamageTakenService> _damageTaken;
        private readonly Lazy<ICombatQueriesService> _queries;
        private readonly Lazy<ICombatResourcesService> _resources;

        private readonly Lazy<IDroidService> _droid;

        public StatService(
            IDBService db,
            ICombatRecorderService combatRecorder,
            IPlayerFeedbackService playerFeedback,
            IStatusEffectService statusEffect,
            IMimicryService mimicry,
            ITwinPistolEquipmentService twinPistolEquipment,
            IPowerGloveEquipmentService powerGloveEquipment,
            IItemService item,
            IRecastService recast,
            IBeastMasteryService beastMastery,
            IEnmityService enmity,
            IResistanceService resistance,
            IPvpCombatService pvpCombat,
            IPerkService perk,
            ISkillService skill,
            ITemporaryStatModifierService temporaryStatModifier,
            Lazy<ICombatAttackTimingService> attackTiming,
            ICombatDamageCatalogService damageCatalog,
            Lazy<ICombatDamageModifiersService> damageModifiers,
            Lazy<ICombatDamageTakenService> damageTaken,
            Lazy<ICombatQueriesService> queries,
            Lazy<ICombatResourcesService> resources,
            Lazy<IDroidService> droid)
        {
            _droid = droid;
            _db = db;
            _combatRecorder = combatRecorder;
            _playerFeedback = playerFeedback;
            _statusEffect = statusEffect;
            _mimicry = mimicry;
            _twinPistolEquipment = twinPistolEquipment;
            _powerGloveEquipment = powerGloveEquipment;
            _item = item;
            _recast = recast;
            _beastMastery = beastMastery;
            _enmity = enmity;
            _resistance = resistance;
            _pvpCombat = pvpCombat;
            _perk = perk;
            _skill = skill;
            _temporaryStatModifier = temporaryStatModifier;
            _attackTiming = attackTiming;
            _damageCatalog = damageCatalog;
            _damageModifiers = damageModifiers;
            _damageTaken = damageTaken;
            _queries = queries;
            _resources = resources;
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

                if (stamina < currentStamina && _beastMastery.IsPlayerBeast(creature))
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
            if (dbPlayer.TemporaryFoodHP > 0 && _statusEffect.GetStatusEffect<FoodStatusEffect>(player) == null)
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

                var copies = _powerGloveEquipment.GetEquipmentStatCopies(GetBaseItemType(item));
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
                var copies = _powerGloveEquipment.GetEquipmentStatCopies(baseItem);
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
                foreach (var type in ResistanceRules.EnumerateResistanceTypes())
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

            if (IsDroid(creature) || _beastMastery.IsPlayerBeast(creature))
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

                var copies = _powerGloveEquipment.GetEquipmentStatCopies(GetBaseItemType(item));
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

        private int CalculateEffectAttack(uint creature, int attack)
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
        public int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0)
        {
            if (attackBonusOverride < 0)
                attackBonusOverride = 0;

            var attackBonus = 0 + attackBonusOverride;
            var skillLevel = 0;
            var stat = GetAbilityScore(creature, abilityType);

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

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

        public int GetAttackNative(CNWSCreature creature, BaseItem itemType, AbilityType statOverride = AbilityType.Invalid,
            bool useForceAttack = false, SkillType skillOverride = SkillType.Invalid)
        {
            var attackBonus = 0;
            var skillLevel = 0;
            var statType = statOverride != AbilityType.Invalid
                ? statOverride
                : GetWeaponDamageAbilityType(creature.m_idSelf, itemType);
            var stat = GetStatValueNative(creature, statType);
            var skillType = skillOverride != SkillType.Invalid ? skillOverride :
                _twinPistolEquipment.IsActivePair(creature.m_idSelf)
                    ? _twinPistolEquipment.ResolveSkill(itemType,
                        GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, creature.m_idSelf)),
                        GetBaseItemType(GetItemInSlot(InventorySlot.LeftHand, creature.m_idSelf)))
                    : GetSkillTypeByBaseItem(itemType);

            // Force-typed attacks (e.g. Imbuement Stance retyping a weapon swing to Force) use the
            // wearer's Force Attack in place of physical Attack so the attack side matches the Force
            // Defense the hit is mitigated against. The weapon's own skill rank still governs the roll.
            var usesForceAttack = useForceAttack || skillType == SkillType.Force;

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

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
        public int GetAttack(int level, int stat, int bonus) => StatRules.GetAttack(level, stat, bonus);

        /// <summary>
        /// Retrieves the defense value used by the attack-vs-defense damage roll.
        /// Physical and Force equipment bonuses live here; elemental/status mitigation lives in resistance.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <param name="type">The type of damage to retrieve.</param>
        /// <param name="abilityType"></param>
        /// <param name="defenseBonusOverride">Overrides the defense bonus granted by equipment. Usually only used for Space combat.</param>
        /// <returns>The defense value toward a given damage type.</returns>
        public int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0)
        {
            if (defenseBonusOverride < 0)
                defenseBonusOverride = 0;

            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetAbilityScore(creature, abilityType);
            int skillLevel;
            var defenseBonus = 0;
            var equipmentDefense = defenseBonusOverride;

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

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

        public int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus) =>
            StatRules.CalculateDefense(defenderStat, skillLevel, defenseBonus);

        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        public int GetStatValueNative(CNWSCreature creature, AbilityType statType)
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
        public int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType)
        {
            var defenseType = type.GetDefenseDamageType();
            var defenderStat = GetStatValueNative(creature, abilityType);
            var skillLevel = 0;
            var defenseBonus = 0;
            var equipmentDefense = 0;

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

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

        private int ApplyPostAttackStatusModifiers(uint creature, int attack, SkillType skillType)
        {
            var adjustment = GetAttackPercentAdjustment(creature, skillType);

            adjustment += GetHighFPAndStaminaAttackAdjustment(creature);
            adjustment += GetNearbyStatusTargetAttackAdjustment(creature);
            adjustment += GetLowHPAttackAdjustment(creature);
            adjustment += GetLowFPAttackAdjustment(creature);
            return Math.Max(1, ApplyPercentAdjustment(attack, adjustment));
        }

        public int GetAttackPercentAdjustment(uint creature, SkillType skillType)
        {
            return GetStatAdjustment(creature, StatType.AttackPercentAdjustment) +
                   (skillType == SkillType.Force
                       ? GetStatAdjustment(creature, StatType.ForceAttackPercentAdjustment)
                       : 0);
        }

        private int GetHighFPAndStaminaAttackAdjustment(uint creature)
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
        public int GetAccuracy(
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
                if (type != ItemPropertyType.Accuracy &&
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
                GetWeaponAccuracyAbilityType(creature, baseItemType) :
                statOverride;
            var stat = statType == AbilityType.Invalid ? 0 : GetAbilityScore(creature, statType);
            var skillType = skillOverride == SkillType.Invalid ? GetWeaponSkill(creature, weapon) : skillOverride;
            var skillLevel = 0;


            // Creature skill level / NPC level, plus Accuracy granted by non-weapon equipment.
            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (skillType != SkillType.Invalid)
                    skillLevel = dbPlayer.Skills[skillType].Rank;

                accuracyBonus += dbPlayer.Accuracy;
            }
            else
            {
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
                accuracyBonus += npcStats.Accuracy;
            }

            if (skillLevelOverride >= 0)
                skillLevel = skillLevelOverride;

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

        private (AbilityType StatOverride, int AccuracyBonus) ApplyAccuracyItemProperty(
            AbilityType statOverride,
            int accuracyBonus,
            ItemPropertyType type,
            int value,
            bool ignoreWeaponAccuracyStatOverride)
        {
            if (type == ItemPropertyType.Accuracy)
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
        public int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, SkillType skillOverride = SkillType.Invalid,
            AbilityType accuracyAbilityOverride = AbilityType.Invalid)
        {
            var accuracyBonus = 0;
            var statOverride = AbilityType.Invalid;

            if (weapon != null)
            {
                foreach (var ip in weapon.m_lstPassiveProperties)
                {
                    // Accuracy found on the weapon.
                    if (ip.m_nPropertyName == (ushort)ItemPropertyType.Accuracy)
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
            var statType = accuracyAbilityOverride != AbilityType.Invalid
                ? accuracyAbilityOverride
                : statOverride == AbilityType.Invalid
                    ? GetWeaponAccuracyAbilityType(creature.m_idSelf, baseItemType)
                    : statOverride;
            var skillType = skillOverride != SkillType.Invalid ? skillOverride :
                weapon == null ? SkillType.Invalid : GetWeaponSkill(creature.m_idSelf, weapon.m_idSelf);
            var stat = GetStatValueNative(creature, statType);
            var skillLevel = 0;


            // Creature skill level / NPC level, plus Accuracy granted by non-weapon equipment.
            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

                if (dbPlayer != null)
                {
                    if (skillType != SkillType.Invalid)
                        skillLevel = dbPlayer.Skills[skillType].Rank;

                    accuracyBonus += dbPlayer.Accuracy;
                }
            }
            else
            {
                var npcStats = GetNPCStatsNative(creature);
                skillLevel = npcStats.Level;
                accuracyBonus += npcStats.Accuracy;
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
        public int GetAccuracy(int level, int stat, int bonus) => StatRules.GetAccuracy(level, stat, bonus);

        private int CalculateEffectAccuracy(uint creature, int accuracy)
        {
            accuracy += GetStatAdjustment(creature, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private int CalculateEffectAccuracyNative(CNWSCreature creature, int accuracy)
        {
            accuracy += GetStatAdjustment(creature.m_idSelf, StatType.Accuracy);

            Log.Write(LogGroup.Attack, $"Native Effect Accuracy: {accuracy}");

            return accuracy;
        }

        private int CalculateEffectEvasion(uint creature)
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
        public int GetEvasion(uint creature, SkillType skillOverride, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetAbilityScore(creature, AbilityType.Agility);
            int skillLevel;
            int evasionBonus;
            var hasNpcStatBudget = false;

            // Base NWN applies an AC bonus based on the DEX stat. The Perception stat is based upon this.
            // Perception should not increase AC in SWLOR, so this is subtracted from the AC.
            var dexOffset = GetAbilityModifier(AbilityType.Perception, creature);
            var ac = GetAC(creature) - dexOffset - 10; // Offset by natural 10 AC granted to all characters.
            var skillType = skillOverride == SkillType.Invalid ? SkillType.Armor : skillOverride;

            Log.Write(LogGroup.Attack, $"Evasion regular AC = {ac}");

            if (GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                skillLevel = dbPlayer.Skills[skillType].Rank;
                evasionBonus = dbPlayer.Evasion;
            }
            else
            {
                var npcStats = GetNPCStats(creature);
                skillLevel = npcStats.Level;
                evasionBonus = npcStats.Evasion;
                hasNpcStatBudget = npcStats.Level > 0;
            }

            evasionBonus += CalculateEffectEvasion(creature);

            Log.Write(LogGroup.Attack, $"Effect Evasion: {evasionBonus}");

            var evasion = GetEvasion(skillLevel, stat, evasionBonus, ac, hasNpcStatBudget);
            return ApplyPostEvasionStatusModifiers(creature, evasion, incomingSkillType);
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

        /// <summary>
        /// Retrieves a creature's evasion rating from a native context.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>The evasion rating of a creature.</returns>
        public int GetEvasionNative(CNWSCreature creature, SkillType incomingSkillType = SkillType.Invalid)
        {
            var stat = GetStatValueNative(creature, AbilityType.Agility);
            var skillLevel = 0;
            var evasionBonus = 0;
            var hasNpcStatBudget = false;

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

            if (creature.m_bPlayerCharacter == 1 && !GetIsDMPossessed(creature.m_idSelf))
            {
                var playerId = creature.m_pUUID.GetOrAssignRandom().ToString();
                var dbPlayer = _db.Get<Player>(playerId);

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
                hasNpcStatBudget = npcStats.Level > 0;
            }

            evasionBonus += CalculateEffectEvasion(creature.m_idSelf);

            var evasion = GetEvasion(skillLevel, stat, evasionBonus, ac, hasNpcStatBudget);
            return ApplyPostEvasionStatusModifiers(creature.m_idSelf, evasion, incomingSkillType);
        }

        public int GetMeleeDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = GetStatAdjustment(creature.m_idSelf, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature.m_idSelf));
        }

        public int GetMeleeDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = GetStatAdjustment(creature, StatType.MeleeDeflection);
            return Math.Clamp(chance, 0, GetMeleeDeflectionChanceCap(creature));
        }

        public int GetRangedDeflectionChanceNative(CNWSCreature creature)
        {
            if (!HasWeaponEquippedForWeaponDeflectionNative(creature) || HasShieldEquippedNative(creature))
                return 0;

            var chance = GetStatAdjustment(creature.m_idSelf, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature.m_idSelf));
        }

        public int GetRangedDeflectionChance(uint creature)
        {
            if (!HasWeaponEquippedForWeaponDeflection(creature) || HasShieldEquipped(creature))
                return 0;

            var chance = GetStatAdjustment(creature, StatType.RangedDeflection);
            return Math.Clamp(chance, 0, GetRangedDeflectionChanceCap(creature));
        }

        public int GetShieldDeflectionChanceNative(CNWSCreature creature)
        {
            var shield = GetEquippedShieldNative(creature);
            if (shield == null)
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonusNative(shield) +
                         GetStatAdjustment(creature.m_idSelf, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public int GetShieldDeflectionChance(uint creature)
        {
            var shield = GetEquippedShield(creature);
            if (!GetIsObjectValid(shield))
                return 0;

            var chance = GetShieldDeflectionItemPropertyBonus(shield) +
                         GetStatAdjustment(creature, StatType.ShieldDeflection);
            return Math.Clamp(chance, 0, MaximumShieldDeflectionChance);
        }

        public int GetGuardChance(uint creature)
        {
            return Math.Clamp(GetStatAdjustment(creature, StatType.Guard), 0, MaximumGuardChance);
        }

        public void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source)
        {
            var creatureId = GetNativeCreatureId(creature);
            TrackDeflection(creatureId, source);

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
            var recastReductionCooldown = GetDeflectionStatAdjustment(
                creatureId,
                StatType.DeflectionRecastReductionCooldownSeconds,
                source);
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
                TryUseStatTrigger(creatureId, staminaRestoreStat, staminaRestoreCooldown))
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, staminaRestoreStat);
                RestoreStamina(creatureId, staminaRestore);
            }

            if (fpRestore > 0 &&
                TryUseStatTrigger(creatureId, fpRestoreStat, fpRestoreCooldown))
            {
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, fpRestoreStat);
                RestoreFP(creatureId, fpRestore);
            }

            if (staminaRestorePercent > 0)
            {
                var amount = GameMath.PercentOf(GetMaxStamina(creatureId), staminaRestorePercent);
                using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creatureId, StatType.DeflectionStaminaRestorePercent);
                RestoreStamina(creatureId, amount);
            }

            if (evasionBoost != 0 || evasionEnmityBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EvasionPercentAdjustment,
                    evasionBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    evasionEnmityBoost,
                    DeflectionEvasionBoostDurationSeconds,
                    StatType.DeflectionEvasionPercentAdjustment);
            }

            if (enmityBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.EnmityPercentAdjustment,
                    enmityBoost,
                    DeflectionEnmityBoostDurationSeconds,
                    StatType.DeflectionEnmityPercentAdjustment);
            }

            if (defenseBoost != 0 || forceDefenseBoost != 0)
            {
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.PhysicalDefensePercentAdjustment,
                    defenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
                ReplaceTemporaryStatModifier(
                    creatureId,
                    StatType.ForceDefensePercentAdjustment,
                    forceDefenseBoost,
                    DeflectionDefenseBoostDurationSeconds,
                    StatType.DeflectionDefensePercentAdjustment);
            }

            // Rate limited like every other deflection payload: the trigger fires once per
            // incoming attack, so without a gate the reduction outpaces the cooldown it shortens
            // as soon as a second attacker joins. The gate is only spent when there is actually a
            // cooldown to shorten, so deflecting while the ability is ready does not burn the
            // window that the next real cooldown needs.
            if (recastReductionGroup != RecastGroup.Invalid && recastReductionSeconds > 0)
            {
                var (isOnRecastDelay, _) = _recast.IsOnRecastDelay(creatureId, recastReductionGroup);
                if (isOnRecastDelay &&
                    TryUseStatTrigger(
                        creatureId,
                        StatType.DeflectionRecastReductionSeconds,
                        recastReductionCooldown))
                {
                    _recast.ReduceRecastDelay(creatureId, recastReductionGroup, recastReductionSeconds);
                }
            }

            GrantNextSkillAbilityBonuses(
                creatureId,
                nextSkillAbilitySkillType,
                nextSkillAbilityDamageBonus,
                nextSkillAbilityCriticalRate,
                Math.Max(nextSkillAbilityDamageWindow, nextSkillAbilityCriticalWindow));

            GrantNextAutoAttackCriticalRateBonus(
                creatureId,
                nextAutoAttackCriticalRateSkillType,
                nextAutoAttackCriticalRate,
                nextAutoAttackCriticalRateWindow);

            if (nextSkillAbilityNoDelay > 0)
            {
                GrantNextAbilityNoDelay(
                    creatureId,
                    nextSkillAbilitySkillType,
                    nextSkillAbilityNoDelayWindow);
            }
        }

        private RecastGroup GetRecastGroupFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(RecastGroup), value)
                ? (RecastGroup)value
                : RecastGroup.Invalid;
        }

        private int GetMeleeDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.MeleeDeflectionChanceCap,
                DefaultMeleeDeflectionChanceCap);
        }

        private int GetRangedDeflectionChanceCap(uint creature)
        {
            return GetDeflectionChanceCap(
                creature,
                StatType.RangedDeflectionChanceCap,
                DefaultRangedDeflectionChanceCap);
        }

        private int GetDeflectionChanceCap(uint creature, StatType capStat, int defaultCap)
        {
            var cap = defaultCap + GetStatAdjustment(creature, capStat);
            return Math.Clamp(cap, defaultCap, MaximumDeflectionChanceCap);
        }

        private int GetDeflectionStatAdjustment(uint creature, StatType statType, DeflectionSource source)
        {
            return GetStatTypeDeflectionSource(statType) == source
                ? GetStatAdjustment(creature, statType)
                : 0;
        }

        private StatType GetDeflectionStatTypeForSource(
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

        private bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature)
        {
            return HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflectionNative(creature, EquipmentSlot.LeftHand);
        }

        private bool HasWeaponEquippedForWeaponDeflectionNative(CNWSCreature creature, EquipmentSlot slot)
        {
            var item = creature.m_pInventory.GetItemInSlot((uint)slot);
            return item != null &&
                   GetWeaponSkill(creature.m_idSelf, item.m_idSelf) != SkillType.Invalid;
        }

        private bool HasWeaponEquippedForWeaponDeflection(uint creature)
        {
            return HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.RightHand) ||
                   HasWeaponEquippedForWeaponDeflection(creature, InventorySlot.LeftHand);
        }

        private bool HasWeaponEquippedForWeaponDeflection(uint creature, InventorySlot slot)
        {
            var item = GetItemInSlot(slot, creature);
            return GetIsObjectValid(item) &&
                   GetWeaponSkill(creature, item) != SkillType.Invalid;
        }

        private SkillType GetMainHandSkillTypeNative(CNWSCreature creature)
        {
            var rightHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
            if (rightHandItem == null)
                return SkillType.Invalid;

            return GetWeaponSkill(creature.m_idSelf, rightHandItem.m_idSelf);
        }

        private SkillType GetSkillTypeFromStat(int value)
        {
            return value > 0 && Enum.IsDefined(typeof(SkillType), value)
                ? (SkillType)value
                : SkillType.Invalid;
        }

        private bool HasShieldEquippedNative(CNWSCreature creature)
        {
            return GetEquippedShieldNative(creature) != null;
        }

        private bool HasShieldEquipped(uint creature)
        {
            return GetIsObjectValid(GetEquippedShield(creature));
        }

        private CNWSItem GetEquippedShieldNative(CNWSCreature creature)
        {
            var leftHandItem = creature.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
            return _item.IsBaseItemType(leftHandItem, _item.ShieldBaseItemTypes)
                ? leftHandItem
                : null;
        }

        private uint GetEquippedShield(uint creature)
        {
            var leftHandItem = GetItemInSlot(InventorySlot.LeftHand, creature);
            return _item.IsBaseItemType(leftHandItem, _item.ShieldBaseItemTypes)
                ? leftHandItem
                : OBJECT_INVALID;
        }

        private int GetShieldDeflectionItemPropertyBonusNative(CNWSItem shield)
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

        private int GetShieldDeflectionItemPropertyBonus(uint shield)
        {
            var bonus = 0;
            for (var ip = GetFirstItemProperty(shield); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(shield))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.ShieldDeflection)
                    bonus += GetItemPropertyCostTableValue(ip);
            }

            return bonus;
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

        private int ApplyPostAccuracyStatusModifiers(uint creature, int accuracy)
        {
            var adjustment = GetStatAdjustment(creature, StatType.AccuracyPercentAdjustment);
            return Math.Max(1, ApplyPercentAdjustment(accuracy, adjustment));
        }

        private int ApplyPostEvasionStatusModifiers(uint creature, int evasion, SkillType incomingSkillType)
        {
            var adjustment = GetStatAdjustment(creature, StatType.EvasionPercentAdjustment);
            if (IsRangedDamageSkill(incomingSkillType))
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
        private int CalculateEffectDefense(uint creature, int defense, CombatDamageType type)
        {
            return defense + GetDefenseAdjustment(creature, type);
        }

        private int ApplyPostDefenseStatusModifiers(uint creature, CombatDamageType type, int defense)
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
        public int GetDefensePercentAdjustment(uint creature, CombatDamageType type)
        {
            return GetStatAdjustment(creature, StatType.DefensePercentAdjustment) + (type switch
            {
                CombatDamageType.Physical => GetStatAdjustment(creature, StatType.PhysicalDefensePercentAdjustment) +
                                             GetStatAdjustment(creature, StatType.PhysicalAndForceDefenseAuraPercentAdjustment) +
                                             GetShieldEquippedPhysicalDefensePercentAdjustment(creature),
                CombatDamageType.Force => GetStatAdjustment(creature, StatType.ForceDefensePercentAdjustment) +
                                          GetStatAdjustment(creature, StatType.PhysicalAndForceDefenseAuraPercentAdjustment),
                _ => 0
            });
        }

        private int GetShieldEquippedPhysicalDefensePercentAdjustment(uint creature)
        {
            return HasShieldEquipped(creature)
                ? GetStatAdjustment(creature, StatType.ShieldEquippedPhysicalDefensePercentAdjustment)
                : 0;
        }

        private int GetDefenseAdjustment(uint creature, CombatDamageType type)
        {
            return GetStatAdjustment(creature, StatType.Defense) + (type switch
            {
                CombatDamageType.Physical => GetStatAdjustment(creature, StatType.PhysicalDefense),
                CombatDamageType.Force => GetStatAdjustment(creature, StatType.ForceDefense),
                _ => 0
            });
        }

        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            var perks = GetPerkStatSources(creature, payloadStat);
            var effects = _statusEffect.GetStatSources(creature, payloadStat);
            var temporary = GetTemporaryStatSources(creature, payloadStat);
            var traits = _mimicry.GetStatSources(creature, payloadStat);
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
            var mimicryTraitAdjustment = _mimicry.GetStatBonus(creature, stat);

            return AggregateStatAdjustment(
                stat,
                AggregateStatAdjustment(stat, statusAdjustment, perkAdjustment),
                mimicryTraitAdjustment);
        }

        public int ApplyOutgoingAbilityHealingAdjustment(uint source, int amount)
        {
            if (amount <= 0 || !GetIsObjectValid(source))
                return amount;

            var statSource = _beastMastery.IsPlayerBeast(source)
                ? GetMaster(source)
                : source;
            var adjustment = GetStatAdjustment(
                statSource,
                StatType.OutgoingAbilityHealingPercentAdjustment);

            return CalculateOutgoingAbilityHealingAmount(amount, adjustment);
        }

        public int CalculateOutgoingAbilityHealingAmount(int amount, int adjustment)
        {
            if (amount <= 0 || adjustment <= 0)
                return amount;

            return amount + (int)Math.Ceiling(amount * (adjustment / 100f));
        }

        /// <param name="isInstantAbilityHeal">True for a heal an ability lands when it resolves. Only these can restore Stamina through the healing-received trigger.</param>
        public int ApplyHealingReceivedAdjustment(uint creature, int amount, bool applyReceivedEffects = true, bool isInstantAbilityHeal = false)
        {
            if (amount <= 0)
                return amount;

            var adjustment = GetStatAdjustment(creature, StatType.HealingReceivedPercentAdjustment);
            var adjustedAmount = Math.Max(1, ApplyPercentAdjustment(amount, adjustment));

            if (applyReceivedEffects)
                ApplyHealingReceivedEffects(creature, adjustedAmount, isInstantAbilityHeal);

            return adjustedAmount;
        }

        public int CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP)
        {
            return currentHP <= 0 ? 0 : Math.Clamp(amount, 0, Math.Max(0, maxHP - currentHP));
        }

        /// <param name="isInstantAbilityHeal">Heals over time, lifesteal, drains and regeneration pass false and never restore Stamina.</param>
        public void ApplyHealingReceivedEffects(uint creature, int amount, bool isInstantAbilityHeal = false)
        {
            // The PvP healing ramp cuts the heal when it lands, so these react to what is left of it.
            amount = PvpCombatRules.ReduceHealing(amount, _pvpCombat.GetHealingReductionPercent(creature));
            if (CalculateEffectiveHealingAmount(amount, ObjectPlugin.GetCurrentHitPoints(creature), GetMaxHitPoints(creature)) <= 0)
                return;
            // The heal these react to is applied right after them.
            using var recorderReaction = _combatRecorder.BeginHealingReaction(creature);
            if (isInstantAbilityHeal)
                ApplyHealingReceivedStaminaRestore(creature);
            ApplyHealingReceivedAttackBoost(creature);
        }

        private void ApplyHealingReceivedStaminaRestore(uint creature)
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

            if (Domain.Foundation.Random.D100(1) > chance)
                return;

            var cooldownSeconds = GetStatAdjustment(creature, StatType.HealingReceivedStaminaRestoreCooldownSeconds);
            if (!TryUseStatTrigger(creature, StatType.HealingReceivedStaminaRestore, cooldownSeconds))
                return;

            using var recorderAttribution = _combatRecorder.BeginStatTriggerAttribution(creature, StatType.HealingReceivedStaminaRestore);
            RestoreStamina(creature, stamina);
        }

        private void ApplyHealingReceivedAttackBoost(uint creature)
        {
            var attackPercent = GetStatAdjustment(creature, StatType.HealingReceivedAttackPercentAdjustment);
            var duration = GetStatAdjustment(creature, StatType.HealingReceivedAttackDurationSeconds);
            if (attackPercent == 0 || duration <= 0)
                return;

            ReplaceTemporaryStatModifier(
                creature,
                StatType.AttackPercentAdjustment,
                attackPercent,
                duration,
                StatType.HealingReceivedAttackPercentAdjustment);
        }

        private AbilityType GetAbilityTypeFromStatValue(int value)
        {
            var abilityValue = value - 1;
            return Enum.IsDefined(typeof(AbilityType), abilityValue)
                ? (AbilityType)abilityValue
                : AbilityType.Invalid;
        }

        private int ApplyFPRestoreAdjustment(uint creature, int amount)
        {
            if (amount <= 0)
                return amount;

            var adjustment = GetStatAdjustment(creature, StatType.FPRestorePercentAdjustment);
            return Math.Max(0, ApplyPercentAdjustment(amount, adjustment));
        }

        private int ApplyPercentAdjustment(int value, int percentAdjustment)
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
        public int GetEvasion(int level, int stat, int bonus) => StatRules.GetEvasion(level, stat, bonus);

        /// <summary>
        /// NPC stat skins carry the complete Evasion budget. Native armor must
        /// not add another budget through inherited equipment, feats or effects.
        /// Stat-based Evasion adjustments remain part of bonus for all creatures.
        /// </summary>
        public int GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget)
        {
            return GetEvasion(level, stat, bonus + (hasNpcStatBudget ? 0 : nativeArmorClass * 5));
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

                    npcStats.Resistances[resistanceType] += _resistance.DecodeItemPropertyCostTableValue(
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

                        npcStats.Resistances[resistanceType] += _resistance.DecodeItemPropertyCostTableValue(
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
        public void RestoreNPCStats(bool outOfCombatRegen)
        {
            RestoreNPCStats(outOfCombatRegen, false);
        }

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        public void RestoreBeastStats()
        {
            RestoreNPCStats(false, true);
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

        private void RestoreNPCStats(bool outOfCombatRegen, bool isBeast)
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
                    !GetIsObjectValid(_enmity.GetHighestEnmityTarget(self)) &&
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

        // The native read of the deflection reactions: they need only the creature's id, so a test can run them.
        protected virtual uint GetNativeCreatureId(CNWSCreature creature) => creature.m_idSelf;

        // The calls that are not injected, one line each, so a test can answer them. The bodies are the original calls. The
        // hot reads among them (the perk and temporary stat reads) add one virtual dispatch and no allocation.

        protected virtual IReadOnlyList<StatAdjustmentSource> GetPerkStatSources(uint creature, StatType payloadStat) =>
            _perk.GetStatSources(creature, payloadStat);

        protected virtual int GetPerkStatBonus(uint creature, StatType stat) => _perk.GetStatBonus(creature, stat);

        protected virtual IReadOnlyList<StatAdjustmentSource> GetTemporaryStatSources(uint creature, StatType payloadStat) =>
            _temporaryStatModifier.GetStatSources(creature, payloadStat);

        protected virtual int GetTemporaryStatAdjustment(uint creature, StatType stat) =>
            _temporaryStatModifier.GetStatAdjustment(creature, stat);

        protected virtual void ReplaceTemporaryStatModifier(uint creature, StatType statType, int amount, float durationSeconds, StatType groupStatType) =>
            _temporaryStatModifier.Replace(creature, statType, amount, durationSeconds, groupStatType);

        protected virtual void ScheduleTemporaryHitPointLimitCheck(uint creature) =>
            TemporaryHitPointEffects.ScheduleLimitCheck(creature);

        protected virtual bool IsDroid(uint creature) => _droid.Value.IsDroid(creature);

        protected virtual SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => _skill.GetSkillTypeByBaseItem(baseItem);

        protected virtual SkillType GetWeaponSkill(uint creature, uint weapon) => _skill.GetWeaponSkill(creature, weapon);

        protected virtual SkillAttribute GetSkillDetails(SkillType skillType) => _skill.GetSkillDetails(skillType);

        protected virtual IReadOnlyList<CombatDamageType> GetDefenseDamageTypes() => _damageCatalog.GetDefenseDamageTypes();

        protected virtual void ApplyFPRestoredEffects(uint creature) => _resources.Value.ApplyFPRestoredEffects(creature);

        protected virtual void ApplyStaminaRestoredEffects(uint creature) => _resources.Value.ApplyStaminaRestoredEffects(creature);

        protected virtual AbilityType GetWeaponDamageAbilityType(uint creature, BaseItem weaponType) =>
            _queries.Value.GetWeaponDamageAbilityType(creature, weaponType);

        protected virtual AbilityType GetWeaponAccuracyAbilityType(uint creature, BaseItem weaponType) =>
            _queries.Value.GetWeaponAccuracyAbilityType(creature, weaponType);

        protected virtual int GetNearbyStatusTargetAttackAdjustment(uint creature) =>
            _damageModifiers.Value.GetNearbyStatusTargetAttackAdjustment(creature);

        protected virtual int GetLowHPAttackAdjustment(uint creature) => _damageModifiers.Value.GetLowHPAttackAdjustment(creature);

        protected virtual int GetLowFPAttackAdjustment(uint creature) => _damageModifiers.Value.GetLowFPAttackAdjustment(creature);

        protected virtual void TrackDeflection(uint creature, DeflectionSource source) => _damageTaken.Value.TrackDeflection(creature, source);

        protected virtual bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds) =>
            _resources.Value.TryUseStatTrigger(creature, statType, cooldownSeconds);

        protected virtual void GrantNextSkillAbilityBonuses(uint creature, SkillType skillType, int damageBonus, int criticalRate, int durationSeconds) =>
            _attackTiming.Value.GrantNextSkillAbilityBonuses(creature, skillType, damageBonus, criticalRate, durationSeconds);

        protected virtual void GrantNextAutoAttackCriticalRateBonus(uint creature, SkillType skillType, int criticalRate, int durationSeconds) =>
            _attackTiming.Value.GrantNextAutoAttackCriticalRateBonus(creature, skillType, criticalRate, durationSeconds);

        protected virtual void GrantNextAbilityNoDelay(uint creature, SkillType skillType, int durationSeconds) =>
            _attackTiming.Value.GrantNextAbilityNoDelay(creature, skillType, durationSeconds);

        protected virtual bool IsRangedDamageSkill(SkillType skillType) => CombatRules.IsRangedDamageSkill(skillType);
    }
}
