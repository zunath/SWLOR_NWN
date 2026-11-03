namespace SWLOR.Game.Server.Service.IndustryService
{
    /// <summary>
    /// The Planetary Depot (Player City Revamp, "Planetary industry"): a city Starport of level 2 or higher lets one of its citizens
    /// bring a holding standing on its planet from the NPC dock to the Starport, and from the Starport back to the dock, once every
    /// 7 days. It changes where the output waits, never how much. A station's Depot serves the holdings on its orbit's planet, and a
    /// Wild Space station has none. A Starport lost to the city is handled by the holdings service, which moves the holding to the
    /// NPC dock without using this limit.
    /// </summary>
    public static class PlanetaryDepotRules
    {
        public const int MinimumStarportLevel = 2;
        public const int CooldownDays = 7;

        public static DepotRefusal GetMoveRefusal(DepotMoveRequest request)
        {
            if (!request.OwnerOwnsHolding)
                return DepotRefusal.NotYourHolding;
            if (request.HoldingExchange != request.DockExchange)
                return DepotRefusal.WrongPlanet;
            if (string.Equals(request.HoldingDockId, request.DockId, StringComparison.Ordinal))
                return DepotRefusal.AlreadyHere;
            if (!request.DockIsCityStarport && !request.HoldingAtCityStarport)
                return DepotRefusal.NotACityMove;
            if (request.PaymentPending)
                return DepotRefusal.PaymentPending;

            if (request.DockIsCityStarport)
            {
                if (request.StarportLevel < MinimumStarportLevel)
                    return DepotRefusal.StarportTooLow;
                if (!request.OwnerIsCitizen)
                    return DepotRefusal.NotACitizen;
            }

            return GetNextMoveUtc(request.LastMoveUtc) > request.NowUtc ? DepotRefusal.TooSoon : DepotRefusal.None;
        }

        /// <summary>When a holding that last moved at <paramref name="lastMoveUtc"/> may move again.</summary>
        public static DateTime GetNextMoveUtc(DateTime lastMoveUtc)
        {
            return lastMoveUtc == default ? DateTime.MinValue : lastMoveUtc.AddDays(CooldownDays);
        }

        /// <summary>Whole days (rounded up) until the holding may move again, or 0.</summary>
        public static int GetDaysLeft(DateTime lastMoveUtc, DateTime nowUtc)
        {
            var left = GetNextMoveUtc(lastMoveUtc) - nowUtc;
            return left <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(left.TotalDays);
        }

        public static string GetRefusalMessage(DepotRefusal refusal, int daysLeft = 0)
        {
            return refusal switch
            {
                DepotRefusal.NotYourHolding => "That holding is not yours.",
                DepotRefusal.WrongPlanet => "This dock does not serve that holding's planet.",
                DepotRefusal.AlreadyHere => "The holding is already at this dock.",
                DepotRefusal.StarportTooLow => $"The Planetary Depot needs a Starport of level {MinimumStarportLevel}.",
                DepotRefusal.NotACitizen => "Only citizens of this city can use its Planetary Depot.",
                DepotRefusal.TooSoon => $"The holding moved recently. It can move again in {Math.Max(1, daysLeft)} day{(daysLeft > 1 ? "s" : string.Empty)}.",
                DepotRefusal.PaymentPending => "Wait for the holding's last payment to settle first.",
                DepotRefusal.NotACityMove => "The Planetary Depot moves holdings to or from a city Starport.",
                _ => string.Empty,
            };
        }
    }
}
