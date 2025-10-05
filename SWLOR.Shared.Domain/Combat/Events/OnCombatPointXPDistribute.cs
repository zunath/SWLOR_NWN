using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnCombatPointXPDistribute : BaseEvent
    {
        public override string Script => ScriptName.OnCombatPointXPDistribute;
    }
}
