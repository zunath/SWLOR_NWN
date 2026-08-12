using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceLensForceAttackStatusEffect : StatusEffectBase
    {
        public override string Name => "Force Lens: Force Attack";
        public override EffectIconType Icon => EffectIconType.ForceLensForceAttackStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public ForceLensForceAttackStatusEffect()
        {
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = 8;
        }
    }
}
