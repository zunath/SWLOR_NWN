using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.StatusEffect.Events
{
    public class OnApplyStatusEffect : BaseEvent
    {
        public override string Script => StatusEffectScriptName.OnApplyStatusEffectScript;
    }
}
