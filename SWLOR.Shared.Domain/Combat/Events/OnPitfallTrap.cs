using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnPitfallTrap : BaseEvent
    {
        public override string Script => CombatScriptName.OnPitfallTrap;
    }
}
