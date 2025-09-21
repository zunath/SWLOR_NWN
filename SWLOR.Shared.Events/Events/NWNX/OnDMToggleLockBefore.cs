using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMToggleLockBefore : BaseEvent
    {
        public override string Script => ScriptName.OnDMToggleLockBefore;
    }
}
