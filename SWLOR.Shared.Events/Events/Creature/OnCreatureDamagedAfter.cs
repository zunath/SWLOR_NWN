using SWLOR.Shared.Abstractions;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Creature
{
    public class OnCreatureDamagedAfter : BaseEvent
    {
        public override string Script => ScriptName.OnCreatureDamagedAfter;
    }
}
