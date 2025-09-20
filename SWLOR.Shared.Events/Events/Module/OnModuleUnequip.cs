using SWLOR.Shared.Events.EventAggregator;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.Module
{
    public class OnModuleUnequip : BaseEvent
    {
        public override string Script => ScriptName.OnModuleUnequip;
    }
}
