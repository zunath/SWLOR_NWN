using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Domain.Combat;

namespace SWLOR.Shared.Events.Events.Player
{
    public class OnPlayerRestStarted: BaseEvent
    {
        public override string Script => CombatScriptName.OnPlayerRestStarted;
    }
}
