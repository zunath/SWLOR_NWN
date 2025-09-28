using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.EventAggregator;

namespace SWLOR.Shared.Events.Events.Perk
{
    public class OnPlayerBuyPerk : BaseEvent
    {
        public override string Script => ScriptName.OnPlayerBuyPerk;
    }
}
