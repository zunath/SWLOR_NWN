using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Mimicry lets players with the Combat Analyzer perk witness enemy creatures using their innate
    /// techniques in combat. When the source creature dies, witnesses roll a chance to learn the
    /// technique permanently. Learned techniques can then be equipped (within a slot budget) as
    /// ordinary active ability feats, scaled off the player's stats and the Technique Potency perk.
    /// </summary>
    public static class Mimicry
    {
        // Technique feat -> its cached ability detail. Populated after Ability's cache runs.
        private static readonly Dictionary<FeatType, AbilityDetail> _techniques = new();

        // Technique feat -> its feat.2da ICON resref. Resolved once at cache time so the Techniques
        // window never has to read the 2da per-row on open (mirrors how perks cache detail.IconResref).
        private static readonly Dictionary<FeatType, string> _techniqueIcons = new();

        // Source NPC feat -> the technique feat it teaches.
        private static readonly Dictionary<FeatType, FeatType> _techniqueByNpcFeat = new();

        // Stat -> the trait feats that adjust it, and by how much. Inverted at cache time so the stat
        // pipeline (a hot path) only walks traits that are relevant to the stat being queried.
        private static readonly Dictionary<StatType, Dictionary<FeatType, int>> _traitStatsByStat = new();

        // Resistance -> the trait feats that adjust it, and by how much.
        private static readonly Dictionary<ResistanceType, Dictionary<FeatType, int>> _traitResistancesByResistance = new();

        // In-memory witness tracker: npc -> playerId -> technique feats witnessed but not yet learned.
        private static readonly Dictionary<uint, Dictionary<string, HashSet<FeatType>>> _witnesses = new();

        private static bool _witnessSweepScheduled;

        private const float WitnessRadius = 15.0f;
        private const float LearnMaxDistance = 40.0f;

        private const int BaseSlotsWithAnalyzer = 2;
        private const int SlotsPerAnalyzerMemoryLevel = 2;
        private const int OverclockedAnalyzerSlotBonus = 2;
        private const int ResonancePotencyPerTechnique = 5;
        private const int ResonancePotencyCap = 20;
        private const int AnalysisCombatPointsPerWitness = 1;

        private const int BaseLearnChancePercent = 20;
        private const int LearnChancePerRankDelta = 2;
        private const int LearnChancePerPatternRecognitionLevel = 10;
        private const int LearnChancePerPerceptionPoint = 1;
        private const int PerceptionLearnChanceBaseline = 10;
        private const int MaxLearnChancePercent = 75;
        private const int LearnTechniqueXP = 400;
        private const string LearnTechniqueSound = "gui_prompt";

        private const int TotalHotBarSlots = 36;
        private const int AutoAddHotBarSlots = 11;

        /// <summary>
        /// Caches technique lookups. Runs on OnModuleCacheAfter so Ability's cache (built on
        /// OnModuleCacheBefore) has already populated every AbilityDetail, including the
        /// Mimicry-specific fields set by AbilityBuilder.MimicryTechnique.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            _techniques.Clear();
            _techniqueByNpcFeat.Clear();
            _techniqueIcons.Clear();
            _traitStatsByStat.Clear();
            _traitResistancesByResistance.Clear();

            if (!_witnessSweepScheduled)
            {
                Scheduler.ScheduleRepeating(SweepStaleWitnesses, TimeSpan.FromMinutes(5));
                _witnessSweepScheduled = true;
            }

            foreach (var (feat, detail) in Ability.GetAllAbilityDetails())
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

            // Resolve icon resrefs in a separate, guarded pass: Get2DAString requires a live engine,
            // so a unit-test harness (no NWNCore.Init) would otherwise throw and abort the technique
            // caching above. Icons are only consumed by the live Techniques UI, so leaving them
            // unresolved in that harness is harmless.
            try
            {
                foreach (var feat in _techniques.Keys)
                    _techniqueIcons[feat] = Get2DAString("feat", "ICON", (int)feat);
            }
            catch
            {
                _techniqueIcons.Clear();
            }
        }

        /// <summary>
        /// Returns each equipped trait's stat payload with the parameters declared by that trait.
        /// </summary>
        public static IEnumerable<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat)
        {
            if (!_traitStatsByStat.ContainsKey(payloadStat) ||
                !GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                yield break;

            var player = DB.Get<Player>(GetObjectUUID(creature));
            if (player == null)
                yield break;

            foreach (var feat in player.EquippedTechniques)
            {
                if (_techniques.TryGetValue(feat, out var detail) && detail.IsMimicryTrait &&
                    detail.MimicryTraitStats.TryGetValue(payloadStat, out var value) && value != 0)
                    yield return new StatAdjustmentSource($"trait:{(int)feat}", detail.MimicryTraitStats);
            }
        }

        /// <summary>
        /// Sums equipped trait adjustments and the elemental-resonance set bonus. These belong
        /// to the loadout rather than status effects that could be cleared on death.
        /// </summary>
        public static int GetStatBonus(uint creature, StatType stat)
        {
            var hasStatContributors = _traitStatsByStat.TryGetValue(stat, out var contributors);

            if (!hasStatContributors && stat != StatType.MimicryPotencyPercent)
                return 0;

            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return 0;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
            if (dbPlayer == null)
                return 0;

            var bonus = stat == StatType.MimicryPotencyPercent
                ? GetSetBonusPotency(dbPlayer)
                : 0;

            if (!hasStatContributors)
                return bonus;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (contributors.TryGetValue(feat, out var amount))
                    bonus += amount;
            }

            return bonus;
        }

        /// <summary>
        /// Sums the resistance adjustments contributed by a creature's equipped Mimicry traits.
        /// </summary>
        public static int GetResistanceBonus(uint creature, ResistanceType resistance)
        {
            if (!_traitResistancesByResistance.TryGetValue(resistance, out var contributors))
                return 0;

            if (!GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return 0;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
            if (dbPlayer == null)
                return 0;

            var bonus = 0;
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (contributors.TryGetValue(feat, out var amount))
                    bonus += amount;
            }

            return bonus;
        }

        /// <summary>
        /// Returns the cached feat.2da ICON resref for a technique, resolved once at cache time.
        /// Empty string if the feat is not a registered technique.
        /// </summary>
        public static string GetTechniqueIcon(FeatType feat)
        {
            return _techniqueIcons.TryGetValue(feat, out var icon) ? icon : string.Empty;
        }

        /// <summary>
        /// Called from the ability-use pipeline whenever a non-player creature uses a registered feat.
        /// If the feat maps to a technique, every nearby player who has the Combat Analyzer perk and
        /// hasn't already learned the technique has the witness recorded (used at the creature's death
        /// to roll for learning). Sends a one-time floating text per (npc, player, technique).
        /// </summary>
        /// <param name="activator">The creature that used the ability.</param>
        /// <param name="npcFeat">The feat that was used.</param>
        public static void OnCreatureAbilityUsed(uint activator, FeatType npcFeat)
        {
            if (!GetIsObjectValid(activator) || GetIsPC(activator))
                return;

            if (!_techniqueByNpcFeat.TryGetValue(npcFeat, out var techniqueFeat))
                return;

            if (!_techniques.TryGetValue(techniqueFeat, out var techniqueDetail))
                return;

            var area = GetArea(activator);
            var nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, activator, nth);

            while (GetIsObjectValid(nearby) && GetDistanceBetween(activator, nearby) <= WitnessRadius)
            {
                if (!GetIsDM(nearby) && GetArea(nearby) == area)
                {
                    TryRecordWitness(activator, nearby, techniqueFeat, techniqueDetail);
                    TryAwardAnalysisCombatPoint(activator, nearby);
                }

                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, activator, nth);
            }
        }

        /// <summary>
        /// Records a witness entry for a player/technique pair, if they qualify and haven't already
        /// been recorded. Sends the "recording" floating text exactly once per (npc, player, technique).
        /// </summary>
        /// <summary>
        /// Grants a Mimicry combat point toward a creature when a player with the Combat Analyzer
        /// witnesses it use a technique, provided the player is already engaged with it (has earned
        /// combat points against it). The point converts to Mimicry XP when the creature dies, giving
        /// an ongoing analysis-driven leveling source alongside learning and using techniques. Unlike
        /// witness recording, this keeps paying out after the technique has already been learned.
        /// </summary>
        private static void TryAwardAnalysisCombatPoint(uint npc, uint player)
        {
            if (Perk.GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            if (!CombatPoint.HasCombatPoints(player, npc))
                return;

            CombatPoint.AddCombatPoint(player, npc, SkillType.Mimicry, AnalysisCombatPointsPerWitness);
        }

        private static void TryRecordWitness(uint npc, uint player, FeatType techniqueFeat, AbilityDetail techniqueDetail)
        {
            if (Perk.GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
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
            // but the feedback makes clear it cannot be learned yet and what rank it needs.
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var requiredSkillRank = techniqueDetail.MimicrySkillRequirement;

            if (skillRank < requiredSkillRank)
            {
                SendMessageToPC(player, ColorToken.Gray(
                    $"Your combat analyzer detects {techniqueDetail.Name}, but the pattern is beyond your current analysis level. (Requires Mimicry {requiredSkillRank})"));
                return;
            }

            SendMessageToPC(player, ColorToken.Cyan($"Your combat analyzer records {techniqueDetail.Name}..."));
        }

        /// <summary>
        /// When a creature dies, every player who witnessed one of its techniques gets a chance to
        /// learn it permanently. The witness cache entry for this creature is always cleared afterward.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnCreatureDeath()
        {
            var npc = OBJECT_SELF;

            if (_witnesses.TryGetValue(npc, out var byPlayer) && byPlayer.Count > 0)
            {
                var nth = 1;
                var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, npc, nth);

                while (GetIsObjectValid(nearby))
                {
                    if (GetDistanceBetween(npc, nearby) > LearnMaxDistance)
                        break;

                    TryLearnTechniques(npc, nearby, byPlayer);

                    nth++;
                    nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, npc, nth);
                }
            }

            _witnesses.Remove(npc);
        }

        /// <summary>
        /// Periodically removes witness entries for creatures that no longer exist. Creatures
        /// normally clear their entry on death, but despawns, DestroyObject calls, and area
        /// unloads never fire OnCreatureDeathAfter, so their entries would otherwise persist
        /// for the life of the server.
        /// </summary>
        private static void SweepStaleWitnesses()
        {
            if (_witnesses.Count <= 0)
                return;

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
        }

        /// <summary>
        /// Rolls a learn chance for every technique a specific player witnessed on this creature.
        /// </summary>
        private static void TryLearnTechniques(uint npc, uint player, Dictionary<string, HashSet<FeatType>> byPlayer)
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

            var dbPlayer = DB.Get<Player>(playerId);
            var skillRank = dbPlayer.Skills.TryGetValue(SkillType.Mimicry, out var mimicrySkill) ? mimicrySkill.Rank : 0;
            var patternRecognitionLevel = Perk.GetPerkLevel(player, PerkType.PatternRecognition);
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
                    // Give explicit feedback on a failed roll so a miss is distinguishable from
                    // "no roll happened". The witness entry for this creature is cleared on its
                    // death, so the player must analyze the technique again on another creature.
                    SendMessageToPC(player, ColorToken.Orange(
                        $"Your combat analyzer failed to decode {detail.Name}. Analyze it again to retry."));
                    continue;
                }

                dbPlayer.LearnedTechniques[feat] = DateTime.UtcNow;
                learnedDetails.Add(detail);
            }

            if (learnedDetails.Count <= 0)
                return;

            // Persist the learned techniques before GiveSkillXP runs - it performs its own
            // DB.Get/DB.Set of this player, so saving our stale copy afterward would clobber the XP.
            DB.Set(dbPlayer);

            foreach (var detail in learnedDetails)
            {
                SendMessageToPC(player, ColorToken.Green($"You learned the technique: {detail.Name}!"));
                Skill.GiveSkillXP(player, SkillType.Mimicry, LearnTechniqueXP);

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
        public static int CalculateLearnChance(int skillRank, int requiredSkillRank, int patternRecognitionLevel, int perception)
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
        public static int GrantAllTechniques(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return 0;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var learnedCount = 0;
            foreach (var feat in _techniques.Keys)
            {
                if (dbPlayer.LearnedTechniques.ContainsKey(feat))
                    continue;

                dbPlayer.LearnedTechniques[feat] = DateTime.UtcNow;
                learnedCount++;
            }

            if (learnedCount > 0)
                DB.Set(dbPlayer);

            return learnedCount;
        }

        /// <summary>
        /// Returns the maximum number of technique slots available to a player.
        /// Zero if the player does not have the Combat Analyzer perk.
        /// </summary>
        public static int GetMaxSlots(uint player)
        {
            if (!GetIsPC(player))
                return 0;

            var playerId = GetObjectUUID(player);
            return GetMaxSlots(DB.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the maximum number of technique slots available to a player's database record.
        /// </summary>
        public static int GetMaxSlots(Player dbPlayer)
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
        public static int GetSetBonusPotency(Player dbPlayer)
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
        public static int GetUsedSlots(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetUsedSlots(DB.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns the number of technique slots currently used by a player database record's equipped techniques.
        /// </summary>
        public static int GetUsedSlots(Player dbPlayer)
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
        public static bool CanEquip(uint player, FeatType feat, out string error)
        {
            var playerId = GetObjectUUID(player);
            return CanEquip(player, DB.Get<Player>(playerId), feat, out error);
        }

        /// <summary>
        /// Determines whether a player can equip a given technique right now, using an
        /// already-fetched player database record to avoid duplicate round trips.
        /// </summary>
        public static bool CanEquip(uint player, Player dbPlayer, FeatType feat, out string error)
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
        public static bool EquipTechnique(uint player, FeatType feat)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!CanEquip(player, dbPlayer, feat, out var error))
            {
                SendMessageToPC(player, ColorToken.Red(error));
                return false;
            }

            dbPlayer.EquippedTechniques.Add(feat);
            DB.Set(dbPlayer);

            GrantTechniqueFeat(player, feat);
            Gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips a technique, removing the underlying feat and any hotbar slot referencing it.
        /// </summary>
        public static bool UnequipTechnique(uint player, FeatType feat)
        {
            if (!GetIsPC(player))
                return false;

            if (GetIsInCombat(player))
            {
                SendMessageToPC(player, ColorToken.Red("You cannot manage techniques while in combat."));
                return false;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (!dbPlayer.EquippedTechniques.Remove(feat))
                return false;

            DB.Set(dbPlayer);
            RevokeTechniqueFeat(player, feat);
            Gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

            return true;
        }

        /// <summary>
        /// Unequips techniques that exceed the player's current Mimicry rank, then removes the
        /// newest-equipped techniques until the remaining loadout fits the current slot budget.
        /// Used after progression changes and on login to keep persisted loadouts valid.
        /// </summary>
        public static void EnforceSlotBudget(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            EnforceSlotBudget(player, DB.Get<Player>(playerId));
        }

        /// <summary>
        /// Enforces Mimicry rank requirements and the slot budget against an already-fetched player
        /// database record, avoiding a duplicate round trip when the caller has one in hand.
        /// </summary>
        public static void EnforceSlotBudget(uint player, Player dbPlayer)
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
                DB.Set(dbPlayer);
                Gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());
            }
        }

        /// <summary>
        /// Immediately removes equipped techniques whose rank requirement is no longer met when
        /// Mimicry loses a rank. This must run for every Mimicry decay, including rank losses that
        /// do not cross a perk requirement and therefore do not trigger a perk refund callback.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorLoseSkill)]
        public static void OnMimicrySkillDecay()
        {
            var skillType = (SkillType)Convert.ToInt32(EventsPlugin.GetEventData("SKILL_TYPE_ID"));
            if (skillType != SkillType.Mimicry)
                return;

            EnforceSlotBudget(OBJECT_SELF);
        }

        /// <summary>
        /// Unequips every equipped technique. Used when the Combat Analyzer perk is refunded.
        /// </summary>
        public static void UnequipAllTechniques(uint player)
        {
            if (!GetIsPC(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            if (dbPlayer.EquippedTechniques.Count <= 0)
                return;

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                RevokeTechniqueFeat(player, feat);
            }

            var unequippedCount = dbPlayer.EquippedTechniques.Count;
            dbPlayer.EquippedTechniques.Clear();
            DB.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new TechniqueChangedRefreshEvent());

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
        public static void OnPlayerLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (Perk.GetPerkLevel(player, PerkType.CombatAnalyzer) < 1)
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            EnforceSlotBudget(player, dbPlayer);

            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                GrantTechniqueFeat(player, feat);
            }

        }

        /// <summary>
        /// Applies an equipped technique to the player. Active techniques grant the underlying feat
        /// (if missing) and add it to the hotbar, mirroring the grant + hotbar logic used by the perk
        /// system's active ability feats. Trait techniques are passive and are never granted as a
        /// usable feat: their stats and resistances are derived straight from the equipped loadout, so
        /// there is no grant or revoke step for them here, and any stale feat/hotbar entry is stripped
        /// so a trait can never end up castable or on the quickbar.
        /// </summary>
        private static void GrantTechniqueFeat(uint player, FeatType feat)
        {
            if (!GetIsObjectValid(player))
                return;

            var detail = GetTechniqueDetail(feat);
            if (detail != null && detail.IsMimicryTrait)
            {
                // Passive traits are never usable: they must not be granted as a castable feat nor
                // placed on the hotbar. Strip any feat/hotbar entry an earlier grant (or a prior
                // version that granted the feat unconditionally) left behind. The trait's stats need
                // no grant step - the stat pipeline reads them from the equipped list directly.
                if (GetHasFeat(feat, player))
                    CreaturePlugin.RemoveFeat(player, feat);

                RemoveFeatFromHotBar(player, feat);
                return;
            }

            // Active technique: grant the feat and add it to the hotbar, using the same rules as a
            // perk-purchased active ability (register + ImpactAction gate, first empty auto-add slot).
            if (!GetHasFeat(feat, player))
                CreaturePlugin.AddFeat(player, feat);

            AddFeatToHotBar(player, feat);
        }

        /// <summary>
        /// Removes the technique's feat, any hotbar slot referencing it, and persistent effects
        /// declared by the ability. Trait stats need no explicit revoke step; they stop applying as
        /// soon as the feat leaves the equipped list.
        /// </summary>
        private static void RevokeTechniqueFeat(uint player, FeatType feat)
        {
            if (!GetIsObjectValid(player))
                return;

            var detail = GetTechniqueDetail(feat);
            if (detail != null)
            {
                foreach (var statusEffectType in detail.StatusEffectTypesRemovedOnPerkRefund)
                {
                    StatusEffect.RemoveStatusEffect(player, statusEffectType, false);
                }

                foreach (var statusEffectType in detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund)
                {
                    StatusEffect.RemoveStatusEffectsFromAllTargetsBySource(
                        player,
                        statusEffectType,
                        false);
                }
            }

            CreaturePlugin.RemoveFeat(player, feat);
            RemoveFeatFromHotBar(player, feat);
        }

        private static bool IsFeatOnHotBar(uint player, FeatType feat)
        {
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat && quickBarSlot.INTParam1 == (int)feat)
                    return true;
            }

            return false;
        }

        private static void AddFeatToHotBar(uint player, FeatType feat)
        {
            if (!Ability.IsFeatRegistered(feat) || Ability.GetAbilityDetail(feat).ImpactAction == null)
                return;

            if (IsFeatOnHotBar(player, feat))
                return;

            var quickBarSlot = PlayerQuickBarSlot.UseFeat(feat);

            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(player, slot, quickBarSlot);
                return;
            }
        }

        private static void RemoveFeatFromHotBar(uint player, FeatType feat)
        {
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat && quickBarSlot.INTParam1 == (int)feat)
                {
                    PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        /// <summary>
        /// Returns every technique a player has learned, alongside its cached ability detail.
        /// </summary>
        public static List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetLearnedTechniques(DB.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has learned, alongside its cached ability detail.
        /// </summary>
        public static List<(FeatType Feat, AbilityDetail Detail)> GetLearnedTechniques(Player dbPlayer)
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
        public static List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(uint player)
        {
            var playerId = GetObjectUUID(player);
            return GetEquippedTechniques(DB.Get<Player>(playerId));
        }

        /// <summary>
        /// Returns every technique a player database record has equipped, alongside its cached ability detail.
        /// </summary>
        public static List<(FeatType Feat, AbilityDetail Detail)> GetEquippedTechniques(Player dbPlayer)
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
        public static bool IsTechniqueEquipped(uint player, FeatType feat)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            return IsTechniqueEquipped(DB.Get<Player>(playerId), feat);
        }

        /// <summary>
        /// Returns true when a technique feat is present in an already-fetched player record's
        /// equipped loadout.
        /// </summary>
        public static bool IsTechniqueEquipped(Player dbPlayer, FeatType feat)
        {
            return dbPlayer?.EquippedTechniques.Contains(feat) == true;
        }

        /// <summary>
        /// Retrieves the cached ability detail for a technique feat, or null if the feat isn't a technique.
        /// </summary>
        public static AbilityDetail GetTechniqueDetail(FeatType feat)
        {
            return _techniques.TryGetValue(feat, out var detail) ? detail : null;
        }

        /// <summary>
        /// Returns true if the given feat is a registered Mimicry technique.
        /// </summary>
        public static bool IsTechnique(FeatType feat)
        {
            return _techniques.ContainsKey(feat);
        }

        /// <summary>
        /// Returns the total number of registered techniques.
        /// </summary>
        public static int TechniqueCount => _techniques.Count;
    }
}
