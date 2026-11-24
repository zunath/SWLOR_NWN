using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The decision side of the profile engine: which creature triggers get a decision, the throttle, the active phase, the
    /// action cache and the boss timer. Static callers reach the container's instance through <c>GameServices.AIDecision</c>.
    /// </summary>
    public interface IAIDecisionService
    {
        bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID,
            bool bypassDecisionThrottle = false);
    }
}
