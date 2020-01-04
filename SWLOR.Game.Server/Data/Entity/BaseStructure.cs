using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class BaseStructure: IEntity
    {
        public BaseStructure()
        {
            Name = "";
            PlaceableResref = "";
            ItemResref = "";
        }


        [Key]
        public int ID { get; set; }
        public int BaseStructureTypeID { get; set; }
        public string Name { get; set; }
        public string PlaceableResref { get; set; }
        public string ItemResref { get; set; }
        public bool IsActive { get; set; }
        public double Power { get; set; }
        public double CPU { get; set; }
        public double Durability { get; set; }
        public int Storage { get; set; }
        public bool HasAtmosphere { get; set; }
        public int ReinforcedStorage { get; set; }
        public bool RequiresBasePower { get; set; }
        public int ResourceStorage { get; set; }
        public int RetrievalRating { get; set; }
        public int FuelRating { get; set; }
        public StructureModeType DefaultStructureModeID { get; set; }
    }
}
