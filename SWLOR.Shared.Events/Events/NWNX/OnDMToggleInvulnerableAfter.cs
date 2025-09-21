using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMToggleInvulnerableAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMToggleInvulnerableAfter;
    }
}
