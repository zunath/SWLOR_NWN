using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnEnterWorld : BaseEvent
    {
        public override string Script => WorldScriptName.OnEnterWorld;
    }
}
