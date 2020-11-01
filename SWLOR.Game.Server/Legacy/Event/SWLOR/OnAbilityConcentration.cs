using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnAbilityConcentration
    {
        public PerkType PerkType { get; set; }

        public OnAbilityConcentration(PerkType perkType)
        {
            PerkType = perkType;
        }
    }
}
