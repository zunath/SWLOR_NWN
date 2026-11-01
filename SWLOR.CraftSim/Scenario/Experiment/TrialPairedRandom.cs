using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// A source of chance for an experiment whose rolls belong to the trial they are drawn in: the roll for trial 4 is the same
    /// however many actions came before it. Two researchers with different perks (a Breakthrough Test takes no roll, Refine Method
    /// and Peer Review use no trial) therefore meet the same dice at the same trial, so a difference between them is theirs and not
    /// a shift in a shared sequence. The tie-break after the experiment ends has its own stream.
    /// </summary>
    internal sealed class TrialPairedRandom : IMinigameRandom
    {
        private const int AfterTheLastTrial = int.MaxValue;

        private readonly Func<int> _trial;
        private readonly Func<int, IMinigameRandom> _forTrial;

        /// <param name="trial">The trial the engine is on: the trials it has used so far.</param>
        /// <param name="forTrial">The dice of one trial, the same for every build.</param>
        public TrialPairedRandom(Func<int> trial, Func<int, IMinigameRandom> forTrial)
        {
            _trial = trial ?? throw new ArgumentNullException(nameof(trial));
            _forTrial = forTrial ?? throw new ArgumentNullException(nameof(forTrial));
        }

        public int D100() => _forTrial(_trial()).D100();

        public int Next(int minInclusive, int maxExclusive) => _forTrial(AfterTheLastTrial).Next(minInclusive, maxExclusive);
    }
}
