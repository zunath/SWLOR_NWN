using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public sealed class AbilityImpactSummary
    {
        public SkillType SkillType { get; set; }
        public bool IsAreaAbility { get; set; }
        public bool IsSingleTargetAbility { get; set; }
        public int ImpactedTargetCount { get; set; }
        public int CriticalHitCount { get; set; }

        /// <summary>
        /// Total damage this impact queued against its targets. Distinguishes "the impact
        /// visited a target" (ImpactedTargetCount, which records even zero-damage visits)
        /// from "the impact actually dealt damage" - the distinction needed to attribute a
        /// queued weapon ability's damage separately from the consuming weapon swing.
        /// </summary>
        public int AttributedDamage { get; set; }

        public AbilityImpactSummary()
        {
            SkillType = SkillType.Invalid;
        }
    }
}
