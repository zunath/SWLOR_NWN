using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureAttackAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureAttackAfter;
    }
}
