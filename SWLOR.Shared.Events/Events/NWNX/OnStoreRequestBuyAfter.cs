using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnStoreRequestBuyAfter : BaseEvent
    {
        public override string Script => ScriptName.OnStoreRequestBuyAfter;
    }
}
