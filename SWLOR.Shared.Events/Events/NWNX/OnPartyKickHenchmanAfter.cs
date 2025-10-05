using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnPartyKickHenchmanAfter : BaseEvent
    {
        public override string Script => ScriptName.OnPartyKickHenchmanAfter;
    }
}
