using System;

namespace SWLOR.Game.Server.Service.TaxiService
{
    public enum TaxiDestinationType
    {
        [TaxiDestination(0, "Invalid Destination", "", int.MaxValue)]
        Invalid = 0,

        // Viscara: Veles Colony

        [TaxiDestination(1, "Veles Entrance", "TAXI_VELES_ENTRANCE", 100)]
        VelesEntrance = 1,
        [TaxiDestination(1, "Veles Bank", "TAXI_VELES_BANK", 100)]
        VelesBank = 2,
        [TaxiDestination(1, "Czerka Offices", "TAXI_VELES_CZERKA", 100)]
        VelesCzerkaOffices = 3,
        [TaxiDestination(1, "Veles General Store", "TAXI_VELES_GENERAL", 100)]
        VelesGeneralStore = 4,
        [TaxiDestination(1, "Veles Medical Center", "TAXI_VELES_MEDICAL", 100)]
        VelesMedicalCenter = 5,
        [TaxiDestination(1, "Veles Starport", "TAXI_VELES_STARPORT", 100)]
        VelesStarport = 6,
        [TaxiDestination(1, "Veles Research & Development", "TAXI_VELES_RD", 100)]
        VelesResearchAndDevelopment = 7,
        [TaxiDestination(1, "Veles Cantina", "TAXI_VELES_CANTINA", 100)]
        VelesCantina = 8,
        [TaxiDestination(1, "Veles Fosz Estate", "VELES_FOSZ_ESTATE", 100)]
        VelesFoszEstate = 9,
        [TaxiDestination(1, "Veles Apartments", "TAXI_VELES_APARTMENTS", 100)]
        VelesApartments = 10,
        [TaxiDestination(1, "Outpost Hope", "TAXI_VELES_OUTPOST", 1000)]
        OutpostHope = 11,
    }

    public class TaxiDestinationAttribute: Attribute
    {
        public int RegionId { get; set; }
        public string Name { get; set; }
        public string WaypointTag { get; set; }
        public int Price { get; set; }

        public TaxiDestinationAttribute(int regionId, string name, string waypointTag, int price)
        {
            RegionId = regionId;
            Name = name;
            WaypointTag = waypointTag;
            Price = price;
        }
    }
}
