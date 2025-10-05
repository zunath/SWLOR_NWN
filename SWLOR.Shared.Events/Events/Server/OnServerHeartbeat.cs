using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Server
{
    public class OnServerHeartbeat: BaseEvent
    {
        public override string Script => ScriptName.OnServerHeartbeat;
    }
}
