using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
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
