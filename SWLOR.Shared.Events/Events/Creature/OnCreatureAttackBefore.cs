using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureAttackBefore : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureAttackBefore;
    }
}
