using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Inventory
{
    public class OnHarvesterUsed : BaseEvent
    {
        public override string Script => ScriptName.OnHarvesterUsed;
    }
}
