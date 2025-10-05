using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnPartyKickAfter : BaseEvent
    {
        public override string Script => ScriptName.OnPartyKickAfter;
    }
}
