using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Area
{
    public class OnAreaHeartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnAreaHeartbeat;
    }
}
