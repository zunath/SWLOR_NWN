using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.PerkService
{
    public class PerkLevel
    {
        public int Price { get; set; }
        public string Description { get; set; }
        public List<Feat> GrantedFeats { get; set; }
        public List<IPerkRequirement> Requirements { get; set; }

        public PerkLevel()
        {
            GrantedFeats = new List<Feat>();
            Requirements = new List<IPerkRequirement>();
        }
    }
}
