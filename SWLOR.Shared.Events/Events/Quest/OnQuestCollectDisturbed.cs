using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Quest
{
    public class OnQuestCollectDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnQuestCollectDisturbed;
    }
}
