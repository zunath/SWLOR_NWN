using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnPartyLeaveBefore : BaseEvent
    {
        public override string Script => ScriptName.OnPartyLeaveBefore;
    }
}
