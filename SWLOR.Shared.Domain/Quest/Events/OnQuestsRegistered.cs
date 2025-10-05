using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnQuestsRegistered : BaseEvent
    {
        public override string Script => ScriptName.OnQuestsRegistered;
    }
}
