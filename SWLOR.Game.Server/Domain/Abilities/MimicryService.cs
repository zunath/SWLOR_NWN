using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Domain.Abilities
{
    /// <summary>
    /// Mimicry lets players with the Combat Analyzer perk witness enemy creatures using their innate
    /// techniques in combat. When the source creature dies, witnesses roll a chance to learn the
    /// technique permanently. Learned techniques can then be equipped (within a slot budget) as
    /// ordinary active ability feats, scaled off the player's stats and the Technique Potency perk.
    /// </summary>
    public class MimicryService : IMimicryService
    {
        // Technique feat -> its cached ability detail. Populated after Ability's cache runs.
        private readonly Dictionary<FeatType, AbilityDetail> _techniques = new();

        // Technique feat -> its feat.2da ICON resref. Resolved once at cache time so the Techniques
        // window never has to read the 2da per-row on open (mirrors how perks cache detail.IconResref).
        private readonly Dictionary<FeatType, string> _techniqueIcons = new();

        // Authored text resolved at boot, before hotbar TLK overrides add resource and recast headers.
        private readonly Dictionary<FeatType, string> _techniqueDescriptions = new();

        // Source NPC feat -> the technique feat it teaches.
        private readonly Dictionary<FeatType, FeatType> _techniqueByNpcFeat = new();

        // Stat -> the trait feats that adjust it, and by how much. Inverted at cache time so the stat
        // pipeline (a hot path) only walks traits that are relevant to the stat being queried.
        private readonly Dictionary<StatType, Dictionary<FeatType, int>> _traitStatsByStat = new();

        // Resistance -> the trait feats that adjust it, and by how much.
        private readonly Dictionary<ResistanceType, Dictionary<FeatType, int>> _traitResistancesByResistance = new();

        // In-memory witness tracker: npc -> playerId -> technique feats witnessed but not yet learned.
        private readonly Dictionary<uint, Dictionary<string, HashSet<FeatType>>> _witnesses = new();

        // Observation is passive: keep its credit out of the combat points used to share active skill XP.
        private readonly Dictionary<uint, HashSet<string>> _analysisObservers = new();

        private bool _witnessSweepScheduled;

        private readonly IDBService _db;
        private readonly IQuickCastService _quickCast;
        private readonly IDMPresenceService _dmPresence;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly IGuiService _gui;
        private readonly Lazy<IAbilityService> _ability;
        private readonly Lazy<IPerkService> _perk;
        private readonly Lazy<IStatService> _stat;
        private readonly ISkillService _skill;
        private readonly Lazy<IStatusEffectService> _statusEffect;
        private readonly Lazy<ICombatPointService> _combatPoint;

        public MimicryService(
            IDBService db,
            IQuickCastService quickCast,
            IDMPresenceService dmPresence,
            IPlayerFeedbackService playerFeedback,
            IGuiService gui,
            Lazy<IAbilityService> ability,
            Lazy<IPerkService> perk,
            Lazy<IStatService> stat,
            ISkillService skill,
            Lazy<IStatusEffectService> statusEffect,
            Lazy<ICombatPointService> combatPoint)
        {
            _db = db;
            _quickCast = quickCast;
            _dmPresence = dmPresence;
            _playerFeedback = playerFeedback;
            _gui = gui;
            _ability = ability;
            _perk = perk;
            _stat = stat;
            _skill = skill;
            _statusEffect = statusEffect;
            _combatPoint = combatPoint;
        }

        private const float WitnessRadius = 15.0f;
        private const float LearnMaxDistance = 40.0f;

        private const int BaseSlotsWithAnalyzer = 2;
        private const int SlotsPerAnalyzerMemoryLevel = 2;
        private const int OverclockedAnalyzerSlotBonus = 2;
        private const int ResonancePotencyPerTechnique = 5;
        private const int ResonancePotencyCap = 20;
        private const int AnalysisXPPercent = 5;
        private const int MaximumAnalysisXPPerCreature = 30;
        private readonly int MaximumMimicryRank = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>().MaxRank;

        private const int BaseLearnChancePercent = 20;
        private const int LearnChancePerRankDelta = 2;
        private const int LearnChancePerPatternRecognitionLevel = 10;
        private const int LearnChancePerPerceptionPoint = 1;
        private const int PerceptionLearnChanceBaseline = 10;
        private const int MaxLearnChancePercent = 75;
        private const int LearnTechniqueXP = 400;
        private const string LearnTechniqueSound = "gui_prompt";

        private const int TotalHotBarSlots = 36;
        // The first hotbar row is F1 through F12, which is slot indices 0 through 11.
        private const int AutoAddHotBarSlots = 12;

        /// <summary>
        /// Caches technique lookups. Runs on OnModuleCacheAfter so Ability's cache (built on
        /// OnModuleCacheBefore) has already populated every AbilityDetail, including the
        /// Mimicry-specific fields set by AbilityBuilder.MimicryTechnique.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CacheData()
        {
            _techniques.Clear();
            _techniqueByNpcFeat.Clear();
            _techniqueIcons.Clear();
            _techniqueDescriptions.Clear();
            _traitStatsByStat.Clear();
            _traitResistancesByResistance.Clear();

            if (!_witnessSweepScheduled)
            {
                ScheduleRepeating(SweepStaleWitnesses, TimeSpan.FromMinutes(5));
                _witnessSweepScheduled = true;
            }

            foreach (var (feat, detail) in GetAllAbilityDetails())
            {
                if (!detail.IsMimicryTechnique)
                    continue;

                _techniques[feat] = detail;

                if (detail.MimicrySourceFeat != FeatType.Invalid)
                    _techniqueByNpcFeat[detail.MimicrySourceFeat] = feat;

                if (!detail.IsMimicryTrait)
                    continue;

                foreach (var (stat, amount) in detail.MimicryTraitStats)
                {
                    if (!_traitStatsByStat.TryGetValue(stat, out var statContributors))
                    {
                        statContributors = new Dictionary<FeatType, int>();
                        _traitStatsByStat[stat] = statContributors;
                    }

                    statContributors[feat] = amount;
                }

                foreach (var (resistance, amount) in detail.MimicryTraitResistances)
                {
                    if (!_traitResistancesByResistance.TryGetValue(resistance, out var resistContributors))
                    {
                        resistContributors = new Dictionary<FeatType, int>();
                        _traitResistancesByResistance[resistance] = resistContributors;
                    }

                    resistContributors[feat] = amount;
                }
            }

            // Resolve icons and authored descriptions during boot, before OnModuleLoad publishes
            // hotbar TLK overrides. Get2DAString requires a live engine,
            // so a unit-test harness (no NWNCore.Init) would otherwise throw and abort the technique
            // caching above. Engine-backed presentation data is unused in that harness.
            try
            {
                foreach (var feat in _techniques.Keys)
                {
                    _techniqueIcons[feat] = Get2DAString("feat", "ICON", (int)feat);
                    _techniqueDescriptions[feat] =
                        int.TryParse(Get2DAString("feat", "DESCRIPTION", (int)feat), out var strRef) && strRef > 0
                            ? GetStringByStrRef(strRef)
                            : string.Empty;
                }
            }
            catch
            {
                _techniqueIcons.Clear();
                _techniqueDescriptions.Clear();
            }
        }

        /// <summary>
        /// Returns each equipped trait's stat payload with the parameters declared by that trait.
        /// </summary>
        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            if (!_traitStatsByStat.ContainsKey(payloadStat) ||
                !GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return Array.Empty<StatAdjustmentSource>();

            var player = _db.Get<Player>(GetObjectUUID(creature));
            if (player == null)
                return Array.Empty<StatAdjustmentSource>();

            var potency = GetStatAdjustment(creature, StatType.MimicryTraitPotencyPercent);
            List<StatAdjustmentSource> sources = null;
            foreach (var feat in player.EquippedTechniques)
            {
                if (_techniques.TryGetValue(feat, out var detail) && detail.IsMimicryTrait &&
                    detail.MimicryTraitStats.TryGetValue(payloadStat, out var value) && value != 0)
                {
                    sources ??= new List<StatAdjustmentSource>();
                    var stats = potency == 0
                        ? detail.MimicryTraitStats
                        : ScaleTraitStats(detail.MimicryTraitStats, potency);
                    sources.Add(new StatAdjustmentSource($"trait:{(int)feat}", stats));
                }
            }
            return (IReadOnlyList<StatAdjustmentSource>)sources ?? Array.Empty<StatAdjustmentSource>();
        }

        /// <summary>
        /// Sums equipped trait adjustments and the elemental-resonance set bonus. These belong
        /// to the loadout rather than status effects that could be cleared on death.
        /// </summary>
        public int GetStatBonus(uint creature, StatType stat)
        {
            var hasStatContributors = _traitStatsByStat.TryGetValue(stat, out var contributors);

            if (!hasStatContributors && stat != StatType.MimicryPotencyPercent)
                return 0;

            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return 0;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(creature));
            if (dbPlayer == null)
                return 0;

            var bonus = stat == StatType.MimicryPotencyPercent
                ? GetSetBonusPotency(dbPlayer)
                : 0;

            if (!hasStatContributors)
                return bonus;

            var potency = stat == StatType.MimicryTraitPotencyPercent
                ? 0
                : GetStatAdjustment(creature, StatType.MimicryTraitPotencyPercent);
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (contributors.TryGetValue(feat, out var amount))
                    bonus += ScaleTraitBonus(amount, potency);
            }

            return bonus;
        }

        /// <summary>
        /// Sums the resistance adjustments contributed by a creature's equipped Mimicry traits.
        /// </summary>
        public int GetResistanceBonus(uint creature, ResistanceType resistance)
        {
            if (!_traitResistancesByResistance.TryGetValue(resistance, out var contributors))
                return 0;

            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return 0;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(creature));
            if (dbPlayer == null)
                return 0;

            var potency = GetStatAdjustment(creature, StatType.MimicryTraitPotencyPercent);
            var bonus = 0;
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (contributors.TryGetValue(feat, out var amount))
                    bonus += ScaleTraitBonus(amount, potency);
            }

            return bonus;
        }

        private int ScaleTraitBonus(int amount, int potency)
        {
            return amount + (int)Math.Ceiling(amount * (potency / 100f));
        }

        private Dictionary<StatType, int> ScaleTraitStats(IReadOnlyDictionary<StatType, int> stats, int potency)
        {
            var scaled = new Dictionary<StatType, int>(stats.Count);
            foreach (var (stat, amount) in stats)
                scaled[stat] = ScaleTraitBonus(amount, potency);
            return scaled;
        }

        /// <summary>
        /// Returns the cached feat.2da ICON resref for a technique, resolved once at cache time.
        /// Empty string if the feat is not a registered technique.
        /// </summary>
        public string GetTechniqueIcon(FeatType feat)
        {
            return _techniqueIcons.TryGetValue(feat, out var icon) ? icon : string.Empty;
        }

        /// <summary>
        /// Returns authored technique text cached at boot, without the hotbar TLK headers.
        /// Empty string if the feat has no cached description.
        /// </summary>
        public string GetTechniqueDescription(FeatType feat)
        {
            return _techniqueDescriptions.TryGetValue(feat, out var description) ? description : string.Empty;
        }

        /// <summary>
        /// Called from the ability-use pipeline whenever a non-player creature uses a registered feat.
        /// If the feat maps to a technique, every nearby player who has the Combat Analyzer perk and
        /// hasn't already learned the technique has the witness recorded (used at the creature's death
        /// to roll for learning). Sends a one-time floating text per (npc, player, technique).
        /// </summary>
        /// <param name="activator">The creature that used the ability.</param>
        /// <param name="npcFeat">The feat that was used.</param>
        public void OnCreatureAbilityUsed(uint activator, FeatType npcFeat)
        {
            if (!GetIsObjectValid(activator) || GetIsPC(activator))
                return;

            if (!_techniqueByNpcFeat.TryGetValue(npcFeat, out var techniqueFeat))
                return;

            if (!_techniques.TryGetValue(techniqueFeat, out var techniqueDetail))
                return;

            foreach (var nearby in GetNearbyPlayers(activator, WitnessRadius))
            {
                if (!_dmPresence.IsControlledByDM(nearby))
                {
                    TryRecordWitness(activator, nearby, techniqueFeat, techniqueDetail);
                    TryRecordAnalysis(activator, nearby);
                }
            }
        }

        private List<uint> GetNearbyPlayers(uint npc, float radius)
        {
            var players = new List<uint>();
            var area = GetArea(npc);
            // Snapshot before callbacks, which may enumerate players for feedback.
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) == area && GetDistanceBetween(npc, player) <= radius)
                    players.Add(player);
            }
            return players;
        }

        /// <summary>
        /// Records one analysis credit per player and creature, including already learned techniques.
        /// The observer must already be participating in combat. Observation never adds combat points.
        /// </summary>
        private void TryRecordAnalysis(uint npc, uint player)
        {
            if (GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            if (!HasCombatPoints(player, npc))
                return;

            if (!_analysisObservers.TryGetValue(npc, out var observers))
            {
                observers = new HashSet<string>();
                _analysisObservers[npc] = observers;
            }

            observers.Add(GetObjectUUID(player));
        }

        /// <summary>
        /// Pays observation XP only after the normal kill payout has validated participation,
        /// survival, area, and distance. Consuming the credit prevents repeat event payouts.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCombatPointXPDistribute)]
        public void CombatPointXPDistributed()
        {
            var player = OBJECT_SELF;
            var npc = StringToObject(EventsPlugin.GetEventData("NPC"));
            if (!_analysisObservers.TryGetValue(npc, out var observers) ||
                !observers.Remove(GetObjectUUID(player)))
                return;

            if (observers.Count == 0)
                _analysisObservers.Remove(npc);

            if (GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            var xp = CalculateAnalysisXP(GetNpcLevel(npc), dbPlayer.Skills[SkillType.Mimicry].Rank);
            if (xp > 0)
            {
                // No area or character XP bonuses: this passive reward must stay within its cap.
                GiveSkillXP(player, SkillType.Mimicry, xp, ignoreBonuses: true, xpMessageSource: npc);
            }
        }

        /// <summary>
        /// Observation grants 5% of rank-relative kill XP, capped at 30 before normal penalties.
        /// Trivial enemies and mastered Mimicry grant no observation XP.
        /// </summary>
        public int CalculateAnalysisXP(int npcLevel, int mimicryRank)
        {
            if (mimicryRank >= MaximumMimicryRank)
                return 0;

            return Math.Min(MaximumAnalysisXPPerCreature,
                GetDeltaXP(npcLevel - mimicryRank) * AnalysisXPPercent / 100);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public void ClearAnalysisOnPlayerExit()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            foreach (var (npc, observers) in _analysisObservers.ToArray())
            {
                observers.Remove(playerId);
                if (observers.Count == 0)
                    _analysisObservers.Remove(npc);
            }
        }

        /// <summary>
        /// Records a witness entry for a player/technique pair, if they qualify and haven't already
        /// been recorded. Sends the "recording" floating text exactly once per (npc, player, technique).
        /// </summary>
        private void TryRecordWitness(uint npc, uint player, FeatType techniqueFeat, AbilityDetail techniqueDetail)
        {
            if (GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer.LearnedTechniques.ContainsKey(techniqueFeat))
                return;

            if (!_witnesses.TryGetValue(npc, out var byPlayer))
            {
                byPlayer = new Dictionary<string, HashSet<FeatType>>();
                _witnesses[npc] = byPlayer;
            }

            if (!byPlayer.TryGetValue(playerId, out var witnessedTechniques))
            {
                witnessedTechniques = new HashSet<FeatType>();
                byPlayer[playerId] = witnessedTechniques;
            }

            // Add returns false if this (npc, player, technique) combination was already recorded.
            if (!witnessedTechniques.Add(techniqueFeat))
                return;

            // Witnessing a technique above the player's current skill is still recorded (the learn roll re-checks the
            // gate at the creature's death, in case the player's rank crosses the floor first),
            // Explain the skill gate, but limit repeated warnings about the same technique across NPCs.
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var requiredSkillRank = techniqueDetail.MimicrySkillRequirement;

            if (skillRank < requiredSkillRank)
            {
                _playerFeedback.SendWarningToPlayer(player, "MIMICRY_RANK_" + (int)techniqueFeat, ColorToken.Gray(
                    $"Your combat analyzer detects {techniqueDetail.Name}, but the pattern is beyond your current analysis level. (Requires Mimicry {requiredSkillRank})"), intervalSeconds: 60);
                return;
            }

            SendMessageToPC(player, ColorToken.Cyan($"Your combat analyzer records {techniqueDetail.Name}..."));
        }

        /// <summary>
        /// When a creature dies, every player who witnessed one of its techniques gets a chance to
        /// learn it permanently. The witness cache entry for this creature is always cleared afterward.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public void OnCreatureDeath()
        {
            var npc = OBJECT_SELF;

            if (_witnesses.TryGetValue(npc, out var byPlayer) && byPlayer.Count > 0)
            {
                foreach (var nearby in GetNearbyPlayers(npc, LearnMaxDistance))
                    TryLearnTechniques(npc, nearby, byPlayer);
            }

            _witnesses.Remove(npc);
        }

        /// <summary>
        /// Periodically removes witness entries for creatures that no longer exist. Creatures
        /// normally clear their entry on death, but despawns, DestroyObject calls, and area
        /// unloads never fire OnCreatureDeathAfter, so their entries would otherwise persist
        /// for the life of the server.
        /// </summary>
        private void SweepStaleWitnesses()
        {
            var staleCreatures = new List<uint>();
            foreach (var npc in _witnesses.Keys)
            {
                if (!GetIsObjectValid(npc))
                    staleCreatures.Add(npc);
            }

            foreach (var npc in staleCreatures)
            {
                _witnesses.Remove(npc);
            }

            // Ineligible observers receive no combat payout event. Reclaim their credit after death
            // as well as despawn, without depending on creature-death handler ordering.
            foreach (var npc in _analysisObservers.Keys.ToArray())
            {
                if (!GetIsObjectValid(npc) || GetIsDead(npc))
                    _analysisObservers.Remove(npc);
            }
        }

        /// <summary>
        /// Rolls a learn chance for every technique a specific player witnessed on this creature.
        /// </summary>
        private void TryLearnTechniques(uint npc, uint player, Dictionary<string, HashSet<FeatType>> byPlayer)
        {
            if (!GetIsPC(player) ||
                GetIsDM(player) ||
                GetIsDead(player) ||
                GetCurrentHitPoints(player) <= 0 ||
                GetArea(player) != GetArea(npc))
                return;

            var playerId = GetObjectUUID(player);
            if (!byPlayer.TryGetValue(playerId, out var witnessedTechniques) || witnessedTechniques.Count == 0)
                return;

            var dbPlayer = _db.Get<Player>(playerId);
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var patternRecognitionLevel = GetPerkLevel(player, PerkType.PatternRecognition);
            var perception = GetAbilityScore(player, AbilityType.Perception);
            var learnedDetails = new List<AbilityDetail>();

            foreach (var feat in witnessedTechniques)
            {
                if (dbPlayer.LearnedTechniques.ContainsKey(feat))
                    continue;

                if (!_techniques.TryGetValue(feat, out var detail))
                    continue;

                var requiredSkillRank = detail.MimicrySkillRequirement;
                if (skillRank < requiredSkillRank)
                    continue;

                var chance = CalculateLearnChance(skillRank, requiredSkillRank, patternRecognitionLevel, perception);

                if (Random.D100(1) > chance)
                {
                    // Explain the retry, limiting repeat failures for the same technique across NPCs.
                    // The witness entry is cleared on death, so another creature must be analyzed.
                    _playerFeedback.SendWarningToPlayer(player, "MIMICRY_DECODE_" + (int)feat, ColorToken.Orange(
                        $"Your combat analyzer failed to decode {detail.Name}. Analyze it again to retry."), intervalSeconds: 60);
                    continue;
                }

                dbPlayer.LearnedTechniques[feat] = DateTime.UtcNow;
                learnedDetails.Add(detail);
            }

            if (learnedDetails.Count <= 0)
                return;

            // Persist the learned techniques before GiveSkillXP runs - it performs its own
            // _db.Get/_db.Set of this player, so saving our stale copy afterward would clobber the XP.
            _db.Set(dbPlayer);

            foreach (var detail in learnedDetails)
            {
                SendMessageToPC(player, ColorToken.Green($"You learned the technique: {detail.Name}!"));
                GiveSkillXP(player, SkillType.Mimicry, LearnTechniqueXP, xpMessageSource: npc);

                Log.WriteStructured(
                    LogGroup.Mimicry,
                    "Technique learned: PlayerId={PlayerId} Technique={Technique} SkillRank={SkillRank}",
                    playerId, detail.Name, skillRank);
            }

            PlayerPlugin.PlaySound(player, LearnTechniqueSound, OBJECT_INVALID);
        }

        /// <summary>
        /// Computes the percent chance to learn a witnessed technique when the source creature dies.
        /// Scales off Mimicry skill rank above the technique's individual requirement, the Pattern Recognition
        /// perk, and the player's Perception attribute (each point above <see cref="PerceptionLearnChanceBaseline"/>
        /// adds <see cref="LearnChancePerPerceptionPoint"/> percent, rewarding perceptive characters).
        /// The result is clamped to <see cref="MaxLearnChancePercent"/>.
        /// </summary>
        public int CalculateLearnChance(int skillRank, int requiredSkillRank, int patternRecognitionLevel, int perception)
        {
            var chance = BaseLearnChancePercent +
                         LearnChancePerRankDelta * (skillRank - requiredSkillRank) +
                         LearnChancePerPatternRecognitionLevel * patternRecognitionLevel +
                         LearnChancePerPerceptionPoint * Math.Max(0, perception - PerceptionLearnChanceBaseline);

            if (chance > MaxLearnChancePercent)
                chance = MaxLearnChancePercent;

            return chance;
        }

        /// <summary>
        /// Grants every registered technique to a player as learned, bypassing the witness/roll flow.
        /// Intended for tester and DM tooling (e.g. the learn-all chat command). Does not equip the
        /// techniques or grant the Combat Analyzer perk; the player still equips them through the
        /// Techniques window. Returns the number of techniques newly added to the player's learned set.
        /// </summary>
        public int GrantAllTechniques(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var learnedCount = 0;
            foreach (var feat in _techniques.Keys)
            {
                if (dbPlayer.LearnedTechniques.ContainsKey(feat))
                    continue;

                dbPlayer.LearnedTechniques[feat] = DateTime.UtcNow;
                learnedCount++;
            }

            if (learnedCount > 0)
                _db.Set(dbPlayer);

            return learnedCount;
        }

        /// <summary>
        /// Returns the maximum number of technique slots available to a player.
        /// Zero if the player does not have the Combat Analyzer perk.
        /// </summary>
        public int GetMaxSlots(uint player)
        {
            if (!GetIsPC(player))
                return 0;

            var playerId = GetObjectUUID(player);
            return GetMaxSlots(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the maximum number of technique slots available to a player's database record.
        /// </summary>
        public int GetMaxSlots(Player dbPlayer)
        {
            if (dbPlayer == null)
                return 0;

            var analyzerLevel = dbPlayer.Perks.TryGetValue(PerkType.CombatAnalyzer, out var level) ? level : 0;
            if (analyzerLevel < 1)
                return 0;

            var memoryLevel = dbPlayer.Perks.TryGetValue(PerkType.AnalyzerMemory, out var memoryPerkLevel) ? memoryPerkLevel : 0;
            var capstoneBonus = dbPlayer.Perks.TryGetValue(PerkType.OverclockedAnalyzer, out var capstoneLevel) && capstoneLevel >= 1
                ? OverclockedAnalyzerSlotBonus
                : 0;

            return BaseSlotsWithAnalyzer + memoryLevel * SlotsPerAnalyzerMemoryLevel + capstoneBonus;
        }

        /// <summary>
        /// Damage-type loadout set bonus ("elemental resonance"): equipping multiple active
        /// techniques sharing a damage type grants scaling technique potency. Each damage type with
        /// at least two equipped techniques contributes (count - 1) * <see cref="ResonancePotencyPerTechnique"/>
        /// percent, summed across types and capped at <see cref="ResonancePotencyCap"/>. Passive
        /// traits have no damage element and do not contribute.
        /// </summary>
        public int GetSetBonusPotency(Player dbPlayer)
        {
            if (dbPlayer == null)
                return 0;

            var countsByElement = new Dictionary<CombatDamageType, int>();
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (!_techniques.TryGetValue(feat, out var detail) || detail.MimicryElement == CombatDamageType.Invalid)
                    continue;

                countsByElement.TryGetValue(detail.MimicryElement, out var count);
                countsByElement[detail.MimicryElement] = count + 1;
            }

            var potency = countsByElement.Values
                .Where(count => count >= 2)
                .Sum(count => (count - 1) * ResonancePotencyPerTechnique);

            return potency > ResonancePotencyCap ? ResonancePotencyCap : potency;
        }

        /// <summary>
        /// Returns the number of technique slots currently used by a player's equipped techniques.
        /// </summary>
        public int GetUsedSlots(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetUsedSlots(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the number of technique slots currently used by a player database record's equipped techniques.
        /// </summary>
        public int GetUsedSlots(Player dbPlayer)
        {
            if (dbPlayer == null)
                return 0;

            var used = 0;
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (_techniques.TryGetValue(feat, out var detail))
                    used += detail.MimicrySlotCost;
            }

            return used;
        }

        /// <summary>
        /// Determines whether a player can equip a given technique right now.
        /// </summary>
        public bool CanEquip(uint player, FeatType feat, out string error)
        {
            var playerId = GetObjectUUID(player);
            return CanEquip(player, _db.Get<Player>(playerId), feat, out error);
        }

        /// <summary>
        /// Determines whether a player can equip a given technique right now, using an
        /// already-fetched player database record to avoid duplicate round trips.
        /// </summary>
        public bool CanEquip(uint player, Player dbPlayer, FeatType feat, out string error)
        {
            error = string.Empty;

            if (!GetIsPC(player) || GetIsDM(player) || dbPlayer == null)
            {
                error = "Only players may equip techniques.";
                return false;
            }

            if (!_techniques.TryGetValue(feat, out var detail))
            {
                error = "That is not a valid technique.";
                return false;
            }

            if (!dbPlayer.LearnedTechniques.ContainsKey(feat))
            {
                error = "You have not learned that technique.";
                return false;
            }

            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var requiredSkillRank = detail.MimicrySkillRequirement;
            if (skillRank < requiredSkillRank)
            {
                error = $"You need Mimicry skill rank {requiredSkillRank} to equip that technique.";
                return false;
            }

            if (dbPlayer.EquippedTechniques.Contains(feat))
            {
                error = "That technique is already equipped.";
                return false;
            }

            if (GetIsInCombat(player))
            {
                error = "You cannot manage techniques while in combat.";
                return false;
            }

            if (GetUsedSlots(dbPlayer) + detail.MimicrySlotCost > GetMaxSlots(dbPlayer))
            {
                error = "You do not have enough technique slots available.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Equips a learned technique, granting the underlying feat and adding it to the player's hotbar.
        /// </summary>
        public bool EquipTechnique(uint player, FeatType feat)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!CanEquip(player, dbPlayer, feat, out var error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return false;
            }

            dbPlayer.EquippedTechniques.Add(feat);
            _db.Set(dbPlayer);

            GrantTechniqueFeat(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips a technique, removing the underlying feat and any hotbar slot referencing it.
        /// </summary>
        public bool UnequipTechnique(uint player, FeatType feat)
        {
            if (!GetIsPC(player))
                return false;

            if (GetIsInCombat(player))
            {
                SendMessageToPC(player, ColorToken.Red("You cannot manage techniques while in combat."));
                return false;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (!dbPlayer.EquippedTechniques.Remove(feat))
                return false;

            _db.Set(dbPlayer);
            RevokeTechniqueFeat(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips techniques that exceed the player's current Mimicry rank, then removes the
        /// newest-equipped techniques until the remaining loadout fits the current slot budget.
        /// Used after progression changes and on login to keep persisted loadouts valid.
        /// </summary>
        public void EnforceSlotBudget(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            EnforceSlotBudget(player, _db.Get<Player>(playerId));
        }

        /// <summary>
        /// Enforces Mimicry rank requirements and the slot budget against an already-fetched player
        /// database record, avoiding a duplicate round trip when the caller has one in hand.
        /// </summary>
        public void EnforceSlotBudget(uint player, Player dbPlayer)
        {
            if (!GetIsPC(player) || dbPlayer == null)
                return;

            var playerId = GetObjectUUID(player);
            var maxSlots = GetMaxSlots(dbPlayer);
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill)
                ? mimicrySkill.Rank
                : 0;
            var changed = false;

            for (var i = dbPlayer.EquippedTechniques.Count - 1; i >= 0; i--)
            {
                var feat = dbPlayer.EquippedTechniques[i];
                if (!_techniques.TryGetValue(feat, out var detail) ||
                    skillRank >= detail.MimicrySkillRequirement)
                    continue;

                dbPlayer.EquippedTechniques.RemoveAt(i);
                changed = true;

                RevokeTechniqueFeat(player, feat);

                Log.WriteStructured(
                    LogGroup.Mimicry,
                    "Technique unequipped by skill requirement enforcement: PlayerId={PlayerId} Technique={Technique} SkillRank={SkillRank} RequiredRank={RequiredRank}",
                    playerId, feat, skillRank, detail.MimicrySkillRequirement);
            }

            for (var i = dbPlayer.EquippedTechniques.Count - 1; i >= 0; i--)
            {
                if (GetUsedSlots(dbPlayer) <= maxSlots)
                    break;

                var feat = dbPlayer.EquippedTechniques[i];
                dbPlayer.EquippedTechniques.RemoveAt(i);
                changed = true;

                RevokeTechniqueFeat(player, feat);

                Log.WriteStructured(
                    LogGroup.Mimicry,
                    "Technique unequipped by slot budget enforcement: PlayerId={PlayerId} Technique={Technique}",
                    playerId, feat);
            }

            if (changed)
            {
                _db.Set(dbPlayer);
                _quickCast.ApplyPreferences(player);
                _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());
            }
        }

        /// <summary>
        /// Immediately removes equipped techniques whose rank requirement is no longer met when
        /// Mimicry loses a rank. This must run for every Mimicry decay, including rank losses that
        /// do not cross a perk requirement and therefore do not trigger a perk refund callback.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
        public void OnMimicrySkillDecay()
        {
            var skillType = (SkillType)Convert.ToInt32(EventsPlugin.GetEventData("SKILL_TYPE_ID"));
            if (skillType != SkillType.Mimicry)
                return;

            EnforceSlotBudget(OBJECT_SELF);
        }

        /// <summary>
        /// Unequips every equipped technique. Used when the Combat Analyzer perk is refunded.
        /// </summary>
        public void UnequipAllTechniques(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            if (dbPlayer.EquippedTechniques.Count <= 0)
                return;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                RevokeTechniqueFeat(player, feat);
            }

            var unequippedCount = dbPlayer.EquippedTechniques.Count;
            dbPlayer.EquippedTechniques.Clear();
            _db.Set(dbPlayer);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            Log.WriteStructured(
                LogGroup.Mimicry,
                "All techniques unequipped by perk refund: PlayerId={PlayerId} Count={Count}",
                playerId, unequippedCount);
        }

        /// <summary>
        /// On login, re-grants every equipped technique's feat (in case it was lost, e.g. a fresh
        /// character load) and enforces rank and slot limits in case progression changed since logout.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void OnPlayerLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            // Character initialization can run migrations that clear native feats later in
            // this same module-enter event. Restore techniques after those handlers finish.
            DelayCommand(0f, () => RestorePlayerTechniques(player));
        }

        private void RestorePlayerTechniques(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            if (GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            EnforceSlotBudget(player, dbPlayer);

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                GrantTechniqueFeat(player, feat);
            }
            _quickCast.ApplyPreferences(player);
        }

        /// <summary>
        /// Applies an equipped technique to the player. Active techniques grant the underlying feat
        /// (if missing) and add it to the hotbar, mirroring the grant + hotbar logic used by the perk
        /// system's active ability feats. Trait techniques are passive and are never granted as a
        /// usable feat: their stats and resistances are derived straight from the equipped loadout, so
        /// there is no grant or revoke step for them here, and any stale feat/hotbar entry is stripped
        /// so a trait can never end up castable or on the quickbar.
        /// </summary>
        private void GrantTechniqueFeat(uint player, FeatType feat, bool addToHotbar = true)
        {
            if (!GetIsObjectValid(player))
                return;

            feat = _quickCast.GetOriginalFeat(feat);
            var detail = GetTechniqueDetail(feat);
            if (detail != null && detail.IsMimicryTrait)
            {
                // Passive traits are never usable: they must not be granted as a castable feat nor
                // placed on the hotbar. Strip any feat/hotbar entry an earlier grant (or a prior
                // version that granted the feat unconditionally) left behind. The trait's stats need
                // no grant step - the stat pipeline reads them from the equipped list directly.
                if (_quickCast.KnowsFeat(player, feat))
                    _quickCast.RemoveFeat(player, feat);

                RemoveFeatFromHotBar(player, feat);
                return;
            }

            // Active technique: grant the feat and add it to the hotbar, using the same rules as a
            // perk-purchased active ability (register + ImpactAction gate, first empty auto-add slot).
            if (!_quickCast.KnowsFeat(player, feat))
                CreaturePlugin.AddFeat(player, _quickCast.GetDisplayedFeat(player, feat));

            if (addToHotbar)
                AddFeatToHotBar(player, feat);
        }

        /// <summary>Validates the entire draft before changing any equipped state.</summary>
        public bool ValidateLoadout(Player dbPlayer, IReadOnlyCollection<FeatType> techniques, out string error)
        {
            error = string.Empty;
            if (techniques.Count != techniques.Distinct().Count())
                error = "A build cannot contain duplicate techniques.";
            else
            {
                var rank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var skill) ? skill.Rank : 0;
                var used = 0;
                foreach (var feat in techniques)
                {
                    if (!_techniques.TryGetValue(feat, out var detail))
                    {
                        error = "This build contains an unavailable technique. Remove it before applying.";
                        break;
                    }
                    if (!dbPlayer.LearnedTechniques.ContainsKey(feat))
                    {
                        error = $"You have not learned {detail.Name}.";
                        break;
                    }
                    if (rank < detail.MimicrySkillRequirement)
                    {
                        error = $"{detail.Name} requires Mimicry rank {detail.MimicrySkillRequirement}.";
                        break;
                    }
                    used += detail.MimicrySlotCost;
                }
                if (error.Length == 0 && used > GetMaxSlots(dbPlayer))
                    error = $"This build needs {used} technique slots; you have {GetMaxSlots(dbPlayer)}.";
            }
            return error.Length == 0;
        }

        public void CaptureLoadoutHotbar(uint player, MimicryLoadout loadout)
        {
            loadout.HotbarSlots.Clear();
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var entry = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (entry.ObjectType != QuickBarSlotType.Feat)
                    continue;

                var feat = _quickCast.GetOriginalFeat((FeatType)entry.INTParam1);
                if (loadout.Techniques.Contains(feat))
                    loadout.HotbarSlots[slot] = feat;
            }
        }

        public bool ApplyLoadout(uint player, string loadoutId, IReadOnlyCollection<FeatType> techniques, out string error)
        {
            error = string.Empty;
            if (!GetIsPC(player) || GetIsDM(player))
                error = "Only players may apply technique builds.";
            else if (GetIsInCombat(player))
                error = "You cannot apply a technique build while in combat.";
            if (error.Length > 0)
                return false;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            var incoming = dbPlayer.MimicryLoadouts.FirstOrDefault(x => x.Id == loadoutId);
            if (incoming == null)
            {
                error = "That loadout no longer exists.";
                return false;
            }
            if (!ValidateLoadout(dbPlayer, techniques, out error))
                return false;

            var previous = dbPlayer.EquippedTechniques.ToList();
            var outgoing = dbPlayer.MimicryLoadouts.FirstOrDefault(x => x.Id == dbPlayer.ActiveMimicryLoadoutId);
            // A modified equipped set must not overwrite the saved preset's hotbar layout.
            if (outgoing != null && MimicryLoadouts.Matches(outgoing.Techniques, previous))
                CaptureLoadoutHotbar(player, outgoing);

            // Commit the whole set once. Revoke only outgoing feats so shared stances survive.
            // RecastTimes is intentionally untouched when builds change.
            dbPlayer.EquippedTechniques = techniques.ToList();
            dbPlayer.ActiveMimicryLoadoutId = loadoutId;
            _db.Set(dbPlayer);
            foreach (var feat in previous.Except(techniques))
                RevokeTechniqueFeat(player, feat);
            foreach (var feat in techniques)
                GrantTechniqueFeat(player, feat, false);

            // Restore only empty or Mimicry-owned cells. Other shortcuts always take precedence.
            var owned = previous.Concat(techniques).ToHashSet();
            if (incoming.HotbarSlots.Count > 0)
            {
                foreach (var feat in techniques)
                    RemoveFeatFromHotBar(player, feat);
                foreach (var (slot, savedFeat) in incoming.HotbarSlots)
                {
                    var feat = _quickCast.GetOriginalFeat(savedFeat);
                    if (slot < 0 || slot >= TotalHotBarSlots || !techniques.Contains(feat) ||
                        !_techniques.TryGetValue(feat, out var detail) || detail.IsMimicryTrait)
                        continue;
                    var cell = PlayerPlugin.GetQuickBarSlot(player, slot);
                    if (cell.ObjectType == QuickBarSlotType.Empty ||
                        cell.ObjectType == QuickBarSlotType.Feat && owned.Contains(_quickCast.GetOriginalFeat((FeatType)cell.INTParam1)))
                        PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.UseFeat(_quickCast.GetDisplayedFeat(player, feat)));
                }
            }
            foreach (var feat in techniques.Where(feat => !_techniques[feat].IsMimicryTrait))
                AddFeatToHotBar(player, feat);
            _quickCast.ApplyPreferences(player);
            _gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());
            return true;
        }

        /// <summary>
        /// Removes the technique's feat, any hotbar slot referencing it, and persistent effects
        /// declared by the ability. Trait stats need no explicit revoke step; they stop applying as
        /// soon as the feat leaves the equipped list.
        /// </summary>
        private void RevokeTechniqueFeat(uint player, FeatType feat)
        {
            if (!GetIsObjectValid(player))
                return;

            feat = _quickCast.GetOriginalFeat(feat);
            var detail = GetTechniqueDetail(feat);
            if (detail != null)
            {
                foreach (var statusEffectType in detail.StatusEffectTypesRemovedOnPerkRefund)
                {
                    RemoveStatusEffect(player, statusEffectType, false);
                }

                foreach (var statusEffectType in detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund)
                {
                    RemoveStatusEffectsFromAllTargetsBySource(
                        player,
                        statusEffectType,
                        false,
                        detail);
                }
            }

            _quickCast.RemoveFeat(player, feat);
            RemoveFeatFromHotBar(player, feat);
        }

        private bool IsFeatOnHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    _quickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1) == feat)
                    return true;
            }

            return false;
        }

        private void AddFeatToHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            if (!IsFeatRegistered(feat) || GetAbilityDetail(feat).ImpactAction == null)
                return;

            if (IsFeatOnHotBar(player, feat))
                return;

            var quickBarSlot = PlayerQuickBarSlot.UseFeat(_quickCast.GetDisplayedFeat(player, feat));

            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(player, slot, quickBarSlot);
                return;
            }
        }

        private void RemoveFeatFromHotBar(uint player, FeatType feat)
        {
            feat = _quickCast.GetOriginalFeat(feat);
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    _quickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1) == feat)
                {
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        /// <summary>
        /// Returns every technique a player has learned, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetLearnedTechniques(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has learned, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(Player dbPlayer)
        {
            var result = new List<(FeatType, AbilityDetail)>();
            if (dbPlayer == null)
                return result;

            foreach (var feat in dbPlayer.LearnedTechniques.Keys)
            {
                if (_techniques.TryGetValue(feat, out var detail))
                    result.Add((feat, detail));
            }

            return result;
        }

        /// <summary>
        /// Returns every technique a player currently has equipped, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetEquippedTechniques(_db.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has equipped, alongside its cached ability detail.
        /// </summary>
        public List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(Player dbPlayer)
        {
            var result = new List<(FeatType, AbilityDetail)>();
            if (dbPlayer == null)
                return result;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (_techniques.TryGetValue(feat, out var detail))
                    result.Add((feat, detail));
            }

            return result;
        }

        /// <summary>
        /// Returns true when a technique feat is present in a player's equipped loadout.
        /// </summary>
        public bool IsTechniqueEquipped(uint player, FeatType feat)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            return IsTechniqueEquipped(_db.Get<Player>(playerId), feat);
        }

        /// <summary>
        /// Returns true when a technique feat is present in an already-fetched player record's
        /// equipped loadout.
        /// </summary>
        public bool IsTechniqueEquipped(Player dbPlayer, FeatType feat)
        {
            return dbPlayer?.EquippedTechniques.Contains(_quickCast.GetOriginalFeat(feat)) == true;
        }

        /// <summary>
        /// Retrieves the cached ability detail for a technique feat, or null if the feat isn't a technique.
        /// </summary>
        public AbilityDetail GetTechniqueDetail(FeatType feat)
        {
            return _techniques.TryGetValue(_quickCast.GetOriginalFeat(feat), out var detail) ? detail : null;
        }

        /// <summary>
        /// Returns true if the given feat is a registered Mimicry technique.
        /// </summary>
        public bool IsTechnique(FeatType feat)
        {
            return _techniques.ContainsKey(_quickCast.GetOriginalFeat(feat));
        }

        /// <summary>
        /// Returns the total number of registered techniques.
        /// </summary>
        public int TechniqueCount => _techniques.Count;

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _ability.Value.GetAllAbilityDetails();

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual bool IsFeatRegistered(FeatType feat) => _ability.Value.IsFeatRegistered(feat);

        /// <summary>The unconverted <c>Ability</c> registry.</summary>
        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _ability.Value.GetAbilityDetail(feat);

        /// <summary>The unconverted <c>Perk</c> service.</summary>
        protected virtual int GetPerkLevel(uint creature, PerkType perkType) => _perk.Value.GetPerkLevel(creature, perkType);

        /// <summary>The unconverted <c>Stat</c> service.</summary>
        protected virtual int GetStatAdjustment(uint creature, StatType stat) => _stat.Value.GetStatAdjustment(creature, stat);

        /// <summary>The level of an NPC from the unconverted <c>Stat</c> service.</summary>
        protected virtual int GetNpcLevel(uint npc) => _stat.Value.GetNPCStats(npc).Level;

        /// <summary>The unconverted <c>Skill</c> service.</summary>
        protected virtual int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true, uint xpMessageSource = OBJECT_INVALID) =>
            _skill.GiveSkillXP(player, skill, xp, ignoreBonuses, applyHenchmanPenalty, xpMessageSource);

        /// <summary>The unconverted <c>Skill</c> XP chart.</summary>
        protected virtual int GetDeltaXP(int delta) => _skill.GetDeltaXP(delta);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RemoveStatusEffect(uint creature, Type statusEffectType, bool sendsWornOffMessage) =>
            _statusEffect.Value.RemoveStatusEffect(creature, statusEffectType, sendsWornOffMessage);

        /// <summary>The unconverted <c>StatusEffect</c> service.</summary>
        protected virtual void RemoveStatusEffectsFromAllTargetsBySource(uint source, Type statusEffectType,
            bool sendsWornOffMessage, AbilityDetail originatingAbility) =>
            _statusEffect.Value.RemoveStatusEffectsFromAllTargetsBySource(source, statusEffectType, sendsWornOffMessage,
                originatingAbility);

        /// <summary>The unconverted <c>CombatPoint</c> service.</summary>
        protected virtual bool HasCombatPoints(uint player, uint creature) => _combatPoint.Value.HasCombatPoints(player, creature);

        /// <summary>The unconverted <c>Scheduler</c>.</summary>
        protected virtual void ScheduleRepeating(Action task, TimeSpan interval) => Scheduler.ScheduleRepeating(task, interval);
    }
}
