using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class HostileAbilityForceAttackStatusEffect : StatusEffectBase
    {
        private readonly int _forceAttack;

        public int ForceAttack => _forceAttack;
        public override string Name => "Hostile Ability Force Attack";
        public override EffectIconType Icon => EffectIconType.HostileAbilityForceAttackStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public HostileAbilityForceAttackStatusEffect()
            : this(5)
        {
        }

        public HostileAbilityForceAttackStatusEffect(int forceAttack)
        {
            _forceAttack = forceAttack;
            StatGroup.Stats[StatType.ForceAttackPercentAdjustment] = forceAttack;
        }

        public override IStatusEffect Clone()
        {
            return new HostileAbilityForceAttackStatusEffect(_forceAttack);
        }
    }
}
