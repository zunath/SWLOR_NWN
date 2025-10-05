using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnRestTriggerExit : BaseEvent
    {
        public override string Script => ScriptName.OnRestTriggerExit;
    }
}
