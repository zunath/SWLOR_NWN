using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>One fish of a band's mix: its level, rarity and how much of the band's bites it takes.</summary>
    /// <param name="Fish">The fish.</param>
    /// <param name="Level">The fish's level.</param>
    /// <param name="Rarity">The fish's rarity.</param>
    /// <param name="Rank">The angler's rank: the fish's level, at most 50.</param>
    /// <param name="RodTier">The best rod tier that rank allows.</param>
    /// <param name="Weight">The share of the band's bites, from 0 to 1.</param>
    public sealed record FishingMixEntry(FishType Fish, int Level, FishRarity Rarity, int Rank, int RodTier, double Weight);

    /// <summary>
    /// The fishing cohort of the XP-rate gate, as the plan pins it: one case for each band of fish levels, the angler's rank equal to the
    /// fish's level (at most 50) with the best rod tier that rank allows, and every (rod, bait) table of every fishing location that rod
    /// reaches weighted equally, half by day and half by night, so both loops draw the same fish. A run is a bite: XP is paid per bite,
    /// caught or not, so what differs between the loops is the time. Today's loop spends the cast and the wait; the fight spends the
    /// same and then plays its turns, each one <see cref="FishingParityOptions.SecondsPerTurn"/>.
    /// </summary>
    public sealed class FishingXpRateCohort : IXpRateCohort
    {
        /// <summary>How many bites each case stands for, so the weights of its mix can be played as whole runs.</summary>
        public const int BitesPerCase = 10_000;

        private const string Variant = "best rod for the rank, rank = fish level (at most 50)";

        private static readonly IReadOnlyList<(string Name, int First, int Last)> Bands = new[]
        {
            ("0-10", 0, 10), ("11-20", 11, 20), ("21-30", 21, 30), ("31-40", 31, 40), ("41-52", 41, 52)
        };

        private readonly FishingParity _parity;
        private readonly FishingParityOptions _options;
        private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<FishingMixEntry>>> _mixes;

        public FishingXpRateCohort(FishingParity parity, FishingParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? FishingParityOptions.Default;
            _mixes = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<FishingMixEntry>>>(BuildMixes);
        }

        public string Name => "Fishing";

        public string RunName => "bite";

        /// <summary>Today's catch roll takes no action from the player.</summary>
        public XpLoopTiming LegacyTiming => new(0, _options.CastSeconds);

        public XpLoopTiming ReplacementTiming => new(_options.SecondsPerTurn, _options.CastSeconds);

        /// <summary>The fish a band's bites draw, with their shares.</summary>
        public IReadOnlyList<FishingMixEntry> Mix(string band) =>
            _mixes.Value.TryGetValue(band, out var mix) ? mix : throw new ArgumentException($"The cohort has no band '{band}'.");

        /// <summary>Plays the cohort's fights in parallel, so the gate that follows only reads them.</summary>
        public void Prepare()
        {
            _parity.Prepare(_mixes.Value.Values.SelectMany(mix => mix).Select(entry => new CatchKey(
                entry.Level,
                entry.Rarity,
                entry.Rank,
                entry.RodTier,
                LinePlayer.Adaptive,
                LineAnglerEffects.None)));
        }

        public IReadOnlyList<XpRateCase> Cases(GateContext context) =>
            Bands.Where(band => _mixes.Value.ContainsKey(band.Name)).Select(band => new XpRateCase(band.Name, Variant)).ToList();

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            var xp = Mix(xpCase.Group).Sum(entry => FishingCatchModel.XpPerBite(entry.Rank, entry.Level) * entry.Weight * BitesPerCase);
            return new XpLoopSample(xp, 0, BitesPerCase);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            double xp = 0, actions = 0;
            foreach (var entry in Mix(xpCase.Group))
            {
                var bites = entry.Weight * BitesPerCase;
                var sample = _parity.Catch(entry.Level, entry.Rarity, entry.Rank, entry.RodTier);
                xp += FishingCatchModel.XpPerBite(entry.Rank, entry.Level) * bites;
                actions += sample.MeanTurns * bites;
            }

            return new XpLoopSample(xp, actions, BitesPerCase);
        }

        private IReadOnlyDictionary<string, IReadOnlyList<FishingMixEntry>> BuildMixes()
        {
            var levels = FishingCatchModel.Fish().ToDictionary(fish => fish.Type, fish => fish.Level);
            var weights = new Dictionary<(string Band, FishType Fish), double>();
            foreach (var table in FishingCatchModel.Pools().GroupBy(pool => (pool.Location, pool.Rod, pool.Bait)))
            {
                // Every (rod, bait) table of a location counts once; its day and night tables split that one share.
                var split = 1.0 / table.Count();
                foreach (var pool in table)
                {
                    var total = pool.Entries.Sum(entry => (double)entry.Frequency);
                    foreach (var entry in pool.Entries)
                    {
                        var level = levels[entry.Fish];
                        var tier = FishingParity.NominalRodTier(Math.Min(level, 50));
                        if (FishingRodTier.Tier(pool.Rod) != tier)
                            continue;

                        var band = Bands.First(b => level >= b.First && level <= b.Last).Name;
                        weights[(band, entry.Fish)] = weights.GetValueOrDefault((band, entry.Fish)) + split * entry.Frequency / total;
                    }
                }
            }

            var mixes = new Dictionary<string, IReadOnlyList<FishingMixEntry>>();
            foreach (var band in Bands)
            {
                var inBand = weights.Where(pair => pair.Key.Band == band.Name).ToList();
                var total = inBand.Sum(pair => pair.Value);
                if (total <= 0)
                    continue;

                mixes[band.Name] = inBand
                    .Select(pair =>
                    {
                        var level = levels[pair.Key.Fish];
                        var rank = Math.Min(level, 50);
                        return new FishingMixEntry(
                            pair.Key.Fish, level, FishingCatchRateGate.RarityOf(pair.Key.Fish), rank, FishingParity.NominalRodTier(rank), pair.Value / total);
                    })
                    .OrderBy(entry => entry.Level)
                    .ThenBy(entry => (int)entry.Fish)
                    .ToList();
            }

            return mixes;
        }
    }
}
