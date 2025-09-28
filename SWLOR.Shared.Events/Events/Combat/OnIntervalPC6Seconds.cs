using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Combat
{
    public class OnIntervalPC6Seconds : BaseEvent
    {
        public override string Script => ScriptName.OnIntervalPC6Seconds;
    }
}
