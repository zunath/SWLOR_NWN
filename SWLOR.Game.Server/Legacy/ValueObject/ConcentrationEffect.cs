using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class ConcentrationEffect
    {
        public PerkType Type { get; set; }
        public int Tier { get; set; }

        public ConcentrationEffect(PerkType type, int tier)
        {
            Type = type;
            Tier = tier;
        }
    }
}
