using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureAttackBefore : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureAttackBefore;
    }
}
