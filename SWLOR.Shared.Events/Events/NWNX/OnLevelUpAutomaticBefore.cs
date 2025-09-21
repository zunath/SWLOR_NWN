using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.NWNX
{
    public class OnLevelUpAutomaticBefore : BaseEvent
    {
        public override string Script => ScriptName.OnLevelUpAutomaticBefore;
    }
}
