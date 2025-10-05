using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastHeartbeat : BaseEvent
    {
        public override string Script => ScriptName.OnBeastHeartbeat;
    }
}
