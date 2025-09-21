using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleExit : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleExit;
    }
}
