using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Ability
{
    public class OnGrenadeGas2Heartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnGrenadeGas2Heartbeat;
    }
}
