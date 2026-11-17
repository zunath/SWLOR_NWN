using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Placing a structure: the item use handler that checks the limits and permissions of a property before it creates the
    /// structure or building. Split from <c>PropertyService</c> (P5-9b); the members moved unchanged.
    /// </summary>
    public interface IPropertyStructurePlacementService
    {
        /// <summary>
        /// Retrieves the structure type from an item.
        /// Item's resref must start with 'structure_' and end with 4 numbers.
        /// I.E: 'structure_0004'
        /// Returns StructureType.Invalid on error.
        /// </summary>
        /// <param name="item">The item to retrieve from.</param>
        /// <returns>A structure type associated with the item.</returns>
        StructureType GetStructureTypeFromItem(uint item);

        /// <summary>
        /// Before an item is used, if it is a structure item, place it at the specified location.
        /// </summary>
        void PlaceStructure();
    }
}
