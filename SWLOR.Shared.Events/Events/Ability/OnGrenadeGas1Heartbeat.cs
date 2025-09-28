using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Ability
{
    public class OnGrenadeGas1Heartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnGrenadeGas1Heartbeat;
    }
}
