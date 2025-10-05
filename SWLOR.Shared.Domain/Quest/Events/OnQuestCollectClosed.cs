using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestCollectClosed : BaseEvent
    {
        public override string Script => QuestScriptName.OnQuestCollectClosed;
    }
}
