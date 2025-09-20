using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnEventsHooked: BaseEvent
    {
        public override string Script => ScriptName.OnEventsHooked;
    }
}
