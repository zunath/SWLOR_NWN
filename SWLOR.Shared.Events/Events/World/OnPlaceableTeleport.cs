using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.World
{
    public class OnPlaceableTeleport : BaseEvent
    {
        public override string Script => ScriptName.OnPlaceableTeleport;
    }
}
