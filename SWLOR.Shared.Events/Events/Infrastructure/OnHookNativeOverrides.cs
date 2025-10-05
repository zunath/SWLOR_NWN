using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Infrastructure
{
    public class OnHookNativeOverrides: BaseEvent
    {
        public override string Script => ScriptName.OnHookNativeOverrides;
    }
}
