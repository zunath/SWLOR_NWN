using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.StatusEffect.Events
{
    public class OnRemoveStatusEffect : BaseEvent
    {
        public override string Script => StatusEffectScriptName.OnRemoveStatusEffectScript;
    }
}
