using System.Reflection;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>
    /// The fishing study: the economy-parity gate's catch-rate check, the length gate and the XP-rate gate over the fishing cohort, plus the evidence
    /// the Angler perk draft quotes, written as one markdown report in the style of the other balance reports.
    /// </summary>
    public static class FishingReport
    {
        public const string Title = "Crafting revamp: the Line (fishing fight) against today's catch roll";

        /// <summary>Plays every case in parallel, runs the two gates and the perk evidence, and returns the results the report shows.</summary>
        public static IReadOnlyList<GateResult> Run(GateContext context, FishingParity parity, FishingParityOptions options, GateThresholds thresholds)
        {
            var cohort = new FishingXpRateCohort(parity, options);
            cohort.Prepare();
            parity.Prepare(FishingAnglerEvidence.Keys());

            var scores = context.Attributes.Modifiers.Select(AttributeSweep.ScoreFor).ToList();
            var results = new List<GateResult>
            {
                new FishingCatchRateGate(parity, options).Evaluate(context),
                new FishingLengthGate(parity, options).Evaluate(context),
                new XpRateGate(cohort, thresholds).Evaluate(context)
            };

            var style = FishingAnglerEvidence.StyleBalance(parity);
            results.Add(new GateResult(
                "Evidence: Angler perk magnitudes",
                GateVerdict.NotApplicable,
                "Not a gate. What each proposed Angler purchase adds to the adaptive player's catch rate; the draft quotes it.",
                new[] { FishingAnglerEvidence.PerkMagnitudes(parity) }));
            results.Add(new GateResult(
                "Evidence: style balance",
                GateVerdict.NotApplicable,
                $"Not a gate. Patient Angler is the better style on {Percent(style.PatientShare)} of the cases and Aggressive Angler on " +
                $"{Percent(style.AggressiveShare)}; the mean gap is {Number(style.MeanGap)} points (Patient less Aggressive).",
                new[] { style.Table }));
            results.Add(new GateResult(
                "Evidence: rare fish and Trophy Catch",
                GateVerdict.NotApplicable,
                $"Not a gate. The ceiling for the capstone is today's catch chance for a level {FishingAnglerEvidence.RareFishLevel} fish at rank 50 for a " +
                $"character with Willpower {scores.Max()}: {FishingCatchRateGate.Today(50 - FishingAnglerEvidence.RareFishLevel, scores.Max())}%.",
                new[] { FishingAnglerEvidence.RareFish(parity, scores) }));
            return results;
        }

        public static string Build(
            GateContext context,
            FishingParity parity,
            FishingParityOptions options,
            GateThresholds thresholds,
            IReadOnlyList<GateResult> results,
            string command) =>
            GateReport.Build(Title, context, results, command, Inputs(context, parity, options, thresholds), Limitations(options));

        public static IReadOnlyList<string> Inputs(GateContext context, FishingParity parity, FishingParityOptions options, GateThresholds thresholds)
        {
            var scores = context.Attributes.Modifiers.Select(AttributeSweep.ScoreFor).ToList();
            var lines = new List<string>
            {
                "- Today (the catch roll the fight replaces): a bite lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, " +
                $"kept from 0 to 95. It is played at Willpower scores {string.Join(", ", scores)} (`AttributeSweep`); the corridor the fight is judged against is Willpower " +
                $"{options.CorridorLowScore} to {options.CorridorHighScore}, because the median Willpower of active characters is not measured yet.",
                $"- The fight: the Line engine, {parity.Runs} fights per case, each with its own dice from seed {parity.Seed}. No attributes are read. Fish strength comes from the fish's level " +
                "against the angler's rank (`LineSetup.ForFish`); the rod tier and the rank set the Tension cap and the reel.",
                $"- Adaptive player: {LineFights.AdaptiveOptions.Describe()}. It is a floor on skilled play. Rule-of-thumb player: reads the announced move and answers it by a fixed rule, never looking further ahead.",
                $"- Time: {Number(options.CastSeconds)} s for a cast and the wait for a bite (6-8 s in the game), in both loops; {Number(options.SecondsPerTurn)} s for each turn of the fight. Both are estimates until the client run measures them.",
                "- XP cohort (gate 6): fish-level bands 0-10, 11-20, 21-30, 31-40 and 41-52; the angler's rank equals the fish's level (at most 50) with the best rod tier that rank allows; every (rod, bait) table of every location that rod reaches counts equally, half by day and half by night, so both loops draw the same fish. XP is per bite.",
                $"- Tolerance: the adaptive player's catch rate is near today's when it sits within {Number(options.TolerancePoints, 0)} points of the corridor.",
                "- XP: paid at the bite, caught or not, as today. Its size is today's XP for the fish against the rank, times a multiplier by the fish's level band, as the game server applies it (`CraftMinigameXp`): " +
                string.Join(", ", XpBands().Select(band => $"{band.Name} {band.Multiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}x")) +
                ". Each is the least multiple of 0.05 that keeps gate 6 passing in its band at this report's runs and seed; 1.0 in every band restores today's XP per bite.",
                "- Rod tiers: Bamboo, Carbon and Clothespole 0; Composite, Ebisu and Fastwater 10; Glass Fiber, Halcyon and Judge 20; Maze Monger, Willow and Yew 30; Butterfly, Tranquility and LuShang 40.",
                "- Named starting values of the fight (`LineRules`), all tunable:"
            };

            foreach (var (name, value) in StartingValues())
                lines.Add($"  - `{name}` = {value}");

            return lines;
        }

        /// <summary>The fish-level bands of the XP cohort with the multiplier the game server puts on a bite's XP in each.</summary>
        public static IReadOnlyList<(string Name, int First, double Multiplier)> XpBands() => new[]
        {
            ("0-10", 0), ("11-20", 11), ("21-30", 21), ("31-40", 31), ("41-52", 41)
        }.Select(band => (band.Item1, band.Item2, CraftMinigameXp.MultiplierFor(CraftMinigameType.Line, band.Item2))).ToList();

        /// <summary>Every starting value of the Line, by name, as the report records them.</summary>
        public static IReadOnlyList<(string Name, string Value)> StartingValues() =>
            typeof(LineRules).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral)
                .Select(field => (field.Name, Convert.ToString(field.GetRawConstantValue(), System.Globalization.CultureInfo.InvariantCulture)))
                .ToList();

        public static IReadOnlyList<string> Limitations(FishingParityOptions options) => new[]
        {
            "- Today's catch chance depends on the character's Willpower, which is not measured for active characters yet; the corridor between Willpower 10 and 14 stands in for it.",
            "- The adaptive player is a bounded-depth expectimax with a generic fish heuristic. It is not an optimal player, and real players are less skilled; the rule-of-thumb player marks the other end. The study does not measure how real players play.",
            $"- The game places the six special fish at a random location and bait on every server start. The study fixes one placement (`FishingCatchModel.SpecialPlacementSeed` = {FishingCatchModel.SpecialPlacementSeed}) so the report can be reproduced; another placement moves the top band's mix and the pooled rows a little.",
            "- The fight has no hidden chance, so a very easy fish is landed every time by a skilled player, where today's chance stops at 95%.",
            "- The pooled table averages every pool a rod can reach with equal weight, as the legacy baseline does, and ignores the 80% chance of no bite when only the default fish is on offer. Fish per hour is not modelled: location choice, bait supply and respawn are outside the study.",
            $"- Gate 6 weighs the time a fight adds ({Number(options.SecondsPerTurn)} s a turn is unmeasured) against the XP multiplier, so the multipliers are starting values: they shrink or grow with the real seconds per turn, and a fight that turns out shorter than modelled would pay more than today's XP per hour.",
            "- The multiplier raises the XP of a bite about sevenfold, because the fight takes several times as long as the cast it follows; the catch rate per bite stays near today's, so fish per hour falls by the same factor. Gate 6 asks for it; it is flagged here for the owner.",
            "- Perks, food and status effects are not modelled except where an evidence table names a perk. The client was not run."
        };
    }
}
