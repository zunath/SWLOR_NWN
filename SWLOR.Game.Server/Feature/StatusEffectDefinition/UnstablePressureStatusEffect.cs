using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class UnstablePressureStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPenaltyPercent;
        private readonly int _lowHPForceDamageTakenPercent;
        private readonly int _lowHPThresholdPercent;

        public override string Name => "Unstable Pressure";
        public override EffectIconType Icon => EffectIconType.UnstablePressureStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.SoothePet;
        public override float Frequency => 1f;

        public UnstablePressureStatusEffect()
            : this(5, 5, 35)
        {
        }

        public UnstablePressureStatusEffect(
            int evasionPenaltyPercent,
            int lowHPForceDamageTakenPercent,
            int lowHPThresholdPercent)
        {
            _evasionPenaltyPercent = System.Math.Abs(evasionPenaltyPercent);
            _lowHPForceDamageTakenPercent = System.Math.Abs(lowHPForceDamageTakenPercent);
            _lowHPThresholdPercent = System.Math.Clamp(lowHPThresholdPercent, 1, 100);
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = -_evasionPenaltyPercent;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = 0;
        }

        public override IStatusEffect Clone()
        {
            return new UnstablePressureStatusEffect(
                _evasionPenaltyPercent,
                _lowHPForceDamageTakenPercent,
                _lowHPThresholdPercent);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            UpdateLowHPForceDamagePenalty(creature);
        }

        protected override void Reapply(uint creature)
        {
            UpdateLowHPForceDamagePenalty(creature);
        }

        protected override void Tick(uint creature)
        {
            UpdateLowHPForceDamagePenalty(creature);
        }

        private void UpdateLowHPForceDamagePenalty(uint creature)
        {
            var maxHP = GetMaxHitPoints(creature);
            var isLowHP = maxHP > 0 &&
                          GetCurrentHitPoints(creature) <= maxHP * _lowHPThresholdPercent / 100;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = isLowHP
                ? _lowHPForceDamageTakenPercent
                : 0;
        }
    }
}
