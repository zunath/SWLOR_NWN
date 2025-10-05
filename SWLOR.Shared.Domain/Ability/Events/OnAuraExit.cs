using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Ability.Events
{
    public class OnAuraExit : BaseEvent
    {
        public override string Script => AbilityScriptName.OnAuraExit;
    }
}
