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
        public const string Title = "Crafting revamp: Quick Reel and the Line (fishing fight) against today's catch roll";

        /// <summary>Plays every case in parallel, runs the two gates and the perk evidence, and returns the results the report shows.</summary>
        public static IReadOnlyList<GateResult> Run(GateContext context, FishingParity parity, FishingParityOptions options, GateThresholds thresholds)
        {
            var cohort = new FishingXpRateCohort(parity, options);
            cohort.Prepare();
            parity.Prepare(FishingAnglerEvidence.Keys());

            var results = new List<GateResult>
            {
                new FishingCatchRateGate(parity, options).Evaluate(context),
                new FishingFightBoundGate(parity, options).Evaluate(context),
                new FishingLengthGate(parity, options).Evaluate(context),
                new XpRateGate(cohort, thresholds).Evaluate(context)
            };

            // The game lets the first action of a fight resolve at once, so the fastest allowed player's fight of n actions lasts n - 1 turns.
            // The multipliers are calibrated for the typical player above; this shows what the fastest allowed pace earns against today's.
            var fastest = new FishingXpRateCohort(parity, options with { FreeOpeningTurn = true });
            var fastestGate = new XpRateGate(fastest, thresholds).Evaluate(context);
            var fastestRatios = fastest.Cases(context).Select(xpCase =>
                fastest.PlayReplacement(xpCase, context).XpPerHour(fastest.ReplacementTiming) /
                fastest.PlayLegacy(xpCase, context).XpPerHour(fastest.LegacyTiming)).ToList();
            results.Add(new GateResult(
                "Evidence: XP rate at the fastest allowed pace",
                GateVerdict.NotApplicable,
                $"Not a gate. A player who takes the free first action at once and then waits the turn each time earns {Number(fastestRatios.Min(), 2)}x to " +
                $"{Number(fastestRatios.Max(), 2)}x of today's XP per hour across the bands; the multipliers above are calibrated for the typical player, who reads the first move for a turn like the others.",
                fastestGate.Tables));

            var style = FishingAnglerEvidence.StyleBalance(parity);
            results.Add(new GateResult(
                "Evidence: Angler perk magnitudes",
                GateVerdict.NotApplicable,
                "Not a gate. What each proposed Angler purchase adds to the adaptive player's catch rate, and whether it stays within the fight bound; the draft quotes it. " +
                "The row marked Rejected is the first draft's whole line, kept to show how far over the bound it went; each of its purchases alone is within it.",
                new[] { FishingAnglerEvidence.PerkMagnitudes(parity, options) }));
            results.Add(new GateResult(
                "Evidence: style balance",
                GateVerdict.NotApplicable,
                $"Not a gate. Patient Angler is the better style on {Percent(style.PatientShare)} of the cases and Aggressive Angler on " +
                $"{Percent(style.AggressiveShare)}; the mean gap is {Number(style.MeanGap)} points (Patient less Aggressive).",
                new[] { style.Table }));
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
                "- Today (the catch roll before the Line): a bite lands the fish at `40 + 10 x (Agriculture rank - fish level) + 2 x Willpower score` percent, " +
                $"kept from 0 to 95. It is played at Willpower scores {string.Join(", ", scores)} (`AttributeSweep`); the corridor Quick Reel is judged against is Willpower " +
                $"{options.CorridorLowScore} to {options.CorridorHighScore}, because the median Willpower of active characters is not measured yet.",
                "- Quick Reel (owner decision, October 2026): on a bite the angler may resolve it at once by today's formula, with Willpower fixed at " +
                $"{options.QuickReelWillpowerScore} (`FishingQuickReel.ReferenceWillpowerScore`, the middle of the corridor and **pending the owner's attribute-median decision**), " +
                "pays today's XP for the bite and nothing more, takes no fight time and spends the bait as today. It is only a choice before the first move of a fight.",
                $"- The fight: the Line engine, {parity.Runs} fights per case, each with its own dice from seed {parity.Seed}. No attributes are read. Fish strength comes from the fish's level " +
                "against the angler's rank (`LineSetup.ForFish`); the rod tier and the rank set the Tension cap and the reel. Fighting is optional; its catch rate is bounded by Quick Reel's " +
                $"(owner decision; starting value): at most {Number(options.FightBoundRelative * 100, 0)}% more than Quick Reel's chance for the same fish and angler, plus {Number(options.FightBoundSlackPoints, 0)} points " +
                "(two standard errors of a 1000-fight case, and a floor for fish Quick Reel can hardly land). The adaptive player stands for the skilled fight.",
                $"- Adaptive player: {LineFights.AdaptiveOptions.Describe()}. It is a floor on skilled play. Rule-of-thumb player: reads the announced move and answers it by a fixed rule, never looking further ahead.",
                $"- Time: {Number(options.CastSeconds)} s for a cast and the wait for a bite (6-8 s in the game), in both loops; {Number(options.SecondsPerTurn)} s for each turn of the fight. Both are estimates until the client run measures them. The game enforces the turn as the least time between two actions (`LineRules.TurnSeconds`, the first action free), so fast clicking cannot beat the pace the XP multipliers assume.",
                "- XP cohort (gate 6): fish-level bands 0-10, 11-20, 21-30, 31-40 and 41-52; the angler's rank equals the fish's level (at most 50) with the best rod tier that rank allows; every (rod, bait) table of every location that rod reaches counts equally, half by day and half by night, so both loops draw the same fish. XP is per bite.",
                $"- XP: today's XP for the fish against the rank is paid at the bite, caught or not, as today; the rest of the multiplied XP is paid when the fight ends on its own (landed, snapped or escaped), in proportion to how long the fight lasted up to {LineRules.XpFullTurns} turns of {Number(LineRules.TurnSeconds)} s, and never for a fight the angler abandons, so neither skipping the fight nor losing it on purpose after one move earns more per hour than today. The multiplied XP is today's times a multiplier by the fish's level band, as the game server applies it (`CraftMinigameXp`): " +
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
            $"- Gate 6 weighs the time a fight adds ({Number(options.SecondsPerTurn)} s a turn is unmeasured) against the XP multiplier, so the multipliers are starting values: they shrink or grow with the real seconds per turn. The game keeps the pace from below in two ways: each action after the first waits the turn, and the rest of the multiplied XP is paid in proportion to how long the fight lasted (up to the full turns), so a fast player, or one who loses the fight on purpose after a move, earns about today's XP per hour and no more; the evidence table for the fastest allowed pace (first action at once) shows it a few percent above today's. The gate plays every fight at the modelled turn, so a player slower than that earns less than today per hour.",
            "- The multiplier raises the XP of a fight that lasts the full turns about eightfold, because the fight takes several times as long as the cast it follows; a fighter lands fewer fish per hour than a quick reeler by the same factor. Gate 6 asks for it; it is flagged here for the owner. Quick Reel pays today's XP, so it needs no multiplier.",
            $"- The bound leaves the unperked fight below Quick Reel: `LineRules.RungAtRank` ({LineRules.RungAtRank}) is set so that, with no perks, a skilled fight lands fewer fish than Quick Reel at the fish's own level and the Angler line can lift it up to the bound and no further. The fight is the XP choice and Quick Reel the fish choice until the line is bought. Lowering `RungAtRank` strengthens the unperked fight but leaves the line no room under the bound.",
            "- The Angler line is cut to what the bound allows: Strong Line and Hard Set to two steps each, Fish Sense, a style and Trophy Catch are seven of the plan's ten purchases; the other three need effects that do not raise the catch rate, or a looser bound. A rare fish ignores the rest of the line, so only Trophy Catch moves it.",
            "- Perks, food and status effects are not modelled except where an evidence table names a perk. The client was not run."
        };
    }
}
