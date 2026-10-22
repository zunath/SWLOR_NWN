using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>The results of many crafts of the old minigame at one recipe level, rank, gear level, policy and enhancement load.</summary>
    public sealed class LegacyEnhancementCell
    {
        public int Level { get; init; }
        public int Rank { get; init; }
        public string Gear { get; init; }
        public string Policy { get; init; }
        public CraftEnhancementConfiguration Enhancements { get; init; }
        public int Runs { get; init; }
        public int Successes { get; init; }
        public long Actions { get; init; }

        /// <summary>The summed quality fraction of the crafts that were made.</summary>
        public double QualitySum { get; init; }

        /// <summary>The summed transfer chance, in percent, of the crafts that were made.</summary>
        public long TransferChanceSum { get; init; }

        /// <summary>The summed expected number of transferred enhancements over every craft; a failed craft transfers none.</summary>
        public double TransfersSum { get; init; }

        /// <summary>The quality fraction of each craft (0 for a failure), in run order.</summary>
        public IReadOnlyList<double> Qualities { get; init; }

        public IReadOnlyList<bool> Succeeded { get; init; }

        /// <summary>The mean XP of one attempt for a recipe whose base XP is <paramref name="baseXp"/> (no blueprint, not a first craft); enhancements do not change the XP formula.</summary>
        public double XpPerAttempt(int baseXp)
        {
            var total = 0.0;
            for (var run = 0; run < Runs; run++)
            {
                total += Succeeded[run]
                    ? CraftOutcomeRules.CalculateXp(baseXp, 0, false, (float)Qualities[run])
                    : CraftOutcomeRules.CalculateFailureXp(baseXp, 0);
            }

            return Runs == 0 ? 0 : total / Runs;
        }

        public double SuccessRate => Runs == 0 ? 0 : Successes / (double)Runs;

        public double ActionsPerAttempt => Runs == 0 ? 0 : Actions / (double)Runs;

        /// <summary>The mean quality fraction of the crafts that were made.</summary>
        public double MeanQualityOfSuccesses => Successes == 0 ? 0 : QualitySum / Successes;

        /// <summary>The mean chance, in percent, that an enhancement transfers on a craft that was made.</summary>
        public double MeanTransferChancePercent => Successes == 0 ? 0 : TransferChanceSum / (double)Successes;

        /// <summary>Today's expected transferred enhancements per enhanced craft attempt, failures counting none: the number gate 7 compares.</summary>
        public double ExpectedTransfersPerAttempt => Runs == 0 ? 0 : TransfersSum / Runs;

        /// <summary>The expected transferred enhancements per craft that was made.</summary>
        public double ExpectedTransfersPerSuccess => Successes == 0 ? 0 : TransfersSum / Successes;
    }

    /// <summary>
    /// Plays the old minigame with enhancements slotted: the progress the piece needs rises by the enhancements' penalties, and a
    /// craft that is made transfers each enhancement at the quality share. The transfer rolls are not drawn; a craft's expected
    /// transfers are slots x chance / 100 exactly, which carries the same mean with less noise.
    /// </summary>
    public static class LegacyEnhancementBaseline
    {
        /// <summary>The policies the legacy comparators play, in the order the baseline numbers them: the rotation players report, then the lookahead.</summary>
        public static readonly IReadOnlyList<ILegacyCraftPolicy> Policies = new ILegacyCraftPolicy[] { new DominantRotationPolicy(), new RolloutPolicy() };

        /// <summary>The index in <see cref="Policies"/> of the lookahead player, which the gates compare against.</summary>
        public const int LookaheadIndex = 1;

        public static LegacyEnhancementCell PlayCell(
            int level, int rank, GearLevel gear, int gearIndex, int policyIndex, CraftEnhancementConfiguration enhancements, int runs, int seed)
        {
            var policy = Policies[policyIndex];
            var context = new LegacyCraftContext(level, rank, gear.Control, gear.Craftsmanship, gear.CpBonus, enhancements);
            var successes = 0;
            long actions = 0;
            long chanceSum = 0;
            var qualitySum = 0.0;
            var transfers = 0.0;
            var qualities = new double[runs];
            var succeeded = new bool[runs];

            for (var run = 0; run < runs; run++)
            {
                var rng = SimRandom.ForRun(
                    seed, SimStreams.LegacyEnhanced, (int)gear.Skill, level, rank, gearIndex, policyIndex,
                    context.Enhancements.Count, context.Enhancements.ProgressPenaltyEach, run);
                var result = LegacyCraftRunner.Play(context, policy, rng);
                actions += result.Actions;
                if (!result.Succeeded)
                    continue;

                successes++;
                succeeded[run] = true;
                qualities[run] = result.QualityFraction;
                qualitySum += result.QualityFraction;
                chanceSum += result.TransferChancePercent;
                transfers += LegacyEnhancementRules.ExpectedTransfers(context.Enhancements.Count, result.TransferChancePercent);
            }

            return new LegacyEnhancementCell
            {
                Level = level,
                Rank = rank,
                Gear = gear.Name,
                Policy = policy.Name,
                Enhancements = context.Enhancements,
                Runs = runs,
                Successes = successes,
                Actions = actions,
                QualitySum = qualitySum,
                TransferChanceSum = chanceSum,
                TransfersSum = transfers,
                Qualities = qualities,
                Succeeded = succeeded
            };
        }
    }
}
