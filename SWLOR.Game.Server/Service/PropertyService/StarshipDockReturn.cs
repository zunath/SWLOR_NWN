using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// Keeps player starships recoverable across server restarts.
    /// A ship's safe dock is the last regular starport it docked at (LastNPCDockPosition and LastNPCSpacePosition).
    /// On restart, a ship docked in a ship-only location, at a player starport that no longer exists,
    /// or with no dock at all returns to the regular starport of the planet it is at, so a restart never moves a ship
    /// (or what it carries) to another system. Its safe dock is used when that dock is in the same system, or when the
    /// planet the ship is at cannot be resolved.
    /// </summary>
    public static class StarshipDockReturn
    {
        /// <summary>
        /// Area local int. Set to 1 on every area of a location players can only reach by player ship, such as a dungeon station.
        /// Docking there never becomes a ship's safe dock, and a ship left there returns to its safe dock when the server restarts.
        /// </summary>
        public const string ShipOnlyLocationVariable = "SHIP_ONLY_LOCATION";

        /// <summary>
        /// Determines whether an area is marked as reachable only by player ship.
        /// </summary>
        /// <param name="area">The area to check.</param>
        /// <returns>true if the area is a ship-only location, false otherwise.</returns>
        public static bool IsShipOnlyArea(uint area)
        {
            return GetIsObjectValid(area) && GetLocalBool(area, ShipOnlyLocationVariable);
        }

        /// <summary>
        /// Replaces a safe dock that was recorded in a ship-only location, which happened before the location was marked,
        /// with the starport of the planet that location belongs to.
        /// </summary>
        /// <param name="positions">The starship property's positions. Updated in place.</param>
        /// <param name="isShipOnlyArea">Whether the area with the given resref is a ship-only location.</param>
        /// <param name="getPlanetStarport">The landing and orbit of the starport serving the planet an area belongs to, or null if there is none.</param>
        /// <returns>true if the safe dock was replaced, false otherwise.</returns>
        public static bool RepairSafeDock(
            Dictionary<PropertyLocationType, PropertyLocation> positions,
            Func<string, bool> isShipOnlyArea,
            Func<string, (PropertyLocation Landing, PropertyLocation Orbit)?> getPlanetStarport)
        {
            if (!positions.TryGetValue(PropertyLocationType.LastNPCDockPosition, out var safeDock) ||
                !IsAreaLocation(safeDock) ||
                !isShipOnlyArea(safeDock.AreaResref))
            {
                return false;
            }

            return TrySetSafeDock(positions, AreaStarport(safeDock.AreaResref, getPlanetStarport));
        }

        /// <summary>
        /// Returns a starship to the regular starport of the planet it is at if its current dock cannot be relied on
        /// after a restart. The planet comes from its ship-only location, the caller's fallback area, or the orbit it
        /// docked from, in that order. Its safe dock is kept when it is in the same system (its starport shares the
        /// planet's landing or orbit area) or the planet is unknown, so a restart never carries a ship to another
        /// system. Ships docked at a regular starport or an existing player starport are left untouched.
        /// </summary>
        /// <param name="positions">The starship property's positions. Updated in place.</param>
        /// <param name="isShipOnlyArea">Whether the area with the given resref is a ship-only location.</param>
        /// <param name="starportExists">Whether the player starport with the given property Id still exists.</param>
        /// <param name="getPlanetStarport">
        /// The landing and orbit of the starport serving the planet an area belongs to, or null if there is none.
        /// Used when a ship that must return has no usable safe dock, and to find the orbit of a safe dock
        /// recorded before safe orbits were.
        /// </param>
        /// <param name="fallbackAreaResref">
        /// An area whose planet's starport receives a ship that must return, has no usable safe dock and is not in a
        /// ship-only location, such as the city area of a player starport being retrieved.
        /// </param>
        /// <param name="getOrbitStarport">
        /// The landing and orbit of the starport serving the planet a saved orbit belongs to, or null if there is none.
        /// One orbit area can hold several planets' orbits, so this resolves the orbit's position, not just its area.
        /// When omitted, the orbit's area is resolved with getPlanetStarport.
        /// </param>
        /// <returns>What happened to the ship's dock.</returns>
        public static StarshipDockReturnResult ReturnToSafeDock(
            Dictionary<PropertyLocationType, PropertyLocation> positions,
            Func<string, bool> isShipOnlyArea,
            Func<string, bool> starportExists,
            Func<string, (PropertyLocation Landing, PropertyLocation Orbit)?> getPlanetStarport,
            string fallbackAreaResref = "",
            Func<PropertyLocation, (PropertyLocation Landing, PropertyLocation Orbit)?> getOrbitStarport = null)
        {
            StarshipDockReturnResult result;
            var shipOnlyAreaResref = string.Empty;

            if (!positions.TryGetValue(PropertyLocationType.DockPosition, out var dock) || dock == null)
            {
                result = StarshipDockReturnResult.RestoredMissingDock;
            }
            else if (!string.IsNullOrWhiteSpace(dock.InstancePropertyId))
            {
                if (starportExists(dock.InstancePropertyId))
                    return StarshipDockReturnResult.Unchanged;

                result = StarshipDockReturnResult.ReturnedFromMissingStarport;
            }
            else if (isShipOnlyArea(dock.AreaResref))
            {
                result = StarshipDockReturnResult.ReturnedFromShipOnlyLocation;
                shipOnlyAreaResref = dock.AreaResref;
            }
            else
            {
                return StarshipDockReturnResult.Unchanged;
            }

            // The starport of the planet the ship is at: its ship-only location's, the caller's fallback area's
            // (a retrieved starport's city), or that of the orbit it docked from.
            getOrbitStarport ??= orbitLocation => getPlanetStarport(orbitLocation.AreaResref);
            var hereStarport = AreaStarport(shipOnlyAreaResref, getPlanetStarport) ??
                               AreaStarport(fallbackAreaResref, getPlanetStarport) ??
                               (positions.TryGetValue(PropertyLocationType.SpacePosition, out var orbit) && IsAreaLocation(orbit)
                                   ? ValidStarport(getOrbitStarport(orbit))
                                   : null);

            var hasSafeDock = positions.TryGetValue(PropertyLocationType.LastNPCDockPosition, out var safeDock) &&
                              IsAreaLocation(safeDock) &&
                              !isShipOnlyArea(safeDock.AreaResref);

            // A safe dock in another system would carry the ship (and its cargo) there, so the ship goes to the
            // starport of the planet it is at instead, which becomes its safe dock.
            if (hasSafeDock &&
                hereStarport != null &&
                !IsSameSystem(AreaStarport(safeDock.AreaResref, getPlanetStarport), hereStarport.Value) &&
                TrySetSafeDock(positions, hereStarport))
            {
                positions[PropertyLocationType.DockPosition] = Copy(positions[PropertyLocationType.LastNPCDockPosition]);
                positions[PropertyLocationType.SpacePosition] = Copy(positions[PropertyLocationType.LastNPCSpacePosition]);

                return result;
            }

            if (hasSafeDock)
            {
                positions[PropertyLocationType.DockPosition] = Copy(safeDock);

                // Ships that have not docked at a regular starport since safe orbits were recorded get the orbit of
                // their safe dock's planet, which is saved as the safe orbit. They keep their current orbit only if
                // that planet cannot be resolved.
                if (!positions.TryGetValue(PropertyLocationType.LastNPCSpacePosition, out var safeOrbit) ||
                    !IsAreaLocation(safeOrbit))
                {
                    safeOrbit = getPlanetStarport(safeDock.AreaResref) is { } safePlanet && IsAreaLocation(safePlanet.Orbit)
                        ? Copy(safePlanet.Orbit)
                        : null;

                    if (safeOrbit != null)
                        positions[PropertyLocationType.LastNPCSpacePosition] = safeOrbit;
                }

                if (safeOrbit != null)
                    positions[PropertyLocationType.SpacePosition] = Copy(safeOrbit);

                return result;
            }

            // A ship without a usable safe dock goes to the starport of the planet it is at, which becomes its safe
            // dock from now on.
            if (TrySetSafeDock(positions, hereStarport))
            {
                positions[PropertyLocationType.DockPosition] = Copy(positions[PropertyLocationType.LastNPCDockPosition]);
                positions[PropertyLocationType.SpacePosition] = Copy(positions[PropertyLocationType.LastNPCSpacePosition]);

                return StarshipDockReturnResult.ReturnedToPlanetStarport;
            }

            return StarshipDockReturnResult.NoSafeDock;
        }

        /// <summary>
        /// The planet a saved orbit belongs to: of the planets whose orbit waypoint is in the orbit's area, the one
        /// whose waypoint is nearest the orbit's position, since one orbit area can hold several planets' orbits.
        /// </summary>
        /// <param name="orbit">The orbit to look up, such as a ship's saved orbit.</param>
        /// <param name="planetOrbits">Each planet and the location of its orbit waypoint.</param>
        /// <returns>The planet, or PlanetType.Invalid if no planet's orbit is in that area.</returns>
        public static PlanetType FindOrbitPlanet(PropertyLocation orbit, IEnumerable<(PlanetType Planet, PropertyLocation OrbitWaypoint)> planetOrbits)
        {
            if (!IsAreaLocation(orbit))
                return PlanetType.Invalid;

            var nearestPlanet = PlanetType.Invalid;
            var nearestDistanceSquared = float.MaxValue;
            foreach (var (planet, waypoint) in planetOrbits)
            {
                if (!IsAreaLocation(waypoint) ||
                    !string.Equals(waypoint.AreaResref, orbit.AreaResref, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var x = waypoint.X - orbit.X;
                var y = waypoint.Y - orbit.Y;
                var z = waypoint.Z - orbit.Z;
                var distanceSquared = x * x + y * y + z * z;
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearestPlanet = planet;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearestPlanet;
        }

        private static bool TrySetSafeDock(
            Dictionary<PropertyLocationType, PropertyLocation> positions,
            (PropertyLocation Landing, PropertyLocation Orbit)? starport)
        {
            if (ValidStarport(starport) is not { } validStarport)
                return false;

            positions[PropertyLocationType.LastNPCDockPosition] = Copy(validStarport.Landing);
            positions[PropertyLocationType.LastNPCSpacePosition] = Copy(validStarport.Orbit);
            return true;
        }

        private static (PropertyLocation Landing, PropertyLocation Orbit)? AreaStarport(
            string areaResref,
            Func<string, (PropertyLocation Landing, PropertyLocation Orbit)?> getPlanetStarport)
        {
            return string.IsNullOrWhiteSpace(areaResref)
                ? null
                : ValidStarport(getPlanetStarport(areaResref));
        }

        private static (PropertyLocation Landing, PropertyLocation Orbit)? ValidStarport(
            (PropertyLocation Landing, PropertyLocation Orbit)? starport)
        {
            return starport is { } planetStarport &&
                   IsAreaLocation(planetStarport.Landing) &&
                   IsAreaLocation(planetStarport.Orbit)
                ? starport
                : null;
        }

        /// <summary>
        /// Whether a safe dock's starport is in the same system as the starport of the planet the ship is at: the
        /// same landing, or an orbit in the same area, which the ship reaches without a jump. A safe dock whose planet
        /// cannot be resolved matches nothing.
        /// </summary>
        private static bool IsSameSystem(
            (PropertyLocation Landing, PropertyLocation Orbit)? safeDockStarport,
            (PropertyLocation Landing, PropertyLocation Orbit) hereStarport)
        {
            return safeDockStarport is { } safeStarport &&
                   (string.Equals(safeStarport.Landing.AreaResref, hereStarport.Landing.AreaResref, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(safeStarport.Orbit.AreaResref, hereStarport.Orbit.AreaResref, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAreaLocation(PropertyLocation location)
        {
            return location != null && !string.IsNullOrWhiteSpace(location.AreaResref);
        }

        private static PropertyLocation Copy(PropertyLocation location)
        {
            return new PropertyLocation
            {
                AreaResref = location.AreaResref,
                InstancePropertyId = location.InstancePropertyId,
                X = location.X,
                Y = location.Y,
                Z = location.Z,
                Orientation = location.Orientation
            };
        }
    }
}
