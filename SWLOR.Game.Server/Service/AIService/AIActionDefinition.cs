using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.AIService
{
    public sealed class AIActionDefinition
    {
        public AIActionType Type { get; set; }
        public FeatType Feat { get; set; }
        public AITargetSelector TargetSelector { get; set; }
        public List<AIGuard> Guards { get; } = new();
        public AIScoreCalculation Score { get; set; }
        public int Priority { get; set; }
        public string CooldownId { get; set; }
        public float CooldownSeconds { get; set; }
        public bool OncePerPhase { get; set; }
        public string ScriptName { get; set; }
        public string Text { get; set; }
        public float FloatValue { get; set; }
        public string DebugName { get; set; }

        public AIActionDefinition()
        {
            Type = AIActionType.Invalid;
            Feat = FeatType.Invalid;
            Score = AIScore.Fixed(0);
            Priority = 100;
        }
    }
}
