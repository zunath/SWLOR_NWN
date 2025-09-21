using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleGuiEvent : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleGuiEvent;
    }
}
