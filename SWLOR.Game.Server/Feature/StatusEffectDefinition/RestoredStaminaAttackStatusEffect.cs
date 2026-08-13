using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RestoredStaminaAttackStatusEffect : StatusEffectBase
    {
        private readonly int _attack;

        public override string Name => "Force Lens: Attack";
        public override EffectIconType Icon => EffectIconType.RestoredStaminaAttackStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public RestoredStaminaAttackStatusEffect()
            : this(8)
        {
        }

        public RestoredStaminaAttackStatusEffect(int attack)
        {
            _attack = attack;
            StatGroup.Stats[StatType.AttackPercentAdjustment] = attack;
        }

        public override IStatusEffect Clone()
        {
            return new RestoredStaminaAttackStatusEffect(_attack);
        }
    }
}
