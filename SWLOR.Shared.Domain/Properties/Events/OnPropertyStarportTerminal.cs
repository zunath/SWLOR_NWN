using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Properties.Events
{
    public class OnPropertyStarportTerminal : BaseEvent
    {
        public override string Script => ScriptName.OnPropertyStarportTerminal;
    }
}
