using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMSetFactionAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMSetFactionAfter;
    }
}
