using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceLensForceAttackStatusEffect : StatusEffectBase
    {
        private readonly int _forceAttack;

        public override string Name => "Force Lens: Force Attack";
        public override EffectIconType Icon => EffectIconType.ForceLensForceAttackStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public ForceLensForceAttackStatusEffect()
            : this(8)
        {
        }

        public ForceLensForceAttackStatusEffect(int forceAttack)
        {
            _forceAttack = forceAttack;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = forceAttack;
        }

        public override IStatusEffect Clone()
        {
            return new ForceLensForceAttackStatusEffect(_forceAttack);
        }
    }
}
