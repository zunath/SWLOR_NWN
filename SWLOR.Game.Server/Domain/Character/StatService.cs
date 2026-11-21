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
    /// Stats: the stat adjustment read and its sources, the FP and Stamina pools, the NPC stat skin reads, and the Detection,
    /// Stealth, Lockpicking, Combat Readiness, Control and Craftsmanship reads (part 1 of the stat split). Walk mode, the
    /// movement rate, maximum HP, attributes, NPC loading and regeneration are <c>CreatureStatService</c>'s and the player
    /// records fed by worn gear are <c>StatRecordService</c>'s. The Attack, Accuracy, Defense and Evasion ratings and
    /// the deflection and Guard chances are <c>CombatRatingService</c>'s (Combat); what a deflection sets off is
    /// <c>CombatDeflectionReactionService</c>'s, the healing adjustments are <c>CombatResourcesService</c>'s, and what a
    /// restore grants is the <see cref="IResourceRestoredListener"/>'s (Combat). The logic that used to be static in the
    /// facade <c>Stat</c> is moved here unchanged; the event handlers run on the container's instance, and static callers reach
    /// it through <c>GameServices.Stat</c>. Calls into <c>Skill</c>, <c>Perk</c>, <c>TemporaryStatModifier</c> and the restore
    /// listener sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls. The stat type
    /// metadata and the formulas the offline tools read live in <see cref="StatRules"/>, for code that has no container.
    /// </summary>
    public class StatService : IStatService
    {
        private const int FPPerWillpower = 3;
        private const int StaminaPerTwoMight = 3;
        internal const int MaximumNPCHitPoints = 30000;
        public const float BeastNaturalStaminaRegenDelaySeconds = 6f;
        internal const string BeastNaturalStaminaRegenAvailableAtVariable = "BEAST_STAMINA_REGEN_AVAILABLE_AT";
        public const int MaximumCombatReadinessPercent = 15;
        public const int MaximumNPCDetection = 50;
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
        /// Returns the maximum HP a stat skin grants, or 0 when it grants none.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        public int GetNPCSkinMaxHP(uint skin)
        {
            return Math.Min(GetNPCSkinHPBudget(skin), StatService.MaximumNPCHitPoints);
        }

        /// <summary>
        /// Returns the uncapped NPCHP budget stored on a stat skin, or 0 when it has none.
        /// Gear bonuses are added to and removed from this total, so the cap must not be applied to it.
        /// </summary>
        /// <param name="skin">The stat skin item.</param>
        public int GetNPCSkinHPBudget(uint skin)
        {
            var budget = 0;
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCHP)
                {
                    budget += GetItemPropertyCostTableValue(ip);
                }
            }

            return budget;
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

        protected virtual SkillAttribute GetSkillDetails(SkillType skillType) => _skill.GetSkillDetails(skillType);

        protected virtual void ApplyFPRestoredEffects(uint creature) => _resourceRestored.ApplyFPRestoredEffects(creature, this);

        protected virtual void ApplyStaminaRestoredEffects(uint creature) => _resourceRestored.ApplyStaminaRestoredEffects(creature, this);
    }
}
