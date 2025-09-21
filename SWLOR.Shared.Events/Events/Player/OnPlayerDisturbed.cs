using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Player
{
    public class OnPlayerDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerDisturbed;
    }
}
