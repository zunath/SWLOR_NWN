using System;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>The AI settings of one active ability, kept by <see cref="IAIService"/>.</summary>
    public sealed class AbilityAISettingValues
    {
        public AITargetSelector TargetSelector { get; set; }
        public AIScoreCalculation Score { get; set; }

        /// <summary>Maximum-HP percentage spent on activation, used to preserve an AI health reserve.</summary>
        public Func<uint, int> HitPointCostPercent { get; set; }
    }
}
