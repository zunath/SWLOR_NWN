using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureBlockedAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureBlockedAfter;
    }
}
