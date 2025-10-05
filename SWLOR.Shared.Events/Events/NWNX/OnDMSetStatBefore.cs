using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMSetStatBefore : BaseEvent
    {
        public override string Script => ScriptName.OnDMSetStatBefore;
    }
}
