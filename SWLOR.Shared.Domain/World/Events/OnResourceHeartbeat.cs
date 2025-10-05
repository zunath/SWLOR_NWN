using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnResourceHeartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnResourceHeartbeat;
    }
}
