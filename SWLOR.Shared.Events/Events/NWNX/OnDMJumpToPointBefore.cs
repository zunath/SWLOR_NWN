using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMJumpToPointBefore : BaseEvent
    {
        public override string Script => ScriptName.OnDMJumpToPointBefore;
    }
}
