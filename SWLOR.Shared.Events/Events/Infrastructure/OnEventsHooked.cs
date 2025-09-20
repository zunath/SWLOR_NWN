using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnEventsHooked: BaseEvent
    {
        public override string ScriptName => "events_hooked";
    }
}
