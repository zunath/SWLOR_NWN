using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RestoredFPForceAttackStatusEffect : StatusEffectBase
    {
        private readonly int _forceAttack;

        public override string Name => "FP Recovery: Force Attack";
        public override EffectIconType Icon => EffectIconType.RestoredFPForceAttackStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public RestoredFPForceAttackStatusEffect()
            : this(8)
        {
        }

        public RestoredFPForceAttackStatusEffect(int forceAttack)
        {
            _forceAttack = forceAttack;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = forceAttack;
        }

        public override IStatusEffect Clone()
        {
            return new RestoredFPForceAttackStatusEffect(_forceAttack);
        }
    }
}
