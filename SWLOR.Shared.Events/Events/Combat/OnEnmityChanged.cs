using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Combat
{
    public class OnEnmityChanged : BaseEvent
    {
        public override string Script => ScriptName.OnEnmityChanged;
    }
}
