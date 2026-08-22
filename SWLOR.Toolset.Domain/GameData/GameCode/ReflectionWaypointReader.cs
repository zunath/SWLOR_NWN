using System.Reflection;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.TaxiService;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    internal static class ReflectionWaypointReader
    {
        public static IReadOnlyList<WaypointDestinationInfo> ReadPlanetLandings() =>
            ReadPlanets(attribute => attribute.LandingWaypointTag);

        public static IReadOnlyList<WaypointDestinationInfo> ReadPlanetOrbits() =>
            ReadPlanets(attribute => attribute.SpaceOrbitWaypointTag);

        public static IReadOnlyList<TaxiDestinationInfo> ReadTaxiDestinations()
        {
            return Enum.GetValues<TaxiDestinationType>()
                .Select(value => typeof(TaxiDestinationType).GetMember(value.ToString()).Single())
                .Select(member => member.GetCustomAttribute<TaxiDestinationAttribute>())
                .Where(attribute => attribute != null && !string.IsNullOrWhiteSpace(attribute.WaypointTag))
                .Select(attribute => new TaxiDestinationInfo(
                    attribute!.WaypointTag,
                    attribute.Name,
                    attribute.RegionId,
                    attribute.Price))
                .OrderBy(destination => destination.RegionId)
                .ThenBy(destination => destination.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IReadOnlyList<WaypointDestinationInfo> ReadPlanets(
            Func<PlanetAttribute, string> selectTag)
        {
            return Enum.GetValues<PlanetType>()
                .Select(value => typeof(PlanetType).GetMember(value.ToString()).Single())
                .Select(member => member.GetCustomAttribute<PlanetAttribute>())
                .Where(attribute => attribute != null)
                .Select(attribute => new WaypointDestinationInfo(selectTag(attribute!), attribute!.Name))
                .Where(destination => !string.IsNullOrWhiteSpace(destination.Tag))
                .OrderBy(destination => destination.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
