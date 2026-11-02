namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// A short-lived effect a situation puts on a city. Values are saved on the city and never change.
    /// </summary>
    public enum CityModifierType
    {
        Invalid = 0,

        /// <summary>Citizens earn 5% more RP XP.</summary>
        FestivalXpBonus = 1,

        /// <summary>Transportation tax income is halved.</summary>
        TransportationIncomeHalved = 2,

        /// <summary>The Medical Center bonus is halved.</summary>
        MedicalCenterBonusHalved = 3,

        /// <summary>The Open Gates Edict is suspended.</summary>
        OpenGatesSuspended = 4,

        /// <summary>Crops harvested in the city yield 1 less, never below 1.</summary>
        CropPests = 5
    }
}
