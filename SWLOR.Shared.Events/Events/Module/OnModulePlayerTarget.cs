using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModulePlayerTarget : BaseEvent
    {
        public override string Script => ScriptName.OnModulePlayerTarget;
    }
}
