using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Event.SWLOR
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
