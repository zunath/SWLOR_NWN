using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EvasiveCombatStatusEffect : StatusEffectBase
    {
        private readonly int _attackPercent;
        private readonly int _evasionPercent;
        private readonly int _enmityPercent;

        public override string Name => "Evasive Combat";
        public override EffectIconType Icon => EffectIconType.EvasiveCombatStatusEffect;

        public EvasiveCombatStatusEffect()
            : this(-15, 10, -15)
        {
        }

        public EvasiveCombatStatusEffect(int attackPercent, int evasionPercent, int enmityPercent)
        {
            _attackPercent = attackPercent;
            _evasionPercent = evasionPercent;
            _enmityPercent = enmityPercent;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = _attackPercent;
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = _evasionPercent;
            StatGroup.Stats[StatType.EnmityPercentAdjustment] = _enmityPercent;
        }

        public override IStatusEffect Clone()
        {
            return new EvasiveCombatStatusEffect(_attackPercent, _evasionPercent, _enmityPercent);
        }
    }
}
