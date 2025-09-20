using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnServerLoaded : BaseEvent
    {
        public override string ScriptName => "server_loaded";
    }
}
