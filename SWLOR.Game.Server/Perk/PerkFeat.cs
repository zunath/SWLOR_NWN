using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk
{
    public class PerkFeat
    {
        public Feat Feat { get; set; }
        public int Tier { get; set; }
        public int BaseFPCost { get; set; }
        public int ConcentrationFPCost { get; set; }
        public int ConcentrationTickInterval { get; set; }
    }
}
