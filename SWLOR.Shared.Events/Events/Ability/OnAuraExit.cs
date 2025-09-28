using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Ability
{
    public class OnAuraExit : BaseEvent
    {
        public override string Script => ScriptName.OnAuraExit;
    }
}
