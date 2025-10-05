using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnEffectAppliedBefore : BaseEvent
    {
        public override string Script => ScriptName.OnEffectAppliedBefore;
    }
}
