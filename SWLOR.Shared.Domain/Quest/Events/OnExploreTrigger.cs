using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Quest.Events
{
    public class OnExploreTrigger : BaseEvent
    {
        public override string Script => ScriptName.OnExploreTrigger;
    }
}
