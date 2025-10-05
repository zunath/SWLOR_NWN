using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Properties.Events
{
    public class OnApartmentTerminal : BaseEvent
    {
        public override string Script => ScriptName.OnApartmentTerminal;
    }
}
