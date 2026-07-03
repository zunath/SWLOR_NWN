using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class NearbyAllyAttackDeflectionStatusEffect : StatusEffectBase
    {
        private readonly string _name;
        private readonly EffectIconType _icon;
        private readonly int _attackDeflection;
        private readonly int _selfEnmityPercentAdjustment;

        public override string Name => _name;
        public override EffectIconType Icon => _icon;

        public NearbyAllyAttackDeflectionStatusEffect()
            : this(8, 20, "Nearby Ally Attack Deflection", EffectIconType.Invalid)
        {
        }

        public NearbyAllyAttackDeflectionStatusEffect(
            int attackDeflection,
            int selfEnmityPercentAdjustment,
            string name = "Nearby Ally Attack Deflection",
            EffectIconType icon = EffectIconType.Invalid)
        {
            _name = name;
            _icon = icon;
            _attackDeflection = attackDeflection;
            _selfEnmityPercentAdjustment = selfEnmityPercentAdjustment;
            StatGroup.Stats[StatType.AttackDeflection] = _attackDeflection;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (creature == Source)
            {
                StatGroup.Stats.Remove(StatType.AttackDeflection);
                StatGroup.Stats[StatType.EnmityPercentAdjustment] = _selfEnmityPercentAdjustment;
            }
        }

        public override IStatusEffect Clone()
        {
            return new NearbyAllyAttackDeflectionStatusEffect(
                _attackDeflection,
                _selfEnmityPercentAdjustment,
                _name,
                _icon);
        }
    }
}
