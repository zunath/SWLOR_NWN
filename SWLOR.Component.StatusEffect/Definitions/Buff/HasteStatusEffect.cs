using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Definitions.Buff
{
    public class HasteStatusEffect : StatusEffectBase
    {
        public override StatusEffectType Type => StatusEffectType.Haste;
        public override string Name => "Haste";
        public override EffectIconType Icon => EffectIconType.Haste;
        public override StatusEffectStackType StackingType => StatusEffectStackType.Disabled;
        public override float Frequency => 180f;

        public HasteStatusEffect()
        {
            StatGroup.SetStat(StatType.Haste, 15);
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffectType.Vfx_Imp_Haste), creature);
        }
    }
}
