using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Properties
{
    public class OnOpenPropertyBank : BaseEvent
    {
        public override string Script => ScriptName.OnOpenPropertyBank;
    }
}
