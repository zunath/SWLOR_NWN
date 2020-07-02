
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCBaseStructure")]
    public class PCBaseStructure: IEntity
    {
        public PCBaseStructure()
        {
            ID = Guid.NewGuid();
            CustomName = "";
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PCBaseID { get; set; }
        public int BaseStructureID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public double Durability { get; set; }
        public int? InteriorStyleID { get; set; }
        public int? ExteriorStyleID { get; set; }
        public Guid? ParentPCBaseStructureID { get; set; }
        public string CustomName { get; set; }
        public int StructureBonus { get; set; }
        public DateTime? DateNextActivity { get; set; }
        public int StructureModeID { get; set; }
        public int TileMainLight1Color { get; set; }
        public int TileMainLight2Color { get; set; }
        public int TileSourceLight1Color { get; set; }
        public int TileSourceLight2Color { get; set; }

        public IEntity Clone()
        {
            return new PCBaseStructure
            {
                ID = ID,
                PCBaseID = PCBaseID,
                BaseStructureID = BaseStructureID,
                LocationX = LocationX,
                LocationY = LocationY,
                LocationZ = LocationZ,
                LocationOrientation = LocationOrientation,
                Durability = Durability,
                InteriorStyleID = InteriorStyleID,
                ExteriorStyleID = ExteriorStyleID,
                ParentPCBaseStructureID = ParentPCBaseStructureID,
                CustomName = CustomName,
                StructureBonus = StructureBonus,
                DateNextActivity = DateNextActivity,
                StructureModeID = StructureModeID,
                TileMainLight1Color = TileMainLight1Color,
                TileMainLight2Color = TileMainLight2Color,
                TileSourceLight1Color = TileSourceLight1Color,
                TileSourceLight2Color = TileSourceLight2Color
            };
        }
    }
}
