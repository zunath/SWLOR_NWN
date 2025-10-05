using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Player
{
    public class OnPlayerTargetUpdated : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerTargetUpdated;
    }
}
