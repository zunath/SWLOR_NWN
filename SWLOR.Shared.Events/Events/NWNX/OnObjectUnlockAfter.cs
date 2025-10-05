using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnObjectUnlockAfter : BaseEvent
    {
        public override string Script => ScriptName.OnObjectUnlockAfter;
    }
}
