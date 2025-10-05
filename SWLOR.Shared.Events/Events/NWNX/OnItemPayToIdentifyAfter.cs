using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnItemPayToIdentifyAfter : BaseEvent
    {
        public override string Script => ScriptName.OnItemPayToIdentifyAfter;
    }
}
