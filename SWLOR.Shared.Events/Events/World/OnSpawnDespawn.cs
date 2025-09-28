using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.World
{
    public class OnSpawnDespawn : BaseEvent
    {
        public override string Script => ScriptName.OnSpawnDespawn;
    }
}
