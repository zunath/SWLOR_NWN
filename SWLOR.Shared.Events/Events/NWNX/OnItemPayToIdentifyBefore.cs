using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnItemPayToIdentifyBefore : BaseEvent
    {
        public override string Script => ScriptName.OnItemPayToIdentifyBefore;
    }
}
