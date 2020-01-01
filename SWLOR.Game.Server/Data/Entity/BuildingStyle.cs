using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class BuildingStyle: IEntity
    {
        public BuildingStyle()
        {
            Name = "";
            Resref = "";
            DoorRule = "";
        }

        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public int? BaseStructureID { get; set; }
        public bool IsDefault { get; set; }
        public string DoorRule { get; set; }
        public bool IsActive { get; set; }
        public int BuildingTypeID { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public int FurnitureLimit { get; set; }
    }
}
