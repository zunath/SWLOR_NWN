using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Perk.Contracts;

namespace SWLOR.Shared.Domain.Perk.ValueObjects
{
    public class PerkLevel
    {
        public int Price { get; set; }
        public string Description { get; set; }
        public List<FeatType> GrantedFeats { get; set; }
        public List<IPerkRequirement> Requirements { get; set; }
        public int DroidAISlots { get; set; }

        public PerkLevel()
        {
            GrantedFeats = new List<FeatType>();
            Requirements = new List<IPerkRequirement>();
        }
    }
}
