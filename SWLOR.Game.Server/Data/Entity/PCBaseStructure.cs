
using System;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PCBaseStructure: IEntity
    {
        public PCBaseStructure()
        {
            ID = Guid.NewGuid();
            CustomName = "";
        }

        [Key]
        public Guid ID { get; set; }
        public Guid PCBaseID { get; set; }
        public BaseStructure BaseStructureID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public double Durability { get; set; }
        public BuildingStyle InteriorStyleID { get; set; }
        public BuildingStyle ExteriorStyleID { get; set; }
        public Guid? ParentPCBaseStructureID { get; set; }
        public string CustomName { get; set; }
        public int StructureBonus { get; set; }
        public DateTime? DateNextActivity { get; set; }
        public StructureModeType StructureModeID { get; set; }
    }
}
