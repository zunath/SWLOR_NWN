using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BaseStructures]")]
    public class BaseStructure: IEntity
    {
        public BaseStructure()
        {
            Name = "";
            PlaceableResref = "";
            ItemResref = "";
        }


        [ExplicitKey]
        public int BaseStructureID { get; set; }
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
   
    }
}
