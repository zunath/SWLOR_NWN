using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnLevelUpAutomaticBefore : BaseEvent
    {
        public override string Script => ScriptName.OnLevelUpAutomaticBefore;
    }
}
