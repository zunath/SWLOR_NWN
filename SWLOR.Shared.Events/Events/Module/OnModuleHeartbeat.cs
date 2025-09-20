using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleHeartbeat : BaseEvent
    {
        public override string ScriptName => "mod_heartbeat";
    }
}
