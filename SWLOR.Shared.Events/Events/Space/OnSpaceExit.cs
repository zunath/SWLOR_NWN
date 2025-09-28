using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Space
{
    public class OnSpaceExit : BaseEvent
    {
        public override string Script => ScriptName.OnSpaceExit;
    }
}
