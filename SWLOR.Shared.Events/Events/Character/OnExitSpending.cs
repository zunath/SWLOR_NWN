using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Character
{
    public class OnExitSpending : BaseEvent
    {
        public override string Script => ScriptName.OnExitSpending;
    }
}
