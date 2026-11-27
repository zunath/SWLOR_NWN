using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

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

        // Authored text resolved at boot, before hotbar TLK overrides add resource and recast headers. A hot reload reads the
        // overridden text, so it keeps the old build's (CarryTechniqueDescriptions, AdoptTechniqueDescriptions).
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<FeatType, string> _techniqueDescriptions = new();

        /// <summary>The key the technique descriptions are carried under across a hot reload (<see cref="ReloadCarry"/>).</summary>
        private const string DescriptionCarryKey = "mimicryDescriptions";

        // Source NPC feat -> the technique feat it teaches.
        private readonly Dictionary<FeatType, FeatType> _techniqueByNpcFeat = new();

        // Resistance -> the trait feats that adjust it, and by how much.
        private readonly Dictionary<ResistanceType, Dictionary<FeatType, int>> _traitResistancesByResistance = new();

        // In-memory witness tracker: npc -> playerId -> technique feats witnessed but not yet learned.
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, Dictionary<string, HashSet<FeatType>>> _witnesses = new();

        // Observation is passive: keep its credit out of the combat points used to share active skill XP.
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, HashSet<string>> _analysisObservers = new();

        private bool _witnessSweepScheduled;

        private readonly IDBService _db;
        private readonly IQuickCastService _quickCast;
        private readonly IDMPresenceService _dmPresence;
        private readonly IPlayerFeedbackService _playerFeedback;
        private readonly IAbilityCatalog _catalog;
        private readonly IPerkService _perk;
        private readonly Lazy<IStatService> _stat;
        private readonly ISkillXPService _skill;
        private readonly Lazy<ICombatPointService> _combatPoint;

        public MimicryService(
            IDBService db,
            IQuickCastService quickCast,
            IDMPresenceService dmPresence,
            IPlayerFeedbackService playerFeedback,
            IAbilityCatalog catalog,
            IPerkService perk,
            Lazy<IStatService> stat,
            ISkillXPService skill,
            Lazy<ICombatPointService> combatPoint)
        {
            _db = db;
            _quickCast = quickCast;
            _dmPresence = dmPresence;
            _playerFeedback = playerFeedback;
            _catalog = catalog;
            _perk = perk;
            _stat = stat;
            _skill = skill;
            _combatPoint = combatPoint;
        }

        private const float WitnessRadius = 15.0f;
        private const float LearnMaxDistance = 40.0f;

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

        /// <summary>
        /// Caches technique lookups. Runs on OnModuleCacheAfter so Ability's cache (built on
        /// OnModuleCacheBefore) has already populated every AbilityDetail, including the
        /// Mimicry-specific fields set by AbilityBuilder.MimicryTechnique. A hot reload reruns it after the TLK overrides, so
        /// <see cref="AdoptTechniqueDescriptions"/> puts back the authored descriptions the boot read.
        /// </summary>
        [BootPhase(BootReload.Rerun, AdoptedBy = nameof(AdoptTechniqueDescriptions), CarriedBy = nameof(CarryTechniqueDescriptions))]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CacheData()
        {
            _techniques.Clear();
            _techniqueByNpcFeat.Clear();
            _techniqueIcons.Clear();
            _techniqueDescriptions.Clear();
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
                    bonus += MimicryTraitRules.ScaleTraitBonus(amount, potency);
            }

            return bonus;
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
        /// When a hot reload quiesces this build: leaves the authored technique descriptions it read at boot on the module for the
        /// next build (<see cref="AdoptTechniqueDescriptions"/>), which can only read the text the TLK overrides changed. It
        /// changes nothing.
        /// </summary>
        public void CarryTechniqueDescriptions()
        {
            var carry = new JObject();
            foreach (var (feat, description) in _techniqueDescriptions)
                carry[((int)feat).ToString(System.Globalization.CultureInfo.InvariantCulture)] = description;

            ReloadCarry.Put(DescriptionCarryKey, carry.ToString(Formatting.None));
        }

        /// <summary>
        /// On a hot reload, after the rerun <see cref="CacheData"/>: puts back the authored description the old build read for each
        /// technique this build still has. A technique new in this build keeps the text the rerun read.
        /// </summary>
        public void AdoptTechniqueDescriptions()
        {
            var text = ReloadCarry.Take(DescriptionCarryKey);
            if (text == null)
                return;

            foreach (var (key, description) in JObject.Parse(text))
            {
                var feat = (FeatType)int.Parse(key, System.Globalization.CultureInfo.InvariantCulture);
                if (_techniques.ContainsKey(feat))
                    _techniqueDescriptions[feat] = description.Value<string>();
            }
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

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets the player as an observer; the observers are carried.")]
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
        /// Reads a technique straight from the technique table. The key is not normalised the way
        /// <see cref="GetTechniqueDetail"/> and <see cref="IsTechnique"/> normalise theirs.
        /// </summary>
        public bool TryGetTechnique(FeatType feat, out AbilityDetail detail)
        {
            return _techniques.TryGetValue(feat, out detail);
        }

        /// <summary>
        /// Reads a technique straight from the technique table; throws <see cref="KeyNotFoundException"/> for a feat
        /// that is not a technique, as an indexer on the table does.
        /// </summary>
        public AbilityDetail GetTechnique(FeatType feat)
        {
            return _techniques[feat];
        }

        /// <summary>
        /// Finds the technique a creature's own feat teaches, and its ability detail.
        /// Returns false when the feat teaches nothing or the technique is not registered.
        /// </summary>
        public bool TryGetTechniqueForNpcFeat(FeatType npcFeat, out FeatType technique, out AbilityDetail detail)
        {
            detail = null;
            return _techniqueByNpcFeat.TryGetValue(npcFeat, out technique) &&
                   _techniques.TryGetValue(technique, out detail);
        }

        /// <summary>
        /// Every registered technique with its ability detail, read straight from the technique table without a copy.
        /// </summary>
        public IEnumerable<KeyValuePair<FeatType, AbilityDetail>> EnumerateTechniques()
        {
            return _techniques;
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
        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _catalog.GetAllAbilityDetails();

        /// <summary>The unconverted <c>Perk</c> service.</summary>
        protected virtual int GetPerkLevel(uint creature, PerkType perkType) => _perk.GetPerkLevel(creature, perkType);

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

        /// <summary>The unconverted <c>CombatPoint</c> service.</summary>
        protected virtual bool HasCombatPoints(uint player, uint creature) => _combatPoint.Value.HasCombatPoints(player, creature);

        /// <summary>The unconverted <c>Scheduler</c>.</summary>
        protected virtual void ScheduleRepeating(Action task, TimeSpan interval) => Scheduler.ScheduleRepeating(task, interval);
    }
}
