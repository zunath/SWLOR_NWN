using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Ability
{
    public class OnAuraEnter : BaseEvent
    {
        public override string Script => ScriptName.OnAuraEnter;
    }
}
