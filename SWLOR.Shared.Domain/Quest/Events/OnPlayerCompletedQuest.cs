using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnPlayerCompletedQuest : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerCompletedQuest;
    }
}
