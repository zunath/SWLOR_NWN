using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnValidateItemEquipBefore : BaseEvent
    {
        public override string Script => ScriptName.OnValidateItemEquipBefore;
    }
}
