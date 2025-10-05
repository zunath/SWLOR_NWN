using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestCollectOpen : BaseEvent
    {
        public override string Script => ScriptName.OnQuestCollectOpen;
    }
}
