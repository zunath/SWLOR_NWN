using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Inventory
{
    public class OnTrashDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnTrashDisturbed;
    }
}
