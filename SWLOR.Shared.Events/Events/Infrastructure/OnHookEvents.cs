using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnHookEvents: BaseEvent
    {
        public override string Script => ScriptName.OnHookEvents;
    }
}
