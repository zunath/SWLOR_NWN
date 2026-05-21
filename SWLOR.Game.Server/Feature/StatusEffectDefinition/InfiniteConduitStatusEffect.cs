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
        public override EffectIconType Icon => EffectIconType.InfiniteConduitStatusEffect;

        public InfiniteConduitStatusEffect()
        {
            StatGroup.Stats[StatType.AutoAttackFPRestore] = 2;
            StatGroup.Stats[StatType.SkillAbilityStaminaCostFlatAdjustment] = -2;
        }
    }
}
