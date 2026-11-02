using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// The Industrial Charter Edict (Player City Revamp, "Planetary industry"): a citizen in good standing has half the setback
    /// chance and needs half the repair goods on a holding that stands on the city's planet, for as long as the Edict is in
    /// effect. Boom, raid and customs chances do not change, and neither does output. A station applies it to its orbit's planet,
    /// and a Wild Space station has no planet.
    /// </summary>
    public static class IndustrialCharterRules
    {
        public const double SetbackChanceScale = 0.5;
        public const double RepairUnitsScale = 0.5;

        public static readonly HoldingModifiers Modifiers = new()
        {
            SetbackChanceScale = SetbackChanceScale,
            RepairUnitsScale = RepairUnitsScale,
        };

        /// <summary>Whether a holding of this exchange's planet stands on the planet the city serves.</summary>
        public static bool IsOnCityPlanet(TradeExchangeType holdingExchange, TradeExchangeType cityExchange, bool isWildSpaceStation)
        {
            return !isWildSpaceStation && cityExchange != TradeExchangeType.Invalid && holdingExchange == cityExchange;
        }

        /// <summary>The modifiers one holding is advanced under: the Charter's when all three conditions hold, otherwise none.</summary>
        public static HoldingModifiers GetModifiers(bool charterInEffect, bool ownerIsCitizenInGoodStanding, bool onCityPlanet)
        {
            return charterInEffect && ownerIsCitizenInGoodStanding && onCityPlanet ? Modifiers : HoldingModifiers.None;
        }
    }
}
