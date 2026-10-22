using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.CraftSim.Scenario.Experiment
{
    /// <summary>
    /// A researcher who knows what they want: at a random level every Test goes to one chosen candidate, at a fixed level every Test
    /// banks evidence and the last trial analyzes it. With <see cref="UseRefine"/> they refine their method before every Test they can
    /// afford. It is the player the magnitude, time-cut and XP tables use, because it states a strategy instead of searching for one.
    /// </summary>
    public sealed class ExperimentFocusedPolicy : ICraftPolicy
    {
        public ExperimentFocusedPolicy(ResearchCandidate target, bool useRefine = true)
        {
            Target = target;
            UseRefine = useRefine;
        }

        public ResearchCandidate Target { get; }

        public bool UseRefine { get; }

        public string Name => UseRefine ? $"Focused on {Target}" : $"Focused on {Target}, no Refine Method";

        public string Choose(ICraftMinigameEngine engine)
        {
            var experiment = (ExperimentEngine)engine;

            if (UseRefine && experiment.Preview(ExperimentActionIds.RefineMethod).Enabled)
                return ExperimentActionIds.RefineMethod;

            if (!experiment.Setup.IsRandomLevel)
            {
                if (experiment.TrialsRemaining == 1 &&
                    experiment.Preview(ExperimentActionIds.Analyze).Enabled)
                {
                    return ExperimentActionIds.Analyze;
                }

                return ExperimentActionIds.TestFixed;
            }

            return ExperimentActionIds.TestFor(Target);
        }
    }
}
