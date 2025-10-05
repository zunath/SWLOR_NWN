using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.StatusEffect.Events
{
    public class OnStatusEffectInterval : BaseEvent
    {
        public override string Script => StatusEffectScriptName.OnStatusEffectIntervalScript;
    }
}
