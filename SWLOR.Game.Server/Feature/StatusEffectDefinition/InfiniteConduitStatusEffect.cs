using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class InfiniteConduitStatusEffect : StatusEffectBase
    {
        public override string Name => "Infinite Conduit";
        public override EffectIconType Icon => EffectIconType.Haste;

        public InfiniteConduitStatusEffect()
        {
            StatGroup.Stats[StatType.SkillAutoAttackFPRestoreSkillType] = (int)SkillType.Saberstaff;
            StatGroup.Stats[StatType.SkillAutoAttackFPRestore] = 5;
            StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustmentSkillType] = (int)SkillType.Saberstaff;
            StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustment] = -3;
        }

        protected override void Tick(uint creature)
        {
            if (Stat.GetCurrentFP(creature) <= 0)
            {
                IsFlaggedForRemoval = true;
            }
        }
    }
}
