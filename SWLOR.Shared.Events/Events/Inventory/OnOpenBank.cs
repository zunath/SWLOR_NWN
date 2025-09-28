using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Inventory
{
    public class OnOpenBank : BaseEvent
    {
        public override string Script => ScriptName.OnOpenBank;
    }
}
