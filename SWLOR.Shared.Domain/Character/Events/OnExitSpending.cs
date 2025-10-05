using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Character.Events
{
    public class OnExitSpending : BaseEvent
    {
        public override string Script => ScriptName.OnExitSpending;
    }
}
