using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleDying : BaseEvent
    {
        public override string Script => ScriptName.OnModuleDying;
    }
}
