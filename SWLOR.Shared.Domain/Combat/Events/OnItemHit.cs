using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnItemHit : BaseEvent
    {
        public override string Script => CombatScriptName.OnItemHit;
    }
}
