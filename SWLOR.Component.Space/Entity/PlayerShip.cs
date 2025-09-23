using SWLOR.Component.Space.Enums;
using SWLOR.Shared.Abstractions;

namespace SWLOR.Component.Space.Entity
{
    public class PlayerShip: EntityBase
    {
        public PlayerShip()
        {
            PlayerHotBars = new Dictionary<string, string>();
        }

        [Indexed]
        public string OwnerPlayerId { get; set; }
        [Indexed]
        public string PropertyId { get; set; }
        public string SerializedItem { get; set; }
        public ShipStatus Status { get; set; }
        public Dictionary<string, string> PlayerHotBars { get; set; }
    }
}
