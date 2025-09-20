using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnServerLoaded : BaseEvent
    {
        public override string Script => ScriptName.OnServerLoaded;
    }
}
