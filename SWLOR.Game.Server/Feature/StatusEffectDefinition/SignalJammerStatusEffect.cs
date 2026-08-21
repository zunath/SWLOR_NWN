using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SignalJammerStatusEffect : StatusEffectBase
    {
        public override string Name => "Signal Jammer";
        public override EffectIconType Icon => EffectIconType.SignalJammerStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes =>
            StatusEffectCleanseType.Purify |
            StatusEffectCleanseType.TreatmentKit2 |
            StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Disruption;
        public override float Frequency => 1f;

        public SignalJammerStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalAndForceAbilityHitChancePercentAdjustment] = -6;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 0;
            StatGroup.Stats[StatType.AttackDelayReductionSuppressed] = 1;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            SuppressHaste(creature);
        }

        protected override void Reapply(uint creature)
        {
            SuppressHaste(creature);
        }

        protected override void Tick(uint creature)
        {
            SuppressHaste(creature);
        }

        private void SuppressHaste(uint creature)
        {
            StatusEffect.RemoveStatusEffect(creature, typeof(Hasten1StatusEffect), false);
            StatusEffect.RemoveStatusEffect(creature, typeof(Hasten2StatusEffect), false);

            var currentPenalty = StatGroup.Stats.TryGetValue(StatType.AttackDelayReductionPercent, out var penalty)
                ? penalty
                : 0;
            var hasteFromOtherSources = Stat.GetStatAdjustment(creature, StatType.AttackDelayReductionPercent) - currentPenalty;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = hasteFromOtherSources > 0
                ? -hasteFromOtherSources
                : 0;
        }
    }
}
