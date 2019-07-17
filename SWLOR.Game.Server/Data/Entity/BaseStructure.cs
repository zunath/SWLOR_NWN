using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BaseStructure]")]
    public class BaseStructure: IEntity
    {
        public BaseStructure()
        {
            Name = "";
            PlaceableResref = "";
            ItemResref = "";
        }


        [ExplicitKey]
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
        public int DefaultStructureModeID { get; set; }

        public IEntity Clone()
        {
            return new BaseStructure
            {
                ID = ID,
                BaseStructureTypeID = BaseStructureTypeID,
                Name = Name,
                PlaceableResref = PlaceableResref,
                ItemResref = ItemResref,
                IsActive = IsActive,
                Power = Power,
                CPU = CPU,
                Durability = Durability,
                Storage = Storage,
                HasAtmosphere = HasAtmosphere,
                ReinforcedStorage = ReinforcedStorage,
                RequiresBasePower = RequiresBasePower,
                ResourceStorage = ResourceStorage,
                RetrievalRating = RetrievalRating,
                FuelRating = FuelRating,
                DefaultStructureModeID = DefaultStructureModeID
            };
        }

    }
}
