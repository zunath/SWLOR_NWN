using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestForceCrystal : BaseEvent
    {
        public override string Script => ScriptName.OnQuestForceCrystal;
    }
}
