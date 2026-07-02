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

        public AbilityImpactSummary()
        {
            SkillType = SkillType.Invalid;
        }
    }
}
