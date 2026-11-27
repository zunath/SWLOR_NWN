using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// How a city's properties nest, with no database or engine calls. A city holds structures. A building or house is a structure whose
    /// interior is a separate property: the city holds the building structure, and the structure holds the interior (a house, a bank).
    /// Plots, furniture and crafting devices placed inside a house are structures of the interior. So a plot in a house is three steps
    /// from its city: interior, building structure, city. The database is reached through the lookups passed in.
    /// </summary>
    public static class PropertyHierarchyRules
    {
        /// <summary>The most steps from a property to its city: interior, building structure, city, with one to spare.</summary>
        public const int MaxDepth = 4;

        /// <summary>
        /// The id of the city a property belongs to: the city itself, a structure on its land, a building's interior, or a structure inside
        /// that interior. Empty for a property outside any city, such as an apartment or a ship, and for an id that is not found.
        /// </summary>
        /// <param name="propertyId">The property to start from.</param>
        /// <param name="lookup">Finds a property by id, or null.</param>
        public static string FindCityId(string propertyId, System.Func<string, WorldProperty> lookup)
        {
            for (var depth = 0; depth < MaxDepth && !string.IsNullOrWhiteSpace(propertyId); depth++)
            {
                var property = lookup(propertyId);
                if (property == null)
                    return string.Empty;

                if (property.PropertyType == PropertyType.City)
                    return property.Id;

                propertyId = property.ParentPropertyId;
            }

            return string.Empty;
        }

        /// <summary>
        /// The types of structure a city has: those standing on its land, and the garden plots inside its houses, so a city whose farmers
        /// only garden indoors still counts as having gardens. A house is the interior of one of the city's building structures, and its
        /// plots are structures of that interior.
        /// </summary>
        /// <param name="cityChildren">The properties whose parent is the city.</param>
        /// <param name="childrenOf">Finds the properties whose parent is any of the ids.</param>
        public static IReadOnlyCollection<StructureType> GetStructureTypes(
            IReadOnlyCollection<WorldProperty> cityChildren,
            System.Func<IReadOnlyCollection<string>, IEnumerable<WorldProperty>> childrenOf)
        {
            var types = new HashSet<StructureType>();
            var buildings = cityChildren.Where(child => child.PropertyType == PropertyType.Structure).ToList();
            foreach (var building in buildings)
                types.Add(building.StructureType);

            if (types.Contains(StructureType.GardenPlot) || buildings.Count == 0)
                return types;

            var houseIds = childrenOf(buildings.Select(building => building.Id).ToList())
                .Where(interior => interior.PropertyType == PropertyType.House)
                .Select(house => house.Id)
                .ToList();
            if (houseIds.Count > 0 && childrenOf(houseIds).Any(plot => plot.StructureType == StructureType.GardenPlot))
                types.Add(StructureType.GardenPlot);

            return types;
        }
    }
}
