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
        /// It reads the action cache on the context's state, which <c>AIDecisionService</c> fills before it calls this, and
        /// considers only the first <c>MaxCandidateActions</c> of the profile. A caller that needs a complete decision run uses
        /// <c>IAIDecisionService.ProcessTrigger</c> instead.
        /// </summary>
        AIActionEvaluation SelectAction(AIContext context);

        /// <summary>
        /// Runs the selected action and records its cooldown and once-per-phase mark.
        /// </summary>
        void ExecuteAction(AIContext context, AIActionEvaluation evaluation);

        bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName);
    }
}
