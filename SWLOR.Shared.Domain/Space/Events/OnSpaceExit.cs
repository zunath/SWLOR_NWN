using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Space.Events
{
    public class OnSpaceExit : BaseEvent
    {
        public override string Script => ScriptName.OnSpaceExit;
    }
}
