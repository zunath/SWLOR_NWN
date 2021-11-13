using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Entity
{
    public abstract class PropertyBase: EntityBase
    {
        public string CustomName { get; set; }
        public PropertyType Type { get; set; }
        public string OwnerPlayerId { get; set; }
    }
}
