using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Eventing
{
    internal class OnEventingModuleUnequip : BaseEvent
    {
        public override string Script => ScriptName.OnEventingModuleUnequip;
    }
}
