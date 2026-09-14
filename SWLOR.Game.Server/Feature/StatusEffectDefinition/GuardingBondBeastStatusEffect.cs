using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class GuardingBondBeastStatusEffect : StatusEffectBase
    {
        public override string Name => "Guarding Bond";
        public override EffectIconType Icon => EffectIconType.GuardingBondBeastStatusEffect;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public GuardingBondBeastStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 20;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = 75;
        }
    }
}
