using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnEnmityChanged : BaseEvent
    {
        public override string Script => CombatScriptName.OnEnmityChanged;
    }
}
