using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A short-lived effect a situation put on the city. A repeat refreshes the expiry instead of stacking. Other
    /// systems read it through <see cref="SituationRules.IsModifierActive"/>.
    /// </summary>
    public class CityModifier
    {
        public DateTime ExpiresUtc { get; set; }

        /// <summary>The situation that applied it.</summary>
        public SituationType Source { get; set; }
    }
}
