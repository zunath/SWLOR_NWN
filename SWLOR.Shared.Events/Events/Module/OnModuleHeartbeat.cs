using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleHeartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnModuleHeartbeat;
    }
}
