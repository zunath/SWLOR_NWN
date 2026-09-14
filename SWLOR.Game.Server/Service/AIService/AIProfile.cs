using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIProfile
    {
        public AIProfileType Type { get; set; }
        public string Name { get; set; }
        public float DecisionThrottleSeconds { get; set; }
        public int MaxCandidateActions { get; set; }
        public bool IsBoss { get; set; }
        public List<AIActionDefinition> Actions { get; } = new();
        public Dictionary<AIPhaseId, AIPhaseDefinition> Phases { get; } = new();
        public List<AIPhaseId> PhaseOrder { get; } = new();

        public AIProfile()
        {
            DecisionThrottleSeconds = 0.25f;
            MaxCandidateActions = 16;
        }
    }
}
