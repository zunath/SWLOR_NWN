using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.ValueObject
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
