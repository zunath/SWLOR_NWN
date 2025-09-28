using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Combat
{
    public class OnEnmityAcquired : BaseEvent
    {
        public override string Script => ScriptName.OnEnmityAcquired;
    }
}
