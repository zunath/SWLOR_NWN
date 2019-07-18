using System;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnPurchaseLand
    {
        public NWPlayer Player { get; set; }
        public string Sector { get; set; }
        public string AreaName { get; set; }
        public string AreaTag { get; set; }
        public string AreaResref { get; set; }
        public PCBaseType PCBaseType { get; set; }

        public OnPurchaseLand(NWPlayer player, string sector, string areaName, string areaTag, string areaResref, PCBaseType pcBaseType)
        {
            Player = player;
            Sector = sector;
            AreaName = areaName;
            AreaTag = areaTag;
            AreaResref = areaResref;
            PCBaseType = pcBaseType;
        }
    }
}
