using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestTrigger : BaseEvent
    {
        public override string Script => ScriptName.OnQuestTrigger;
    }
}
