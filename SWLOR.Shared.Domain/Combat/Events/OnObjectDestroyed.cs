using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnObjectDestroyed : BaseEvent
    {
        public override string Script => CombatScriptName.OnObjectDestroyed;
    }
}
