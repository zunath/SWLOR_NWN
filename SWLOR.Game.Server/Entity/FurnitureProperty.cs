using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.HousingService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Entity
{
    public class FurnitureProperty: PropertyBase
    {
        [Indexed]
        public FurnitureType FurnitureType { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Orientation { get; set; }

        public override void SpawnIntoWorld(uint area)
        {
            var furniture = Property.GetFurnitureByType(FurnitureType);

            var position = Vector3(X, Y, Z);
            var location = Location(area, position, Orientation);

            var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
            Property.AssignPropertyId(placeable, Id.ToString());
        }
    }
}
