using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow
{
    /// <summary>
    /// The incubator's read-only panels: the header, the stage tracker, the enzyme totals, the purities, the mutation chance, the
    /// known recipes, the outcomes and the stage time. Each writes the bindings the host exposes.
    /// </summary>
    public class IncubatorPanelService
    {
        private IBeastMasteryService BeastMastery { get; }
        private IIncubationFieldNoteService IncubationFieldNote { get; }
        private IncubatorJobCalculator IncubatorJobCalculator { get; }
        private IKeyItemService KeyItem { get; }

        public IncubatorPanelService(IBeastMasteryService beastMastery, IIncubationFieldNoteService incubationFieldNote, IncubatorJobCalculator incubatorJobCalculator, IKeyItemService keyItem)
        {
            BeastMastery = beastMastery;
            IncubationFieldNote = incubationFieldNote;
            IncubatorJobCalculator = incubatorJobCalculator;
            KeyItem = keyItem;
        }

        private static readonly GuiColor DoneColor = new(110, 200, 110);
        private static readonly GuiColor NextColor = new(120, 180, 250);
        private static readonly GuiColor RunningColor = new(230, 180, 70);
        private static readonly GuiColor PendingColor = GuiColor.Grey;
        private static readonly GuiColor MetColor = new(110, 200, 110);
        private static readonly GuiColor UnmetColor = new(225, 100, 100);
        private static readonly GuiColor HintColor = new(230, 180, 70);
        private static readonly GuiColor PenaltyColor = new(225, 100, 100);

        public void RefreshHeader(IIncubatorHost host, IncubationJob job)
        {
            host.ServerDayText = $"Server day: {DescribeServerDay(GetObjectUUID(host.Player))}";
            host.IsDNASlotEnabled = job == null;
            host.DNASlotTooltip = job == null
                ? (string.IsNullOrWhiteSpace(host.Session.DnaItem) ? "Add DNA" : "Remove DNA")
                : "The DNA is locked in this job.";
            if (job != null)
                host.DNAItemResref = string.IsNullOrWhiteSpace(job.DNAIconResref) ? IncubatorSession.Blank : job.DNAIconResref;

            if (host.Session.DnaType == BeastType.Invalid)
            {
                host.BeastName = "No DNA added";
                host.BeastSubtitle = "Click the slot to add a DNA sample.";
                return;
            }

            var beast = BeastMastery.GetBeastDetail(host.Session.DnaType);
            host.BeastName = $"{beast.Name} DNA";
            host.BeastSubtitle = $"{beast.Role} role";
        }

        /// <summary>"Wednesday (UTC)", or "Sunday (test)" while a tester's /incubationday override is set.</summary>
        private string DescribeServerDay(string playerId)
        {
            var suffix = BeastMastery.HasIncubationDayOverride(playerId) ? "test" : "UTC";
            return $"{BeastMastery.GetIncubationServerDay(playerId)} ({suffix})";
        }

        public void RefreshStageTracker(IIncubatorHost host, IncubationJob job, DateTime now)
        {
            var titles = new string[IncubatorJobCalculator.NumberOfStages];
            var details = new string[IncubatorJobCalculator.NumberOfStages];
            var colors = new GuiColor[IncubatorJobCalculator.NumberOfStages];
            var stageTime = Time.GetTimeShortIntervals(TimeSpan.FromSeconds(IncubatorJobCalculator.CalculateIncubationSeconds(host)), false);

            for (var index = 0; index < IncubatorJobCalculator.NumberOfStages; index++)
            {
                var stage = index + 1;
                var started = job?.CurrentStage ?? 0;
                var finished = job != null && now >= job.DateCompleted;

                if (stage < started || (stage == started && finished))
                {
                    titles[index] = $"Stage {stage} - done";
                    details[index] = "Enzymes added";
                    colors[index] = DoneColor;
                }
                else if (stage == started)
                {
                    titles[index] = $"Stage {stage} - running";
                    details[index] = $"{Time.GetTimeShortIntervals(job.DateCompleted - now, false)} left";
                    colors[index] = RunningColor;
                }
                else if (stage == started + 1 && (job == null || finished))
                {
                    titles[index] = $"Stage {stage} - next";
                    details[index] = $"Takes {stageTime}";
                    colors[index] = NextColor;
                }
                else
                {
                    titles[index] = $"Stage {stage}";
                    details[index] = "Waiting";
                    colors[index] = PendingColor;
                }
            }

            host.Stage1Title = titles[0];
            host.Stage2Title = titles[1];
            host.Stage3Title = titles[2];
            host.Stage1Detail = details[0];
            host.Stage2Detail = details[1];
            host.Stage3Detail = details[2];
            host.Stage1Color = colors[0];
            host.Stage2Color = colors[1];
            host.Stage3Color = colors[2];

            var nextStage = (job?.CurrentStage ?? 0) + 1;
            host.EnzymeSectionTitle = nextStage <= IncubatorJobCalculator.NumberOfStages
                ? $"Enzymes for stage {nextStage}"
                : "Enzymes";
        }

        public static void RefreshEnzymeTotals(IIncubatorHost host, IncubationJob job)
        {
            if (job == null)
            {
                host.EnzymeTotalsText = "Enzymes added so far: none";
                return;
            }

            var parts = new List<string>
            {
                DescribeColorCounts("Lyase", job.LyaseColors),
                DescribeColorCounts("Isomerase", job.IsomeraseColors),
                DescribeColorCounts("Hydrolase", job.HydrolaseColors)
            };
            host.EnzymeTotalsText = "So far - " + string.Join(" | ", parts);
        }

        private static string DescribeColorCounts(string type, Dictionary<EnzymeColorType, int> counts)
        {
            var used = counts
                .Where(x => x.Value > 0)
                .Select(x => $"{x.Value} {x.Key.ToString().ToLower()}")
                .ToList();
            return $"{type}: {(used.Count == 0 ? "none" : string.Join(", ", used))}";
        }

        public void RefreshPurities(IIncubatorHost host, bool hasPendingStage)
        {
            var names = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            var tooltips = new GuiBindingList<string>();
            var values = new GuiBindingList<float>();
            var texts = new GuiBindingList<string>();

            void AddRow(string name, string tooltip, float percent, float gain, GuiColor color)
            {
                names.Add(name);
                tooltips.Add(tooltip);
                colors.Add(color);
                values.Add(Math.Clamp(percent / 100f, 0f, 1f));
                texts.Add(gain > 0f ? $"{percent:0.0}% (+{gain:0.0})" : $"{percent:0.0}%");
            }

            foreach (var stat in IncubationProjection.MainPurityStats)
            {
                var (percent, gain) = GetProjectedPercent(host, stat, hasPendingStage);
                var color = stat == IncubationStatType.XPPenalty ? PenaltyColor : GuiColor.White;
                AddRow(IncubationProjection.GetStatName(stat), IncubationProjection.GetStatTooltip(stat), percent, gain, color);
            }

            if (host.Session.ShowResistances)
            {
                foreach (var stat in IncubationProjection.ResistancePurityStats)
                {
                    var (percent, gain) = GetProjectedPercent(host, stat, hasPendingStage);
                    AddRow(IncubationProjection.GetStatName(stat), IncubationProjection.GetStatTooltip(stat), percent, gain, GuiColor.White);
                }
            }
            else
            {
                var projected = IncubationProjection.ResistancePurityStats
                    .Select(stat => GetProjectedPercent(host, stat, hasPendingStage))
                    .ToList();
                AddRow($"Resistances ({projected.Count})",
                    "Average of the eight resistance purities. Use Show resistances to see each one.",
                    projected.Average(x => x.Percent),
                    projected.Average(x => x.Gain),
                    GuiColor.Grey);
            }

            host.PurityHeader = hasPendingStage
                ? "Purities (+ this stage)"
                : "Purities";
            host.ResistanceToggleText = host.Session.ShowResistances ? "Hide resistances" : "Show resistances";

            host.PurityNames = names;
            host.PurityNameColors = colors;
            host.PurityTooltips = tooltips;
            host.PurityValues = values;
            host.PurityTexts = texts;
        }

        private (float Percent, float Gain) GetProjectedPercent(IIncubatorHost host, IncubationStatType stat, bool hasPendingStage)
        {
            host.Session.BaseStats.TryGetValue(stat, out var baseValue);
            host.Session.StageStats.TryGetValue(stat, out var stageValue);
            var gain = hasPendingStage ? Math.Clamp(stageValue, 0, IncubatorJobCalculator.MaxStageIncrease) : 0;
            var total = Math.Min(baseValue + gain, IncubatorJobCalculator.MaxStat);
            return (total / 10f, (total - Math.Min(baseValue, IncubatorJobCalculator.MaxStat)) / 10f);
        }

        public void RefreshMutation(IIncubatorHost host, IncubationJob job, bool hasPendingStage)
        {
            var chance = IncubatorJobCalculator.GetProjectedMutationChance(host, job, hasPendingStage);
            host.Session.BaseStats.TryGetValue(IncubationStatType.MutationChance, out var baseValue);
            var gain = (chance - baseValue) / 10f;

            if (!host.IsMutationAllowed)
            {
                host.MutationChanceText = "Mutation off - beast type is kept";
                host.MutationChanceColor = GuiColor.Grey;
                return;
            }

            host.MutationChanceText = gain > 0f
                ? $"Chance: {chance / 10f:0.0}% (+{gain:0.0} this stage)"
                : $"Chance: {chance / 10f:0.0}%";
            host.MutationChanceColor = GuiColor.White;
        }

        public void RefreshRecipes(IIncubatorHost host, IncubationJob job, bool hasPendingStage)
        {
            var lines = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();

            if (host.Session.DnaType == BeastType.Invalid)
            {
                host.RecipeHeader = "Known recipes";
                host.RecipeLines = lines;
                host.RecipeLineColors = colors;
                host.UnknownRecipesText = "Add DNA to see its mutation recipes.";
                return;
            }

            var beast = BeastMastery.GetBeastDetail(host.Session.DnaType);
            var projection = IncubationProjection.Create(
                job,
                hasPendingStage && !string.IsNullOrWhiteSpace(host.Session.LyaseItem) ? host.Session.LyaseColor : EnzymeColorType.Invalid,
                hasPendingStage && !string.IsNullOrWhiteSpace(host.Session.IsomeraseItem) ? host.Session.IsomeraseColor : EnzymeColorType.Invalid,
                hasPendingStage && !string.IsNullOrWhiteSpace(host.Session.HydrolaseItem) ? host.Session.HydrolaseColor : EnzymeColorType.Invalid,
                IncubatorJobCalculator.GetProjectedMutationChance(host, job, hasPendingStage),
                hasPendingStage);

            var unknown = 0;
            var today = BeastMastery.GetIncubationServerDay(GetObjectUUID(host.Player));
            foreach (var mutation in beast.PossibleMutations)
            {
                if (!IncubationFieldNote.TryGetNoteForTarget(mutation.Type, out var note) ||
                    !KeyItem.HasKeyItem(host.Player, note.Note))
                {
                    unknown++;
                    continue;
                }

                var report = projection.Evaluate(mutation, today);
                lines.Add(BeastMastery.GetBeastDetail(mutation.Type).Name);
                colors.Add(GuiColor.White);

                foreach (var requirement in report.Lines)
                {
                    lines.Add($"  {(requirement.IsMet ? "[ok]" : "[ ]")} {requirement.Text}");
                    colors.Add(requirement.IsMet ? MetColor : requirement.IsWaitable ? HintColor : UnmetColor);
                }

                lines.Add($"  {report.Summary}");
                colors.Add(HintColor);
            }

            host.RecipeHeader = $"Known recipes for {beast.Name}";
            if (lines.Count == 0)
            {
                lines.Add(beast.PossibleMutations.Count == 0
                    ? "This beast cannot mutate."
                    : "You have no field notes for this beast.");
                colors.Add(GuiColor.Grey);
            }

            host.RecipeLines = lines;
            host.RecipeLineColors = colors;
            host.UnknownRecipesText = unknown == 0
                ? string.Empty
                : $"{unknown} unknown recipe{(unknown == 1 ? string.Empty : "s")} - find the field note{(unknown == 1 ? string.Empty : "s")}";
        }

        public void RefreshOutcomes(IIncubatorHost host, IncubationJob job, DateTime now)
        {
            var names = new GuiBindingList<string>();
            var chances = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();

            if (job != null && IncubatorJobCalculator.IsJobComplete(job, now))
            {
                var beast = BeastMastery.GetBeastDetail(job.BeastDNAType);
                var outcomes = IncubationProjection.GetOutcomes(beast, job, host.IsMutationAllowed);
                foreach (var outcome in outcomes)
                {
                    var known = outcome.Type == job.BeastDNAType ||
                                (IncubationFieldNote.TryGetNoteForTarget(outcome.Type, out var note) &&
                                 KeyItem.HasKeyItem(host.Player, note.Note));
                    var name = outcome.Type == job.BeastDNAType
                        ? $"{beast.Name} (no mutation)"
                        : known
                            ? BeastMastery.GetBeastDetail(outcome.Type).Name
                            : "Unknown mutation (no field note)";

                    names.Add(name);
                    chances.Add($"{outcome.Percent:0.0}%");
                    colors.Add(outcome.Type == job.BeastDNAType ? GuiColor.White : MetColor);
                }

                host.OutcomeHeader = $"Outcomes if completed now - {DescribeServerDay(job.PlayerId)}";
            }
            else
            {
                host.OutcomeHeader = "Possible outcomes";
            }

            host.OutcomeNames = names;
            host.OutcomeChances = chances;
            host.OutcomeColors = colors;
        }

        public void RefreshIncubationTime(IIncubatorHost host)
        {
            var timespan = TimeSpan.FromSeconds(IncubatorJobCalculator.CalculateIncubationSeconds(host));
            host.EstimatedTimeToCompletion = $"Each stage takes {Time.GetTimeShortIntervals(timespan, false)}";
        }
    }
}
