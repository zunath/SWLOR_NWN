using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Inventory
{
    public class OnTrashOpened : BaseEvent
    {
        public override string Script => ScriptName.OnTrashOpened;
    }
}
