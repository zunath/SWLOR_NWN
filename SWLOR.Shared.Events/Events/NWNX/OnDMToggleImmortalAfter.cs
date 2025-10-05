using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnDMToggleImmortalAfter : BaseEvent
    {
        public override string Script => ScriptName.OnDMToggleImmortalAfter;
    }
}
