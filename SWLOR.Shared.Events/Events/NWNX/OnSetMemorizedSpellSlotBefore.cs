using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnSetMemorizedSpellSlotBefore : BaseEvent
    {
        public override string Script => ScriptName.OnSetMemorizedSpellSlotBefore;
    }
}
