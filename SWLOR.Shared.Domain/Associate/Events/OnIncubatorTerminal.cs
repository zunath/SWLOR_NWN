using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnIncubatorTerminal : BaseEvent
    {
        public override string Script => AssociateScriptName.OnIncubatorTerminal;
    }
}
