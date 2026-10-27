using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// Lists the blueprints the DM creator can spawn, grouped by the same palettes and categories as the
    /// native creator, and creates objects from them.
    /// Custom palettes are the module's *palcus files; Standard palettes are the base game's *palstd files.
    /// The static facade <c>Service.DMBlueprintCatalog</c> forwards to the container's instance of this interface and
    /// keeps the event handler.
    /// </summary>
    public interface IDMBlueprintCatalogService
    {
        /// <summary>
        /// Reads the custom palettes while the module loads. Listing the blueprints no palette holds reads each
        /// one's template for its name, hundreds for items, which would stall the server inside a DM's click.
        /// </summary>
        void LoadCustomPalettes();

        /// <summary>
        /// Returns a palette, reading it the first time it is asked for.
        /// The custom palette also lists module blueprints that no palette category holds.
        /// </summary>
        DMPalette GetPalette(DMBlueprintCategory category, DMBlueprintSource source);

        /// <summary>
        /// Reads a blueprint's template, or returns a JSON null when it cannot be read.
        /// </summary>
        Json ReadTemplate(DMBlueprint blueprint);

        /// <summary>
        /// Creates an object from a blueprint at a location and applies the same setup as the native DM creator.
        /// </summary>
        /// <param name="dm">The DM creating the object.</param>
        /// <param name="blueprint">The blueprint to create.</param>
        /// <param name="location">Where to create it.</param>
        /// <returns>The created object, or OBJECT_INVALID if it could not be created.</returns>
        uint CreateAtLocation(uint dm, DMBlueprint blueprint, Location location);

        /// <summary>
        /// Creates an item from a blueprint inside a creature's or placeable's inventory.
        /// </summary>
        /// <param name="dm">The DM creating the item.</param>
        /// <param name="blueprint">An item blueprint.</param>
        /// <param name="container">The object that receives the item.</param>
        /// <returns>The created item, or OBJECT_INVALID if it could not be created.</returns>
        uint CreateInInventory(uint dm, DMBlueprint blueprint, uint container);

        /// <summary>
        /// Returns whether a created item can be put in an object's inventory: a creature, a store, or a placeable or
        /// item that is a container.
        /// </summary>
        bool CanReceiveItems(uint target);

        /// <summary>
        /// Returns the width of the square a trigger or encounter blueprint is created as, in meters.
        /// </summary>
        float GetSquareSize(DMBlueprintCategory category);

        /// <summary>
        /// Returns the kind of object a blueprint category creates.
        /// </summary>
        ObjectType GetObjectType(DMBlueprintCategory category);
    }
}
