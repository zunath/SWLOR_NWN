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
        [TaxiDestination(1, "Veles Market", "TAXI_VELES_MARKET", 100)]
        VelesMarket = 2,
        [TaxiDestination(1, "Czerka Tower", "TAXI_VELES_CZERKATOWER", 100)]
        VelesCzerkaTower = 3,
        [TaxiDestination(1, "Veles Council Chamber", "TAXI_VELES_COUNCIL", 100)]
        VelesCouncilChamber = 4,
        [TaxiDestination(1, "Veles Medical Center", "TAXI_VELES_MEDICAL", 100)]
        VelesMedicalCenter = 5,
        [TaxiDestination(1, "Veles Starport", "TAXI_VELES_STARPORT", 100)]
        VelesStarport = 6,
        [TaxiDestination(1, "Veles Industrial District", "TAXI_VELES_INDUSTRIAL", 100)]
        VelesIndustrial = 7,
        [TaxiDestination(1, "Veles Cantina", "TAXI_VELES_CANTINA", 100)]
        VelesCantina = 8,
        [TaxiDestination(1, "Veles Residential District", "TAXI_VELES_RESIDENTIAL", 100)]
        VelesResidential = 9,
        [TaxiDestination(1, "Veles Apartments", "TAXI_VELES_APARTMENT", 100)]
        VelesApartments = 10,

        // Dantooine

        [TaxiDestination(2, "Dantooine Starport", "TAXI_DANTOOINE_STARPORT", 150)]
    DantooineStarport = 11,
        [TaxiDestination(2, "Dantooine Republic Garrison", "TAXI_DANTOOINE_GARRISON", 150)]
        DantooineGarrison = 12,
        [TaxiDestination(2, "Dantooine Monster Gym", "TAXI_DANTOOINE_GYM", 150)]
        DantooineGym = 13,
        [TaxiDestination(2, "Dantooine Medical", "TAXI_DANTOOINE_MEDICAL", 150)]
        DantooineMedical = 14,

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
