using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestPlaceable : BaseEvent
    {
        public override string Script => QuestScriptName.OnQuestPlaceable;
    }
}
