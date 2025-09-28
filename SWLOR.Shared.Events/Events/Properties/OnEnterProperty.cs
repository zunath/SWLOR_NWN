using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Properties
{
    public class OnEnterProperty : BaseEvent
    {
        public override string Script => ScriptName.OnEnterProperty;
    }
}
