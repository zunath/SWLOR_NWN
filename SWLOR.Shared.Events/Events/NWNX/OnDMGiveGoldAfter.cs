using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMGiveGoldAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMGiveGoldAfter;
    }
}
