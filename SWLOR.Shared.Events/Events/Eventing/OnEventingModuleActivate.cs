using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleActivate : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleActivate;
    }
}
