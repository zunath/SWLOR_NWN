using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestCollectDisturbed : BaseEvent
    {
        public override string Script => ScriptName.OnQuestCollectDisturbed;
    }
}
