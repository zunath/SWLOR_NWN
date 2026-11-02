using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The 7-day departure pass (Player City Revamp, "Who may use the Bank and Starport"): a character whose ship is docked at a city's
    /// Starport, or who arrived there by shuttle, can still enter it for 7 days to board the ship or take a flight out, and nothing
    /// else, after they lose the right to use it. The pass is kept per character and city on <see cref="Player.StarportDeparturePasses"/>
    /// and starts over whenever the character is seen with a ship docked there and allowed to use the city's facilities.
    /// </summary>
    public static class StarportDeparture
    {
        /// <summary>Whether the character holds a departure pass for the city that has not run out.</summary>
        public static bool HasPass(Player dbPlayer, string cityId, DateTime? nowUtc = null)
        {
            if (dbPlayer?.StarportDeparturePasses == null ||
                string.IsNullOrWhiteSpace(cityId) ||
                !dbPlayer.StarportDeparturePasses.TryGetValue(cityId, out var startedUtc))
            {
                return false;
            }

            return FacilityAccessRules.IsDeparturePassValid(startedUtc, nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>
        /// A ship has just docked at a player Starport. Its pilot and its owner start their departure pass for the city when they may
        /// use the city's facilities, so the pass stays a way out for someone who later loses access and never a way in for someone who
        /// never had it. Call it after the dock is saved. Arrivals by shuttle call it too, with no ship owner.
        /// </summary>
        /// <param name="pilotPlayerId">The player who docked, or who landed.</param>
        /// <param name="shipOwnerPlayerId">The ship's owner, or empty.</param>
        /// <param name="starportInteriorId">The property id of the Starport interior the ship docked at.</param>
        public static void OnArrived(string pilotPlayerId, string shipOwnerPlayerId, string starportInteriorId)
        {
            var city = Station.GetCityOfInterior(starportInteriorId);
            if (city == null)
                return;

            var now = DateTime.UtcNow;
            foreach (var playerId in new[] { pilotPlayerId, shipOwnerPlayerId }.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
            {
                var dbPlayer = DB.Get<Player>(playerId);
                if (dbPlayer == null || EdictEffects.GetFacilityAccess(dbPlayer, city, nowUtc: now) != FacilityAccessResult.Allowed)
                    continue;

                StartPass(dbPlayer, city.Id, now);
            }
        }

        /// <summary>
        /// The start-of-server check of a ship docked at a player Starport, judged by its owner (call it for every starship property
        /// before the dock safety check). An owner who may use the city's facilities starts their pass over. An owner who lost the
        /// right and whose pass has run out has the ship moved to its safe dock, the way a ship whose Starport is picked up is, and
        /// collects it there. An owner who never held a pass is left alone: docking stays open to everyone, as it has been.
        /// </summary>
        public static void ReturnStrandedShip(WorldProperty starship, DateTime? nowUtc = null)
        {
            if (starship == null ||
                string.IsNullOrWhiteSpace(starship.OwnerPlayerId) ||
                !starship.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock) ||
                string.IsNullOrWhiteSpace(dock.InstancePropertyId))
            {
                return;
            }

            var interior = DB.Get<WorldProperty>(dock.InstancePropertyId);
            var city = interior == null ? null : Station.GetCityOfInterior(interior.Id);
            var owner = DB.Get<Player>(starship.OwnerPlayerId);
            if (city == null || owner == null)
                return;

            var now = nowUtc ?? DateTime.UtcNow;
            DateTime? passStarted = owner.StarportDeparturePasses != null && owner.StarportDeparturePasses.TryGetValue(city.Id, out var started)
                ? started
                : null;
            var canUse = EdictEffects.GetFacilityAccess(owner, city, nowUtc: now) == FacilityAccessResult.Allowed;

            switch (FacilityAccessRules.GetDockedShipAction(canUse, passStarted, now))
            {
                case DockedShipAction.RefreshPass:
                    StartPass(owner, city.Id, now);
                    break;
                case DockedShipAction.ReturnToSafeDock:
                    ReturnToSafeDock(starship, interior, city);
                    break;
            }
        }

        private static void StartPass(Player dbPlayer, string cityId, DateTime nowUtc)
        {
            dbPlayer.StarportDeparturePasses ??= new Dictionary<string, DateTime>();
            dbPlayer.StarportDeparturePasses[cityId] = nowUtc;
            DB.Set(dbPlayer);
        }

        /// <summary>Moves the ship out of the Starport. The caller saves the ship; the Starport's list is saved here.</summary>
        private static void ReturnToSafeDock(WorldProperty starship, WorldProperty interior, WorldProperty city)
        {
            if (starship.ChildPropertyIds.TryGetValue(PropertyChildType.RegisteredStarport, out var registered))
                registered.Remove(interior.Id);

            if (interior.ChildPropertyIds.TryGetValue(PropertyChildType.Starship, out var docked) && docked.Remove(starship.Id))
                DB.Set(interior);

            Property.ReturnStarshipToSafeDock(starship, interior.Id, Station.GetCityPlanetAreaResref(city));
            Log.Write(
                LogGroup.Property,
                $"Starship '{starship.CustomName}' ({starship.Id}) was moved out of the Starport of city '{city.CustomName}' ({city.Id}): its owner lost access to the city's facilities and their {FacilityAccessRules.DeparturePassDays}-day departure pass ran out.");
        }
    }
}
