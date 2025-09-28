using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Space
{
    public class OnPlayerShieldAdjusted : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerShieldAdjusted;
    }
}
