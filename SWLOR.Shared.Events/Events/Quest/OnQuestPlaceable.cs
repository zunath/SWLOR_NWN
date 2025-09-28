using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Quest
{
    public class OnQuestPlaceable : BaseEvent
    {
        public override string Script => ScriptName.OnQuestPlaceable;
    }
}
