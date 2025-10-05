using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnAuraEnter : BaseEvent
    {
        public override string Script => AbilityScriptName.OnAuraEnter;
    }
}
