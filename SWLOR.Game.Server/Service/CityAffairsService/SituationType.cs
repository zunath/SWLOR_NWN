namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A kind of City Affairs situation. Values are saved with the situations and never change.
    /// </summary>
    public enum SituationType
    {
        Invalid = 0,

        DockWorkersStrike = 1,
        FestivalPetition = 2,
        SmugglersOffer = 3,
        BeastSighting = 4,

        /// <summary>Follows an ignored Beast Sighting.</summary>
        CaravanAttacked = 5,
        RefugeeShuttle = 6,

        /// <summary>Follows a welcomed Refugee Shuttle.</summary>
        NewArrivals = 7,
        MedicalSupplyShortage = 8,

        /// <summary>Needs the Open Gates Edict.</summary>
        VisitorBrawl = 9,

        /// <summary>Follows a Visitor Brawl.</summary>
        VisitorsComplain = 10,

        /// <summary>Follows a smuggler's bribe and exposes it.</summary>
        Scandal = 11
    }
}
