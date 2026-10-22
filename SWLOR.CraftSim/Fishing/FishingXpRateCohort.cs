using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The fishing cohort of the XP-rate gate: one case for each band of fish levels, with the angler's rank equal to the fish's level
    /// (kept to 50) and the best rod for the rank, every fish of the band weighted equally. A run is a bite: XP is paid per bite, caught
    /// or not, as it was before the fight, so what changes between the loops is the time. Today's loop spends the cast and the wait; the
    /// fight spends the same and then plays its turns, each one <see cref="FishingParityOptions.SecondsPerTurn"/>.
    /// </summary>
    public sealed class FishingXpRateCohort : IXpRateCohort
    {
        private static readonly IReadOnlyList<(string Name, int First, int Last)> Bands = new[]
        {
            ("0-9", 0, 9), ("10-19", 10, 19), ("20-29", 20, 29), ("30-39", 30, 39), ("40-49", 40, 49), ("50-52", 50, 52)
        };

        private const string Variant = "rank = fish level (at most 50)";

        private readonly FishingParity _parity;
        private readonly FishingParityOptions _options;

        public FishingXpRateCohort(FishingParity parity, FishingParityOptions options = null)
        {
            _parity = parity ?? throw new ArgumentNullException(nameof(parity));
            _options = options ?? FishingParityOptions.Default;
        }

        /// <summary>Plays the cohort's fights in parallel, so the gate that follows only reads them.</summary>
        public void Prepare()
        {
            _parity.Prepare(Bands.SelectMany(band => FishIn(band)).Select(fish => new CatchKey(
                fish.Level,
                RarityOf(fish),
                RankFor(fish),
                FishingParity.NominalRodTier(RankFor(fish)),
                LinePlayer.Adaptive,
                LineAnglerEffects.None)));
        }

        public string Name => "Fishing";

        public string RunName => "bite";

        /// <summary>Today's catch roll takes no action from the player.</summary>
        public XpLoopTiming LegacyTiming => new(0, _options.CastSeconds);

        public XpLoopTiming ReplacementTiming => new(_options.SecondsPerTurn, _options.CastSeconds);

        public IReadOnlyList<XpRateCase> Cases(GateContext context) =>
            Bands.Where(band => FishIn(band).Any()).Select(band => new XpRateCase(band.Name, Variant)).ToList();

        public XpLoopSample PlayLegacy(XpRateCase xpCase, GateContext context)
        {
            double xp = 0;
            var runs = 0;
            foreach (var fish in FishIn(xpCase))
            {
                xp += FishingCatchModel.XpPerBite(RankFor(fish), fish.Level) * _parity.Runs;
                runs += _parity.Runs;
            }

            return new XpLoopSample(xp, 0, runs);
        }

        public XpLoopSample PlayReplacement(XpRateCase xpCase, GateContext context)
        {
            double xp = 0, actions = 0;
            var runs = 0;
            foreach (var fish in FishIn(xpCase))
            {
                var rank = RankFor(fish);
                var sample = _parity.Catch(fish.Level, RarityOf(fish), rank, FishingParity.NominalRodTier(rank));

                xp += FishingCatchModel.XpPerBite(rank, fish.Level) * sample.Runs;
                actions += sample.Turns;
                runs += sample.Runs;
            }

            return new XpLoopSample(xp, actions, runs);
        }

        private static int RankFor(FishInfo fish) => Math.Min(fish.Level, 50);

        private static FishRarity RarityOf(FishInfo fish) => FishingCatchRateGate.RarityOf(fish.Type);

        private static IEnumerable<FishInfo> FishIn((string Name, int First, int Last) band) =>
            FishingCatchModel.Fish().Where(fish => fish.Level >= band.First && fish.Level <= band.Last);

        private static IEnumerable<FishInfo> FishIn(XpRateCase xpCase) =>
            FishIn(Bands.FirstOrDefault(band => band.Name == xpCase.Group) is { Name: not null } found
                ? found
                : throw new ArgumentException($"The cohort has no band '{xpCase.Group}'."));
    }
}
