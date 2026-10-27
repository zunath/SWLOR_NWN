namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Where a hull may go by size (plan sections 7 and 12): the docking limits at starports and the Small/Medium-only
    /// areas. Docking, undocking into a host's starport and the areas' entry points all ask these rules, so a size
    /// is never judged anywhere else. A ship already docked or flying somewhere too small for it stays until it leaves.
    /// </summary>
    public static class ShipAccessRules
    {
        /// <summary>The largest hull a player starport takes before it is upgraded.</summary>
        public const ShipSize PlayerStarportBaseSize = ShipSize.Large;

        /// <summary>The effective starport upgrade level a player starport needs to take Capital hulls.</summary>
        public const int CapitalStarportLevel = 2;

        /// <summary>The major ports that take Capital hulls: their dock areas carry the <c>CAPITAL_DOCK</c> area local.</summary>
        public const string CapitalPortNames = "Veles, Mon Cala, Anchorhead, Smuggler's Moon Station and Korriban";

        /// <summary>Whether a hull of this size fits in an area with this <c>MAX_SHIP_SIZE</c> (Invalid: no limit).</summary>
        public static bool AreaAdmits(ShipSize shipSize, ShipSize areaMax)
        {
            if (areaMax == ShipSize.Invalid)
                return true;

            return shipSize != ShipSize.Invalid && (int)shipSize <= (int)areaMax;
        }

        /// <summary>Why a starport cannot take the ship, or <see cref="StarportDockRefusal.None"/>.</summary>
        public static StarportDockRefusal GetStarportRefusal(StarportDockRequest request)
        {
            if (request.ShipSize == ShipSize.Invalid)
                return StarportDockRefusal.UnknownSize;
            if (!AreaAdmits(request.ShipSize, request.AreaMaxSize))
                return StarportDockRefusal.AreaSizeLimit;
            if ((int)request.ShipSize <= (int)PlayerStarportBaseSize)
                return StarportDockRefusal.None;

            // Only Capital hulls are left: they need a major port or a level 2 player starport.
            if (request.IsPlayerStarport)
                return request.PlayerStarportLevel >= CapitalStarportLevel
                    ? StarportDockRefusal.None
                    : StarportDockRefusal.PlayerStarportTooSmall;

            return request.CapitalDock ? StarportDockRefusal.None : StarportDockRefusal.NpcDockTooSmall;
        }

        /// <summary>The message a pilot sees when a starport refuses the ship.</summary>
        public static string DescribeStarportRefusal(StarportDockRefusal refusal, ShipSize areaMax) => refusal switch
        {
            StarportDockRefusal.UnknownSize => "This ship has no hull size, so no starport can take it. Notify an admin.",
            StarportDockRefusal.AreaSizeLimit => $"Only {DescribeSizeLimit(areaMax)} ships can dock here.",
            StarportDockRefusal.NpcDockTooSmall => $"This dock cannot take a Capital ship. Capital ships dock only at {CapitalPortNames}.",
            StarportDockRefusal.PlayerStarportTooSmall => $"This starport cannot take a Capital ship. Its city's starport must reach level {CapitalStarportLevel} (upgrade it from Manage City).",
            _ => string.Empty,
        };

        /// <summary>The message a pilot sees when an area turns their ship away.</summary>
        public static string DescribeAreaRefusal(string areaName, ShipSize areaMax) =>
            $"Your ship is too large for {areaName}. Only {DescribeSizeLimit(areaMax)} ships can enter.";

        /// <summary>The hulls an area with this <c>MAX_SHIP_SIZE</c> admits, as a phrase: "Small and Medium".</summary>
        public static string DescribeSizeLimit(ShipSize areaMax) => areaMax switch
        {
            ShipSize.Small => "Small",
            ShipSize.Medium => "Small and Medium",
            ShipSize.Large => "Small, Medium and Large",
            _ => "any",
        };
    }
}
