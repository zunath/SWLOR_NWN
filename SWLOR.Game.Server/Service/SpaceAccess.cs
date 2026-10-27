using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Where a hull may go by size (plan sections 7 and 12). The rules live in <see cref="ShipAccessRules"/>; this
    /// reads the area locals and the player's ships for them. Starport docking, undocking into a host's starport,
    /// starlane jumps, launches from a dock and undocking beside a host all ask here.
    /// </summary>
    public static partial class Space
    {
        /// <summary>
        /// Area local int. The largest hull the area admits, as a <see cref="ShipSize"/> value (2 = Medium: Small and
        /// Medium only). Unset means no limit. On a space area it gates starlane jumps, launches from a dock in it
        /// and undocking from a host in it; on a ship-only location it gates docking there.
        /// </summary>
        public const string MaxShipSizeVariable = "MAX_SHIP_SIZE";

        /// <summary>
        /// Area local int. Set to 1 on the landing area of each major port (Veles, Mon Cala, Anchorhead, Smuggler's
        /// Moon Station and Korriban), the only NPC docks that take Capital hulls.
        /// </summary>
        public const string CapitalDockVariable = "CAPITAL_DOCK";

        /// <summary>The largest hull an area admits, or <see cref="ShipSize.Invalid"/> when it has no limit.</summary>
        public static ShipSize GetAreaMaxShipSize(uint area)
        {
            if (!GetIsObjectValid(area))
                return ShipSize.Invalid;

            var value = GetLocalInt(area, MaxShipSizeVariable);
            return value is >= (int)ShipSize.Small and <= (int)ShipSize.Capital ? (ShipSize)value : ShipSize.Invalid;
        }

        /// <summary>The hull size of a registered ship.</summary>
        public static ShipSize GetShipSize(PlayerShip ship)
        {
            var detail = GetShipDetailByItemTag(ship.Status.ItemTag);
            return ShipDockingRules.ResolveSize(detail?.Size ?? ship.Status.Size, detail?.CapitalShip ?? ship.Status.CapitalShip);
        }

        /// <summary>
        /// Why the dock whose landing area is <paramref name="landingArea"/> cannot take the ship. Pass the
        /// starport's property id for a player starport, or an empty id for an NPC dock.
        /// </summary>
        public static StarportDockRefusal GetStarportRefusal(PlayerShip ship, uint landingArea, string playerStarportId)
        {
            var isPlayerStarport = !string.IsNullOrWhiteSpace(playerStarportId);
            return ShipAccessRules.GetStarportRefusal(new StarportDockRequest
            {
                ShipSize = GetShipSize(ship),
                IsPlayerStarport = isPlayerStarport,
                CapitalDock = !isPlayerStarport && GetIsObjectValid(landingArea) && GetLocalBool(landingArea, CapitalDockVariable),
                PlayerStarportLevel = isPlayerStarport ? GetPlayerStarportLevel(playerStarportId) : 0,
                AreaMaxSize = isPlayerStarport ? ShipSize.Invalid : GetAreaMaxShipSize(landingArea),
            });
        }

        /// <summary>Why the dock a ship is saved at (a <see cref="PropertyLocationType.DockPosition"/>) cannot take the ship.</summary>
        public static StarportDockRefusal GetStarportRefusal(PlayerShip ship, PropertyLocation dock)
        {
            var isNpcDock = !string.IsNullOrWhiteSpace(dock.AreaResref);
            return GetStarportRefusal(
                ship,
                isNpcDock ? Area.GetAreaByResref(dock.AreaResref) : OBJECT_INVALID,
                isNpcDock ? string.Empty : dock.InstancePropertyId);
        }

        /// <summary>Tells the pilot why a starport refused their ship. Returns false when it did not.</summary>
        public static bool TryRefuseStarport(uint player, StarportDockRefusal refusal, uint landingArea)
        {
            if (refusal == StarportDockRefusal.None)
                return false;

            SendMessageToPC(player, ColorToken.Red(ShipAccessRules.DescribeStarportRefusal(refusal, GetAreaMaxShipSize(landingArea))));
            return true;
        }

        /// <summary>
        /// Whether a ship may enter an area. Tells the pilot when it may not. Starlane jumps, launches from a dock and
        /// undocking beside a host all ask here; ships already inside an area too small for them are never ejected.
        /// </summary>
        public static bool CanShipEnterArea(uint player, PlayerShip ship, uint area)
        {
            var areaMax = GetAreaMaxShipSize(area);
            if (ShipAccessRules.AreaAdmits(GetShipSize(ship), areaMax))
                return true;

            SendMessageToPC(player, ColorToken.Red(ShipAccessRules.DescribeAreaRefusal(GetName(area), areaMax)));
            return false;
        }

        /// <summary>
        /// The starlane check: a pilot flying a ship may jump into <paramref name="area"/> only when the ship fits.
        /// Anyone not flying a ship (a ground teleporter) always passes.
        /// </summary>
        public static bool CanFlownShipEnterArea(uint pilot, uint area)
        {
            if (!TryGetFlownShip(pilot, out var ship))
                return true;

            return CanShipEnterArea(pilot, ship, area);
        }

        /// <summary>The effective starport upgrade level of the city a player starport belongs to.</summary>
        private static int GetPlayerStarportLevel(string starportPropertyId)
        {
            var starport = DB.Get<WorldProperty>(starportPropertyId);
            if (starport == null || string.IsNullOrWhiteSpace(starport.ParentPropertyId))
                return 0;

            var building = DB.Get<WorldProperty>(starport.ParentPropertyId);
            if (building == null || string.IsNullOrWhiteSpace(building.ParentPropertyId))
                return 0;

            return Property.GetEffectiveUpgradeLevel(building.ParentPropertyId, PropertyUpgradeType.StarportLevel);
        }
    }
}
