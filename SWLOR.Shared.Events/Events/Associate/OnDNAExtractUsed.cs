using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Associate
{
    public class OnDNAExtractUsed : BaseEvent
    {
        public override string Script => ScriptName.OnDNAExtractUsed;
    }
}
