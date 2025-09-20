using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnServerLoaded : BaseEvent
    {
        public override string ScriptName => "server_loaded";
    }
}
