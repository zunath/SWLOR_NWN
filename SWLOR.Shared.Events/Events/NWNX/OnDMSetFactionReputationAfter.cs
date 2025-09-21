using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMSetFactionReputationAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMSetFactionReputationAfter;
    }
}
