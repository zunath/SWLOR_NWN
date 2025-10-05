using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnIntervalPC6Seconds : BaseEvent
    {
        public override string Script => CombatScriptName.OnIntervalPC6Seconds;
    }
}
