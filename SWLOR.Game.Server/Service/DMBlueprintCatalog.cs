using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API for listing the blueprints the DM creator can spawn, grouped by the same palettes and categories
    /// as the native creator, and for creating objects from them.
    /// Custom palettes are the module's *palcus files; Standard palettes are the base game's *palstd files.
    /// The logic and state live in <see cref="IDMBlueprintCatalogService"/>; every member here forwards to the
    /// container's instance, and the event handler stays here so its name in the handler order manifest does not change.
    /// </summary>
    public static class DMBlueprintCatalog
    {
        // A compile-time constant: callers get the value inlined, as before.
        public const string NotInPaletteCategory = Domain.Administration.DMBlueprintCatalogService.NotInPaletteCategory;

        private static IDMBlueprintCatalogService Instance => FacadeAccessor.Resolve<IDMBlueprintCatalogService>();

        /// <summary>
        /// Reads the custom palettes while the module loads. Listing the blueprints no palette holds reads each
        /// one's template for its name, hundreds for items, which would stall the server inside a DM's click.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void LoadCustomPalettes()
        {
            Instance.LoadCustomPalettes();
        }

        /// <summary>
        /// Returns a palette, reading it the first time it is asked for.
        /// The custom palette also lists module blueprints that no palette category holds.
        /// </summary>
        public static DMPalette GetPalette(DMBlueprintCategory category, DMBlueprintSource source)
        {
            return Instance.GetPalette(category, source);
        }

        /// <summary>
        /// Reads a blueprint's template, or returns a JSON null when it cannot be read.
        /// </summary>
        public static Json ReadTemplate(DMBlueprint blueprint)
        {
            return Instance.ReadTemplate(blueprint);
        }

        /// <summary>
        /// Creates an object from a blueprint at a location and applies the same setup as the native DM creator.
        /// </summary>
        /// <param name="dm">The DM creating the object.</param>
        /// <param name="blueprint">The blueprint to create.</param>
        /// <param name="location">Where to create it.</param>
        /// <returns>The created object, or OBJECT_INVALID if it could not be created.</returns>
        public static uint CreateAtLocation(uint dm, DMBlueprint blueprint, Location location)
        {
            return Instance.CreateAtLocation(dm, blueprint, location);
        }

        /// <summary>
        /// Creates an item from a blueprint inside a creature's or placeable's inventory.
        /// </summary>
        /// <param name="dm">The DM creating the item.</param>
        /// <param name="blueprint">An item blueprint.</param>
        /// <param name="container">The object that receives the item.</param>
        /// <returns>The created item, or OBJECT_INVALID if it could not be created.</returns>
        public static uint CreateInInventory(uint dm, DMBlueprint blueprint, uint container)
        {
            return Instance.CreateInInventory(dm, blueprint, container);
        }

        /// <summary>
        /// Returns whether a created item can be put in an object's inventory: a creature, a store, or a placeable or
        /// item that is a container.
        /// </summary>
        public static bool CanReceiveItems(uint target)
        {
            return Instance.CanReceiveItems(target);
        }

        /// <summary>
        /// Returns the width of the square a trigger or encounter blueprint is created as, in meters.
        /// </summary>
        public static float GetSquareSize(DMBlueprintCategory category)
        {
            return Instance.GetSquareSize(category);
        }

        /// <summary>
        /// Returns the kind of object a blueprint category creates.
        /// </summary>
        public static ObjectType GetObjectType(DMBlueprintCategory category)
        {
            return Instance.GetObjectType(category);
        }
    }
}
