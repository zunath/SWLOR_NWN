using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BuildingStyle]")]
    public class BuildingStyle: IEntity
    {
        public BuildingStyle()
        {
            Name = "";
            Resref = "";
            DoorRule = "";
        }

        [ExplicitKey]
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

        public IEntity Clone()
        {
            return new BuildingStyle
            {
                ID = ID,
                Name = Name,
                Resref = Resref,
                BaseStructureID = BaseStructureID,
                IsDefault = IsDefault,
                DoorRule = DoorRule,
                IsActive = IsActive,
                BuildingTypeID = BuildingTypeID,
                PurchasePrice = PurchasePrice,
                DailyUpkeep = DailyUpkeep,
                FurnitureLimit = FurnitureLimit
            };
        }
    }
}
