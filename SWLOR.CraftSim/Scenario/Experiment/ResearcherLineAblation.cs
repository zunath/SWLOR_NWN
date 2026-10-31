using System.Text;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.CraftSim.Report.MarkdownTable;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// Which shared purchase of the Researcher line moves the balance between Methodical and Intuitive. Each row compares the two styles
    /// with the style-balance thresholds of <see cref="ResearcherPerkStudy"/> (a score of the reduction won or the time cut, six cases
    /// per comparison), once with one shared purchase taken out of the whole line and once with it added to the styles alone, so a
    /// purchase that favors one style shows up as the row where the gap moves.
    /// </summary>
    public static class ResearcherLineAblation
    {
        /// <summary>One build of the shared purchases and what the style comparison made of it.</summary>
        public sealed record Row(
            string Name, int Failed, double FixedGap, double ContestedGap, double MethodicalActions, double IntuitiveActions);

        private static readonly PerkType[] Styles = { PerkType.Methodical, PerkType.Intuitive };

        /// <summary>The purchases both styles can own.</summary>
        public static IReadOnlyList<PerkType> SharedPerks { get; } =
            ResearcherBuild.AllPerks.Where(perk => !Styles.Contains(perk)).ToList();

        /// <summary>Compares the styles when both own exactly these shared purchases.</summary>
        public static Row Evaluate(ResearcherPerkStudyOptions options, string name, IReadOnlyCollection<PerkType> shared)
        {
            var methodical = new ResearcherBuild("Methodical", shared.Append(PerkType.Methodical));
            var intuitive = new ResearcherBuild("Intuitive", shared.Append(PerkType.Intuitive));
            var rows = ResearcherPerkStudy.CompareStyles(options, options.Gear, methodical.TuningAt, intuitive.TuningAt);
            return new Row(
                name,
                rows.Count(row => !row.Passed),
                rows.Where(row => row.Level == ResearcherPerkStudy.FixedLevel).Average(row => row.Gap),
                rows.Where(row => row.Level == ResearcherPerkStudy.ContestedLevel).Average(row => row.Gap),
                rows.Average(row => row.FirstActions),
                rows.Average(row => row.SecondActions));
        }

        public static string Build(ResearcherPerkStudyOptions options, string command)
        {
            if (options.Gear == null)
                throw new ArgumentException("The ablation needs gear levels.", nameof(options));

            var text = new StringBuilder();
            text.AppendLine("# Crafting revamp: which shared Researcher purchase moves the style balance");
            text.AppendLine();
            text.AppendLine($"Command: `{command}`");
            text.AppendLine();
            text.AppendLine("## Inputs");
            text.AppendLine();
            text.AppendLine($"- Seed {options.Seed}; {options.Runs} experiments per case and build, every build on the same dice, as `researcher-perks`. Both styles come from the perk definitions.");
            text.AppendLine("- A row is Methodical against Intuitive when both own the shared purchases named. \"Failed\" counts the six comparisons (two level kinds by three gear levels, six cases each) outside the style-balance gate's thresholds; the gaps are the mean score gap over the level kind (positive favors Methodical).");
            text.AppendLine("- The shared purchases: " + string.Join(", ", SharedPerks.Select(ResearcherBuild.NameOf)) + ".");
            text.AppendLine($"- The line under test, read from the perk definitions: {DescribeLine()}. The candidate rows and the grid change these shared purchases and nothing else; a rank below a purchase's own rank repeats the definition's.");
            text.AppendLine();

            var wholeLine = Evaluate(options, "Whole line", SharedPerks);
            var rows = new List<Row> { Evaluate(options, "Styles alone", Array.Empty<PerkType>()), wholeLine };
            rows.AddRange(SharedPerks.Select(perk => Evaluate(options, $"Whole line without {ResearcherBuild.NameOf(perk)}", SharedPerks.Where(p => p != perk).ToList())));

            text.AppendLine("## The whole line, one purchase taken out");
            text.AppendLine();
            text.AppendLine("A purchase whose removal moves the gap toward zero, or the failures toward none, is one that favors a style.");
            text.AppendLine();
            text.AppendLine(Table(rows));

            var added = SharedPerks.Select(perk => Evaluate(options, $"Styles with {ResearcherBuild.NameOf(perk)}", new[] { perk })).ToList();
            text.AppendLine("## The styles alone, one purchase added");
            text.AppendLine();
            text.AppendLine(Table(new[] { rows[0] }.Concat(added)));

            text.AppendLine("## Candidate rebalances of the line");
            text.AppendLine();
            text.AppendLine("Each row changes only a shared purchase (the styles keep their numbers) and compares the styles on the whole line, as the table above. \"Failed\" counts the six comparisons outside the thresholds.");
            text.AppendLine();
            var candidates = Candidates.Select(candidate => EvaluateCandidate(options, candidate)).ToList();
            text.AppendLine(Table(candidates));

            text.AppendLine("## Grid: where Broader Hypotheses sits and how large Field Notes is");
            text.AppendLine();
            text.AppendLine("Failed comparisons of six, the styles on the whole line. Rows: the Fabrication rank Broader Hypotheses first applies at (\"never\" takes it out). Columns: Field Notes' Test chance as a multiple of today's (3 points, then 6).");
            text.AppendLine();
            text.AppendLine(Grid(options));
            return text.ToString();
        }

        /// <summary>Where Broader Hypotheses and Second Opinion sit and how large Field Notes is, as the perk definitions have them.</summary>
        public static string DescribeLine()
        {
            var ranks = Enumerable.Range(1, 10).Select(step => step * 5).ToList();
            int? FirstRank(PerkType perk, Func<ExperimentTuning, bool> changed) =>
                ranks.Cast<int?>().FirstOrDefault(rank => changed(ResearcherBuild.Alone(perk).TuningAt(rank.Value)));
            var trials = FirstRank(PerkType.BroaderHypotheses, tuning => tuning.Trials != ExperimentTuning.Default.Trials);
            var refine = FirstRank(PerkType.SecondOpinion, tuning => tuning.RefineChanceBonus != ExperimentTuning.Default.RefineChanceBonus);
            var notes = ranks.Select(rank => (Rank: rank, Chance: ResearcherBuild.Alone(PerkType.FieldNotes).TuningAt(rank).TestChanceBonusPercent))
                .Where(item => item.Chance > 0).ToList();
            return $"Broader Hypotheses (+1 trial) from rank {trials}, Second Opinion from rank {refine}, Field Notes " +
                   string.Join(" then ", notes.GroupBy(item => item.Chance).Select(group => $"+{group.Key} from rank {group.First().Rank}"));
        }

        /// <summary>A change to the shared purchases, tried without editing the perk definitions: the tuning a style gets at a rank.</summary>
        public sealed record Candidate(string Name, Func<PerkType, int, ExperimentTuning> Tuning);

        private static ExperimentTuning Line(PerkType style, int rank, params PerkType[] without) =>
            new ResearcherBuild("line", SharedPerks.Where(perk => !without.Contains(perk)).Append(style)).TuningAt(rank);

        /// <summary>Test chance Field Notes gives at a rank, so a candidate can scale it.</summary>
        private static int FieldNotesChance(int rank) => ResearcherBuild.Alone(PerkType.FieldNotes).TuningAt(rank).TestChanceBonusPercent;

        public static IReadOnlyList<Candidate> Candidates { get; } = new[]
        {
            new Candidate("The line as it is", (style, rank) => Line(style, rank)),
            new Candidate("Broader Hypotheses from rank 35 (not 15)", (style, rank) => rank < 35 ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank)),
            new Candidate("Broader Hypotheses from rank 45 (not 15)", (style, rank) => rank < 45 ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank)),
            new Candidate("No Broader Hypotheses", (style, rank) => Line(style, rank, PerkType.BroaderHypotheses)),
            new Candidate("Field Notes doubled", (style, rank) =>
                Line(style, rank) is var tuning ? tuning with { TestChanceBonusPercent = tuning.TestChanceBonusPercent + FieldNotesChance(rank) } : null),
            new Candidate("Broader Hypotheses from rank 40 (not 15)", (style, rank) => rank < 40 ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank)),
            new Candidate("Swap Broader Hypotheses (15) and Field Notes II (40)", (style, rank) =>
                (rank < 40 ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank)) is var tuning
                    ? tuning with { TestChanceBonusPercent = tuning.TestChanceBonusPercent + (rank >= 15 && rank < 40 ? FieldNotesChance(40) - FieldNotesChance(15) : 0) }
                    : null),
            new Candidate("Field Notes doubled, Broader Hypotheses from rank 35", (style, rank) =>
                (rank < 35 ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank)) is var tuning
                    ? tuning with { TestChanceBonusPercent = tuning.TestChanceBonusPercent + FieldNotesChance(rank) }
                    : null)
        };

        private static readonly int?[] GridRanks = { 15, 25, 35, 45, null };

        private static readonly double[] GridMultiples = { 1, 1.5, 2, 3 };

        private static string Grid(ResearcherPerkStudyOptions options)
        {
            var table = new MarkdownTable(new[] { "Broader Hypotheses from" }.Concat(GridMultiples.Select(multiple => $"Field Notes x{Number(multiple, 1)}")).ToArray());
            foreach (var from in GridRanks)
            {
                var cells = new List<string> { from == null ? "never" : $"rank {from}" };
                foreach (var multiple in GridMultiples)
                {
                    ExperimentTuning Tuning(PerkType style, int rank)
                    {
                        var tuning = from == null || rank < from ? Line(style, rank, PerkType.BroaderHypotheses) : Line(style, rank);
                        return tuning with { TestChanceBonusPercent = tuning.TestChanceBonusPercent + (int)Math.Round(FieldNotesChance(rank) * (multiple - 1)) };
                    }

                    var rows = ResearcherPerkStudy.CompareStyles(options, options.Gear, rank => Tuning(PerkType.Methodical, rank), rank => Tuning(PerkType.Intuitive, rank));
                    cells.Add($"{rows.Count(row => !row.Passed)} ({ResearcherPerkStudy.SignedNumber(rows.Where(row => row.Level == ResearcherPerkStudy.FixedLevel).Average(row => row.Gap))} / {ResearcherPerkStudy.SignedNumber(rows.Where(row => row.Level == ResearcherPerkStudy.ContestedLevel).Average(row => row.Gap))})");
                }

                table.Add(cells.ToArray());
            }

            return table.ToString();
        }

        private static Row EvaluateCandidate(ResearcherPerkStudyOptions options, Candidate candidate)
        {
            var rows = ResearcherPerkStudy.CompareStyles(
                options, options.Gear, rank => candidate.Tuning(PerkType.Methodical, rank), rank => candidate.Tuning(PerkType.Intuitive, rank));
            return new Row(
                candidate.Name,
                rows.Count(row => !row.Passed),
                rows.Where(row => row.Level == ResearcherPerkStudy.FixedLevel).Average(row => row.Gap),
                rows.Where(row => row.Level == ResearcherPerkStudy.ContestedLevel).Average(row => row.Gap),
                rows.Average(row => row.FirstActions),
                rows.Average(row => row.SecondActions));
        }

        private static string Table(IEnumerable<Row> rows)
        {
            var table = new MarkdownTable(
                "Build", "Failed of 6", "Fixed levels: mean gap", "Evidence decides: mean gap", "Methodical mean actions", "Intuitive mean actions");
            foreach (var row in rows)
            {
                table.Add(
                    row.Name, row.Failed.ToString(), ResearcherPerkStudy.SignedNumber(row.FixedGap), ResearcherPerkStudy.SignedNumber(row.ContestedGap),
                    Number(row.MethodicalActions), Number(row.IntuitiveActions));
            }

            return table.ToString();
        }
    }
}
