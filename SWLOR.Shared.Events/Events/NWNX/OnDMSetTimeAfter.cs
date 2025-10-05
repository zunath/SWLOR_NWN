using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMSetTimeAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMSetTimeAfter;
    }
}
