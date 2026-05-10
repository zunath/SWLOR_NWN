using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIActionDefinition
    {
        public FeatType Feat { get; init; } = FeatType.Invalid;
        public AITargetType TargetType { get; init; } = AITargetType.Invalid;
        public AIPhaseType Phase { get; init; } = AIPhaseType.Damage;
        public float BaseWeight { get; init; } = 1.0f;

        /// <summary>
        /// A deterministic tie-breaker. Lower values are preferred.
        /// </summary>
        public int Priority { get; init; }
    }
}
