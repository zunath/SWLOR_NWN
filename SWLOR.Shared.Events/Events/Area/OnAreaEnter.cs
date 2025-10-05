using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Area
{
    public class OnAreaEnter : BaseEvent
    {
        public override string Script => ScriptName.OnAreaEnter;
    }
}
