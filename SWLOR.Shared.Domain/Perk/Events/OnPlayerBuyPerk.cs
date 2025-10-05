using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Perk.Events
{
    public class OnPlayerBuyPerk : BaseEvent
    {
        public override string Script => PerkScriptName.OnPlayerBuyPerk;
    }
}
