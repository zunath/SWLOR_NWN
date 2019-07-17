
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCBase]")]
    public class PCBase: IEntity
    {
        public PCBase()
        {
            ID = Guid.NewGuid();
            CustomName = "";
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Sector { get; set; }
        public DateTime DateInitialPurchase { get; set; }
        public DateTime DateRentDue { get; set; }
        public int ShieldHP { get; set; }
        public bool IsInReinforcedMode { get; set; }
        public int Fuel { get; set; }
        public int ReinforcedFuel { get; set; }
        public DateTime DateFuelEnds { get; set; }
        public int PCBaseTypeID { get; set; }
        public int? ApartmentBuildingID { get; set; }
        public string CustomName { get; set; }
        public int? BuildingStyleID { get; set; }
        public string ShipLocation { get; set; }
        public int? Starcharts { get; set; }

        public IEntity Clone()
        {
            return new PCBase
            {
                ID = ID,
                PlayerID = PlayerID,
                AreaResref = AreaResref,
                Sector = Sector,
                DateInitialPurchase = DateInitialPurchase,
                DateRentDue = DateRentDue,
                ShieldHP = ShieldHP,
                IsInReinforcedMode = IsInReinforcedMode,
                Fuel = Fuel,
                ReinforcedFuel = ReinforcedFuel,
                DateFuelEnds = DateFuelEnds,
                PCBaseTypeID = PCBaseTypeID,
                ApartmentBuildingID = ApartmentBuildingID,
                CustomName = CustomName,
                BuildingStyleID = BuildingStyleID,
                ShipLocation = ShipLocation,
                Starcharts = Starcharts
            };
        }
    }
}
