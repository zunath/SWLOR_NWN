using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// The focused researcher (<see cref="ExperimentFocusedPolicy"/>) who also uses the actions Researcher perks add. Breakthrough is the
    /// first action, so the guaranteed Test is the next one and the doubled Analyze is ready for the end. Where evidence decides between
    /// two reductions, the focused player puts every Test on one candidate and never has anything for Peer Review to move; with
    /// <c>spread</c> the player is undecided instead, testing both candidates for the first half of the trials, then backing the leader
    /// and, with Peer Review, pooling the evidence behind it into the leader. An undecided player with Peer Review keeps the CP it costs
    /// back from Refine Method.
    /// </summary>
    public sealed class ExperimentPerkedPolicy : ICraftPolicy
    {
        public ExperimentPerkedPolicy(ResearchCandidate target, bool spread = false)
        {
            Target = target;
            Spread = spread;
        }

        public ResearchCandidate Target { get; }

        public bool Spread { get; }

        public string Name => Spread ? "Undecided, then backs the leader" : $"Focused on {Target}, with perk actions";

        public string Choose(ICraftMinigameEngine engine)
        {
            var experiment = (ExperimentEngine)engine;

            if (Enabled(experiment, ExperimentActionIds.Breakthrough))
                return ExperimentActionIds.Breakthrough;

            var reserve = Spread && experiment.Setup.PeerReviewUnlocked && experiment.Setup.IsContested && !experiment.PeerReviewUsed
                ? ExperimentRules.PeerReviewCp
                : 0;
            if (Enabled(experiment, ExperimentActionIds.RefineMethod) && experiment.Meters.Cp.Value - experiment.Setup.RefineCp >= reserve)
                return ExperimentActionIds.RefineMethod;

            if (!experiment.Setup.IsContested)
            {
                return experiment.TrialsRemaining == 1 && Enabled(experiment, ExperimentActionIds.Analyze)
                    ? ExperimentActionIds.Analyze
                    : ExperimentActionIds.TestFixed;
            }

            if (!Spread)
                return ExperimentActionIds.TestFor(Target);

            if (experiment.TrialsUsed < experiment.Setup.Trials / 2)
            {
                return experiment.TrialsUsed % 2 == 0
                    ? ExperimentActionIds.TestCreditReduction
                    : ExperimentActionIds.TestTimeReduction;
            }

            var credit = experiment.EvidenceOf(ResearchCandidate.CreditReduction);
            var time = experiment.EvidenceOf(ResearchCandidate.TimeReduction);
            var leader = credit >= time ? ResearchCandidate.CreditReduction : ResearchCandidate.TimeReduction;
            var trailing = leader == ResearchCandidate.CreditReduction ? time : credit;
            var pool = leader == ResearchCandidate.CreditReduction
                ? ExperimentActionIds.PeerReviewTimeToCredit
                : ExperimentActionIds.PeerReviewCreditToTime;

            return trailing > 0 && Enabled(experiment, pool) ? pool : ExperimentActionIds.TestFor(leader);
        }

        private static bool Enabled(ExperimentEngine engine, string actionId) =>
            CraftPlayer.Offers(engine, actionId) && engine.Preview(actionId).Enabled;
    }
}
