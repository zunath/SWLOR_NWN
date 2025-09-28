using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Character
{
    public class OnRestTriggerExit : BaseEvent
    {
        public override string Script => ScriptName.OnRestTriggerExit;
    }
}
