using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Area
{
    public class OnAreaHeartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnAreaHeartbeat;
    }
}
