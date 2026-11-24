namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// What a situation's outcome does to the planetary holding it is about, gathered while its effects are applied and saved
    /// with the outcome on the city (<see cref="Entity.CitySituationOutcome"/>) before the holding is changed.
    /// </summary>
    public sealed class CityHoldingEffects
    {
        /// <summary>The city drives the raiders off the holding.</summary>
        public bool Help { get; set; }

        /// <summary>The credits of the holding's customs fine the treasury paid off.</summary>
        public int FineCredits { get; set; }
    }
}
