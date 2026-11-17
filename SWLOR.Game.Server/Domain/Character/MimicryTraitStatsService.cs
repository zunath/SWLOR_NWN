using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The stats a player's equipped Mimicry traits and the elemental-resonance set bonus add (part 4 of the stat split,
    /// Readmes/MaintainabilityRefactorStatSplit.md). The reads moved from <c>MimicryService</c> unchanged. The service builds its
    /// own tables from the ability catalog in its cache handler, which runs right after the Mimicry service's, so the stat read
    /// reaches no service above Character; the reads that scale by Trait Potency read it from the stat reader their caller passes.
    /// </summary>
    public class MimicryTraitStatsService : IMimicryTraitStatsService
    {
        private const int ResonancePotencyPerTechnique = 5;
        private const int ResonancePotencyCap = 20;

        // Technique feat -> its cached ability detail. Populated after Ability's cache runs.
        private readonly Dictionary<FeatType, AbilityDetail> _techniques = new();

        // Stat -> the trait feats that adjust it, and by how much. Inverted at cache time so the stat
        // pipeline (a hot path) only walks traits that are relevant to the stat being queried.
        private readonly Dictionary<StatType, Dictionary<FeatType, int>> _traitStatsByStat = new();

        private readonly IDBService _db;
        private readonly IAbilityCatalog _catalog;

        public MimicryTraitStatsService(IDBService db, IAbilityCatalog catalog)
        {
            _db = db;
            _catalog = catalog;
        }

        /// <summary>
        /// Caches the techniques and the trait stat tables. Runs on OnModuleCacheAfter, right after the Mimicry service's own
        /// cache (a split of its handler, <c>tools/Refactor/HandlerRenames.txt</c>), so Ability's cache (built on
        /// OnModuleCacheBefore) has already populated every AbilityDetail, including the Mimicry-specific fields set by
        /// AbilityBuilder.MimicryTechnique. A hot reload reruns it.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CacheData()
        {
            _techniques.Clear();
            _traitStatsByStat.Clear();

            foreach (var (feat, detail) in _catalog.GetAllAbilityDetails())
            {
                if (!detail.IsMimicryTechnique)
                    continue;

                _techniques[feat] = detail;

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
            }
        }

        /// <summary>
        /// Returns each equipped trait's stat payload with the parameters declared by that trait.
        /// </summary>
        public IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat, IStatService statReader)
        {
            if (!_traitStatsByStat.ContainsKey(payloadStat) ||
                !GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
                return Array.Empty<StatAdjustmentSource>();

            var player = _db.Get<Player>(GetObjectUUID(creature));
            if (player == null)
                return Array.Empty<StatAdjustmentSource>();

            var potency = statReader.GetStatAdjustment(creature, StatType.MimicryTraitPotencyPercent);
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
        public int GetStatBonus(uint creature, StatType stat, IStatService statReader)
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
                : statReader.GetStatAdjustment(creature, StatType.MimicryTraitPotencyPercent);
            foreach (var feat in dbPlayer.EquippedTechniques)
            {
                if (contributors.TryGetValue(feat, out var amount))
                    bonus += MimicryTraitRules.ScaleTraitBonus(amount, potency);
            }

            return bonus;
        }

        private Dictionary<StatType, int> ScaleTraitStats(IReadOnlyDictionary<StatType, int> stats, int potency)
        {
            var scaled = new Dictionary<StatType, int>(stats.Count);
            foreach (var (stat, amount) in stats)
                scaled[stat] = MimicryTraitRules.ScaleTraitBonus(amount, potency);
            return scaled;
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
    }
}
