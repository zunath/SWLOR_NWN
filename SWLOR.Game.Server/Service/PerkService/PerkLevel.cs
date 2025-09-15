using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerkService
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
