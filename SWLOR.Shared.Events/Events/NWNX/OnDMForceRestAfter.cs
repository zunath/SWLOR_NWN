using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMForceRestAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMForceRestAfter;
    }
}
