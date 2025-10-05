using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnBarterEndAfter : BaseEvent
    {
        public override string Script => ScriptName.OnBarterEndAfter;
    }
}
