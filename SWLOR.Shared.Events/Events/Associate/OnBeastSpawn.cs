using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Associate
{
    public class OnBeastSpawn : BaseEvent
    {
        public override string Script => ScriptName.OnBeastSpawn;
    }
}
