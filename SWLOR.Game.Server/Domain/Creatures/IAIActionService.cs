namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The action side of the profile engine: scoring the candidate actions, resolving their targets, checking that they can
    /// run and running the winner. Static callers reach the container's instance through <c>GameServices.AIAction</c>.
    /// </summary>
    public interface IAIActionService
    {
        /// <summary>
        /// Scores the candidate actions of the creature's profile and phase and returns the best one, or null when none can run.
        /// </summary>
        AIActionEvaluation SelectAction(AIContext context);

        /// <summary>
        /// Runs the selected action and records its cooldown and once-per-phase mark.
        /// </summary>
        void ExecuteAction(AIContext context, AIActionEvaluation evaluation);

        bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName);
    }
}
