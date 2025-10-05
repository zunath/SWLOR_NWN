using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.World.Events
{
    public class OnSpawnDespawn : BaseEvent
    {
        public override string Script => ScriptName.OnSpawnDespawn;
    }
}
