using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnAssociateRemoveAfter : BaseEvent
    {
        public override string Script => ScriptName.OnAssociateRemoveAfter;
    }
}
