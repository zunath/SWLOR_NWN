using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Quest
{
    public class OnQuestForceCrystal : BaseEvent
    {
        public override string Script => ScriptName.OnQuestForceCrystal;
    }
}
