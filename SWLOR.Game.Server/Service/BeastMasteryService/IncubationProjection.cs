using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    /// <summary>
    /// Read-only incubation math shared by the incubator window, the mutation roll and tests:
    /// which mutations a job qualifies for, the odds of each outcome, and how far a job is
    /// from a known recipe.
    /// </summary>
    public class IncubationProjection
    {
        public const int NumberOfStages = 3;


        public static readonly IReadOnlyList<IncubationStatType> MainPurityStats = new[]
        {
            IncubationStatType.AttackPurity,
            IncubationStatType.AccuracyPurity,
            IncubationStatType.EvasionPurity,
            IncubationStatType.LearningPurity,
            IncubationStatType.PhysicalDefensePurity,
            IncubationStatType.ForceDefensePurity,
            IncubationStatType.XPPenalty,
        };

        public static readonly IReadOnlyList<IncubationStatType> ResistancePurityStats = new[]
        {
            IncubationStatType.FireResistancePurity,
            IncubationStatType.PoisonResistancePurity,
            IncubationStatType.ElectricalResistancePurity,
            IncubationStatType.IceResistancePurity,
            IncubationStatType.MindResistancePurity,
            IncubationStatType.MobilityResistancePurity,
            IncubationStatType.TraumaResistancePurity,
            IncubationStatType.DisruptionResistancePurity,
        };

        private static readonly Dictionary<IncubationStatType, (string Name, string Short, string Tooltip)> _statText = new()
        {
            { IncubationStatType.MutationChance, ("Mutation", "Mut", "Chance the egg becomes a different beast than its DNA.") },
            { IncubationStatType.AttackPurity, ("Attack", "Atk", "Improves the beast's attack rating.") },
            { IncubationStatType.AccuracyPurity, ("Accuracy", "Acc", "Improves the beast's accuracy.") },
            { IncubationStatType.EvasionPurity, ("Evasion", "Eva", "Improves the beast's evasion.") },
            { IncubationStatType.LearningPurity, ("Learning", "Lrn", "Increases the XP the beast earns from defeating enemies.") },
            { IncubationStatType.PhysicalDefensePurity, ("Phys. defense", "PDef", "Improves the beast's physical defense.") },
            { IncubationStatType.ForceDefensePurity, ("Force defense", "FDef", "Improves the beast's force defense.") },
            { IncubationStatType.FireResistancePurity, ("Fire res.", "Fire", "Improves the beast's fire resistance.") },
            { IncubationStatType.PoisonResistancePurity, ("Poison res.", "Pois", "Improves the beast's poison resistance.") },
            { IncubationStatType.ElectricalResistancePurity, ("Elec. res.", "Elec", "Improves the beast's electrical resistance.") },
            { IncubationStatType.IceResistancePurity, ("Ice res.", "Ice", "Improves the beast's ice resistance.") },
            { IncubationStatType.MindResistancePurity, ("Mind res.", "Mind", "Improves the beast's mind resistance.") },
            { IncubationStatType.MobilityResistancePurity, ("Mobility res.", "Mob", "Improves the beast's mobility resistance.") },
            { IncubationStatType.TraumaResistancePurity, ("Trauma res.", "Trau", "Improves the beast's trauma resistance.") },
            { IncubationStatType.DisruptionResistancePurity, ("Disruption res.", "Disr", "Improves the beast's disruption resistance.") },
            { IncubationStatType.XPPenalty, ("XP penalty", "XP pen", "Increases the XP the beast needs to level up.") },
        };

        private static readonly Dictionary<IncubationStatType, ResistanceType> _resistanceByStat = new()
        {
            { IncubationStatType.FireResistancePurity, ResistanceType.Fire },
            { IncubationStatType.PoisonResistancePurity, ResistanceType.Poison },
            { IncubationStatType.ElectricalResistancePurity, ResistanceType.Electrical },
            { IncubationStatType.IceResistancePurity, ResistanceType.Ice },
            { IncubationStatType.MindResistancePurity, ResistanceType.Mind },
            { IncubationStatType.MobilityResistancePurity, ResistanceType.Mobility },
            { IncubationStatType.TraumaResistancePurity, ResistanceType.Trauma },
            { IncubationStatType.DisruptionResistancePurity, ResistanceType.Disruption },
        };

        private readonly IncubationJob _baseJob;
        private readonly IncubationJob _projectedJob;
        private readonly int _openStages;

        private IncubationProjection(IncubationJob baseJob, IncubationJob projectedJob, int openStages)
        {
            _baseJob = baseJob;
            _projectedJob = projectedJob;
            _openStages = openStages;
        }

        public static bool IsKnownStat(IncubationStatType stat) => _statText.ContainsKey(stat);
        public static string GetStatName(IncubationStatType stat) => _statText.TryGetValue(stat, out var text) ? text.Name : stat.ToString();
        public static string GetShortStatName(IncubationStatType stat) => _statText.TryGetValue(stat, out var text) ? text.Short : stat.ToString();
        public static string GetStatTooltip(IncubationStatType stat) => _statText.TryGetValue(stat, out var text) ? text.Tooltip : string.Empty;
        public static ResistanceType GetResistanceType(IncubationStatType stat) => _resistanceByStat[stat];

        /// <summary>
        /// Builds a projection of a job (null for a job not started yet) plus the enzymes waiting in the
        /// incubator's slots for the next stage.
        /// </summary>
        public static IncubationProjection Create(
            IncubationJob job,
            EnzymeColorType pendingLyase,
            EnzymeColorType pendingIsomerase,
            EnzymeColorType pendingHydrolase,
            int projectedMutationChance,
            bool hasPendingStage)
        {
            var baseJob = CopyColors(job);
            var projected = CopyColors(job);
            projected.MutationChance = projectedMutationChance;

            if (hasPendingStage)
            {
                AddColor(projected.LyaseColors, pendingLyase);
                AddColor(projected.IsomeraseColors, pendingIsomerase);
                AddColor(projected.HydrolaseColors, pendingHydrolase);
            }

            var openStages = Math.Max(0, NumberOfStages - (job?.CurrentStage ?? 0));
            return new IncubationProjection(baseJob, projected, openStages);
        }

        private static IncubationJob CopyColors(IncubationJob job)
        {
            var copy = new IncubationJob();
            if (job == null)
                return copy;

            copy.CurrentStage = job.CurrentStage;
            copy.MutationChance = job.MutationChance;
            foreach (var (color, count) in job.LyaseColors) copy.LyaseColors[color] = count;
            foreach (var (color, count) in job.IsomeraseColors) copy.IsomeraseColors[color] = count;
            foreach (var (color, count) in job.HydrolaseColors) copy.HydrolaseColors[color] = count;
            return copy;
        }

        private static void AddColor(Dictionary<EnzymeColorType, int> counts, EnzymeColorType color)
        {
            if (color == EnzymeColorType.Invalid)
                return;

            counts.TryGetValue(color, out var current);
            counts[color] = current + 1;
        }

        /// <summary>
        /// Checks a known recipe against this projection, one line per requirement, plus a short summary
        /// of what is still missing.
        /// </summary>
        public MutationRecipeReport Evaluate(MutationDetail mutation, DateTime now)
        {
            var lines = new List<MutationRequirementLine>();
            var missingEnzymes = 0;
            var otherUnmet = false;
            var reachable = true;
            var dayMet = true;

            foreach (var requirement in mutation.Requirements)
            {
                switch (requirement)
                {
                    case MutationRequirementEnzyme enzyme:
                        reachable &= AddEnzymeLines(lines, ref missingEnzymes, "lyase", enzyme.LyaseEnzymeColors, _baseJob.LyaseColors, _projectedJob.LyaseColors);
                        reachable &= AddEnzymeLines(lines, ref missingEnzymes, "isomerase", enzyme.IsomeraseEnzymeColors, _baseJob.IsomeraseColors, _projectedJob.IsomeraseColors);
                        reachable &= AddEnzymeLines(lines, ref missingEnzymes, "hydrolase", enzyme.HydrolaseEnzymeColors, _baseJob.HydrolaseColors, _projectedJob.HydrolaseColors);
                        break;
                    case MutationRequirementDayOfWeek day:
                        dayMet = day.IsMetOn(now.DayOfWeek);
                        lines.Add(new MutationRequirementLine($"Complete {day.DescribeDays(true)} (UTC)", dayMet, true));
                        break;
                    default:
                        var met = string.IsNullOrWhiteSpace(requirement.CheckRequirements(_projectedJob));
                        lines.Add(new MutationRequirementLine(requirement.GetRequirementDescription(), met, false));
                        otherUnmet |= !met;
                        break;
                }
            }

            string summary;
            var ready = missingEnzymes == 0 && !otherUnmet && dayMet;
            if (!reachable)
                summary = "Out of reach for this job";
            else if (missingEnzymes > 0)
                summary = missingEnzymes == 1 ? "1 more enzyme needed" : $"{missingEnzymes} more enzymes needed";
            else if (otherUnmet)
                summary = "Not ready yet";
            else
                summary = dayMet ? "Requirements met" : "Enzymes met - wait for the day";

            return new MutationRecipeReport(lines, summary, ready);
        }

        private bool AddEnzymeLines(
            List<MutationRequirementLine> lines,
            ref int missingEnzymes,
            string typeName,
            Dictionary<EnzymeColorType, int> required,
            Dictionary<EnzymeColorType, int> baseCounts,
            Dictionary<EnzymeColorType, int> projectedCounts)
        {
            var neededFromOpenStages = 0;
            foreach (var (color, count) in required)
            {
                projectedCounts.TryGetValue(color, out var have);
                baseCounts.TryGetValue(color, out var already);
                var colorName = color.ToString().ToLower();

                lines.Add(new MutationRequirementLine($"{colorName} {typeName} {Math.Min(have, count)}/{count}", have >= count, false));
                missingEnzymes += Math.Max(0, count - have);

                neededFromOpenStages += Math.Max(0, count - already);
            }

            // Each stage takes one enzyme of each type, so the open stages cap how many more of a type can go in.
            return neededFromOpenStages <= _openStages;
        }

        /// <summary>
        /// The mutations a finished job qualifies for, judged at the current server time.
        /// </summary>
        public static List<MutationDetail> GetEligibleMutations(BeastDetail beast, IncubationJob job)
        {
            return beast.PossibleMutations
                .Where(mutation => mutation.Requirements.All(requirement =>
                    string.IsNullOrWhiteSpace(requirement.CheckRequirements(job))))
                .ToList();
        }

        /// <summary>
        /// The chance of each possible result if the job is completed now: each eligible mutation's share of the
        /// mutation chance by weight, and the DNA's own beast for the rest.
        /// </summary>
        public static List<IncubationOutcome> GetOutcomes(BeastDetail beast, IncubationJob job, bool mutationAllowed)
        {
            var outcomes = new List<IncubationOutcome>();
            var eligible = mutationAllowed ? GetEligibleMutations(beast, job) : new List<MutationDetail>();
            var totalWeight = eligible.Sum(x => Math.Max(0, x.Weight));
            var chance = Math.Clamp(job.MutationChance, 0, 1000) / 10f;

            if (totalWeight <= 0)
                chance = 0f;

            foreach (var mutation in eligible.Where(x => x.Weight > 0))
            {
                outcomes.Add(new IncubationOutcome(mutation.Type, chance * mutation.Weight / totalWeight));
            }

            outcomes.Add(new IncubationOutcome(job.BeastDNAType, 100f - chance));
            return outcomes;
        }
    }
}
