using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnValidateItemEquipBefore : BaseEvent
    {
        public override string Script => ScriptName.OnValidateItemEquipBefore;
    }
}
