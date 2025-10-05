using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnUseShipComputer : BaseEvent
    {
        public override string Script => ScriptName.OnUseShipComputer;
    }
}
