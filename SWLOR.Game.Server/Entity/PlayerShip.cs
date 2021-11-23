using System;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Entity
{
    public class PlayerShip: EntityBase
    {
        [Indexed]
        public string PlayerId { get; set; }
        public ShipStatus Status { get; set; }

        [Indexed]
        public string AreaResref { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public string SerializedHotBar { get; set; }
    }
}
