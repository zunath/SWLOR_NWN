using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Server
{
    public class OnServerHeartbeat: BaseEvent
    {
        public override string Script => ScriptName.OnServerHeartbeat;
    }
}
