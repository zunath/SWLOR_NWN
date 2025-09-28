using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnHookNativeOverrides: BaseEvent
    {
        public override string Script => ScriptName.OnHookNativeOverrides;
    }
}
