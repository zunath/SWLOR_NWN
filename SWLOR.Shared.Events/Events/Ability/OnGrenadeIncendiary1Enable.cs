using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Ability
{
    public class OnGrenadeIncendiary1Enable : BaseEvent
    {
        public override string Script => ScriptName.OnGrenadeIncendiary1Enable;
    }
}
