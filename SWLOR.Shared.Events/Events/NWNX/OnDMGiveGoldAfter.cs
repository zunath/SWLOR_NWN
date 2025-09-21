using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMGiveGoldAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMGiveGoldAfter;
    }
}
