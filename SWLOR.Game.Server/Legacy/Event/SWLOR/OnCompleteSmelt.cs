using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
{
    public class OnCompleteSmelt
    {
        public NWPlayer Player { get; set; }
        public string OreResref { get; set; }
        public List<ItemProperty> ItemProperties { get; set; }

        public OnCompleteSmelt(NWPlayer player, string oreResref, List<ItemProperty> itemProperties)
        {
            Player = player;
            OreResref = oreResref;
            ItemProperties = itemProperties;
        }
    }
}
