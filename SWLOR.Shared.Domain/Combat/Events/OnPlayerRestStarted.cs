using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnPlayerRestStarted: BaseEvent
    {
        public override string Script => CombatScriptName.OnPlayerRestStarted;
    }
}
