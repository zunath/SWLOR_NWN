using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Associate
{
    public class OnBeastSpellCast : BaseEvent
    {
        public override string Script => ScriptName.OnBeastSpellCast;
    }
}
