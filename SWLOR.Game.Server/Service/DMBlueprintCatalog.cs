using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Lists the blueprints the DM creator can spawn, grouped by the same palettes and categories as the
    /// native creator, and creates objects from them.
    /// Custom palettes are the module's *palcus files; Standard palettes are the base game's *palstd files.
    /// </summary>
    public static class DMBlueprintCatalog
    {
        public const string NotInPaletteCategory = "Not in palette";

        private static readonly Dictionary<(DMBlueprintCategory, DMBlueprintSource), DMPalette> _palettes = new();

        /// <summary>
        /// Returns a palette, reading it the first time it is asked for.
        /// The custom palette also lists module blueprints that no palette category holds.
        /// </summary>
        public static DMPalette GetPalette(DMBlueprintCategory category, DMBlueprintSource source)
        {
            if (_palettes.TryGetValue((category, source), out var palette))
                return palette;

            palette = ReadPalette(category, source);
            if (source == DMBlueprintSource.Custom)
                palette = AddUnlistedBlueprints(palette, category);

            _palettes[(category, source)] = palette;
            Log.Write(LogGroup.DM, $"Loaded the {source} {category} palette: {palette.Blueprints.Count} blueprints in {palette.CategoryPaths.Count} categories.");
            return palette;
        }

        /// <summary>
        /// Reads a blueprint's template, or returns a JSON null when it cannot be read.
        /// </summary>
        public static Json ReadTemplate(DMBlueprint blueprint)
        {
            return TemplateToJson(blueprint.Resref, GetResType(blueprint.Category));
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
            var created = CreateObject(GetObjectType(blueprint.Category), blueprint.Resref, location);
            if (!GetIsObjectValid(created))
                return OBJECT_INVALID;

            if (blueprint.Category == DMBlueprintCategory.Creatures)
            {
                DMActions.MakeInventoryUndroppable(created);
                Spawn.PrepareDMSpawnedCreature(created);
            }
            else if (blueprint.Category == DMBlueprintCategory.Placeables)
            {
                DMActions.MarkDMSpawnedPlaceable(dm, created);
            }

            Log.Write(LogGroup.DM,
                $"{DMPresence.DescribeDM(dm)} created {blueprint.Category} '{blueprint.Resref}' in {GetResRef(GetAreaFromLocation(location))}.");
            return created;
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
            if (blueprint.Category != DMBlueprintCategory.Items)
                return OBJECT_INVALID;

            var created = CreateItemOnObject(blueprint.Resref, container);
            if (!GetIsObjectValid(created))
                return OBJECT_INVALID;

            Log.Write(LogGroup.DM,
                $"{DMPresence.DescribeDM(dm)} created item '{blueprint.Resref}' in the inventory of {GetName(container)} ({GetObjectUUID(container)}).");
            return created;
        }

        private static DMPalette ReadPalette(DMBlueprintCategory category, DMBlueprintSource source)
        {
            var resref = GetPaletteResref(category, source);
            var template = TemplateToJson(resref, ResType.ITP);
            if (JsonGetType(template) != JsonType.Object)
            {
                Log.Write(LogGroup.Error, $"DM creator could not read palette '{resref}'.");
                return new DMPalette(new List<string>(), new List<DMBlueprint>());
            }

            return DMPaletteReader.Read(JsonDump(template), category, source, strRef => GetStringByStrRef(strRef));
        }

        // Blueprints added after the palette was last rebuilt in the toolset are in no category.
        private static DMPalette AddUnlistedBlueprints(DMPalette palette, DMBlueprintCategory category)
        {
            var listed = new HashSet<string>(palette.Blueprints.Select(blueprint => blueprint.Resref), StringComparer.OrdinalIgnoreCase);
            var unlisted = new List<DMBlueprint>();
            for (var resref = UtilPlugin.GetFirstResRef(GetResRefType(category), string.Empty, true);
                 !string.IsNullOrEmpty(resref);
                 resref = UtilPlugin.GetNextResRef())
            {
                if (listed.Add(resref))
                    unlisted.Add(new DMBlueprint(resref, resref, NotInPaletteCategory, category, DMBlueprintSource.Custom));
            }

            if (unlisted.Count == 0)
                return palette;

            var named = unlisted
                .Select(blueprint => new DMBlueprint(blueprint.Resref, ReadTemplateName(blueprint), NotInPaletteCategory, category, DMBlueprintSource.Custom))
                .OrderBy(blueprint => blueprint.Name, StringComparer.OrdinalIgnoreCase);
            return new DMPalette(
                palette.CategoryPaths.Append(NotInPaletteCategory).ToList(),
                palette.Blueprints.Concat(named).ToList());
        }

        private static string ReadTemplateName(DMBlueprint blueprint)
        {
            var template = ReadTemplate(blueprint);
            if (JsonGetType(template) != JsonType.Object)
                return blueprint.Resref;

            var name = blueprint.Category switch
            {
                DMBlueprintCategory.Creatures =>
                    $"{DMTemplateFields.ReadLocString(template, "FirstName")} {DMTemplateFields.ReadLocString(template, "LastName")}".Trim(),
                DMBlueprintCategory.Items => DMTemplateFields.ReadLocString(template, "LocalizedName"),
                DMBlueprintCategory.Waypoints => DMTemplateFields.ReadLocString(template, "LocalizedName"),
                _ => DMTemplateFields.ReadLocString(template, "LocName")
            };

            name = UtilPlugin.StripColors(name).Trim();
            return string.IsNullOrWhiteSpace(name) ? blueprint.Resref : name;
        }

        private static string GetPaletteResref(DMBlueprintCategory category, DMBlueprintSource source)
        {
            var prefix = category switch
            {
                DMBlueprintCategory.Creatures => "creature",
                DMBlueprintCategory.Placeables => "placeable",
                DMBlueprintCategory.Items => "item",
                DMBlueprintCategory.Stores => "store",
                DMBlueprintCategory.Waypoints => "waypoint",
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };

            // The haks override the *pal skeletons, so Standard must read *palstd to get the base game's palette.
            return prefix + (source == DMBlueprintSource.Custom ? "palcus" : "palstd");
        }

        private static ResRefType GetResRefType(DMBlueprintCategory category)
        {
            return category switch
            {
                DMBlueprintCategory.Creatures => ResRefType.Creature,
                DMBlueprintCategory.Placeables => ResRefType.Placeable,
                DMBlueprintCategory.Items => ResRefType.Item,
                DMBlueprintCategory.Stores => ResRefType.Store,
                DMBlueprintCategory.Waypoints => ResRefType.Waypoint,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }

        private static ResType GetResType(DMBlueprintCategory category)
        {
            return category switch
            {
                DMBlueprintCategory.Creatures => ResType.UTC,
                DMBlueprintCategory.Placeables => ResType.UTP,
                DMBlueprintCategory.Items => ResType.UTI,
                DMBlueprintCategory.Stores => ResType.UTM,
                DMBlueprintCategory.Waypoints => ResType.UTW,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }

        /// <summary>
        /// Returns the kind of object a blueprint category creates.
        /// </summary>
        public static ObjectType GetObjectType(DMBlueprintCategory category)
        {
            return category switch
            {
                DMBlueprintCategory.Creatures => ObjectType.Creature,
                DMBlueprintCategory.Placeables => ObjectType.Placeable,
                DMBlueprintCategory.Items => ObjectType.Item,
                DMBlueprintCategory.Stores => ObjectType.Store,
                DMBlueprintCategory.Waypoints => ObjectType.Waypoint,
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
            };
        }
    }
}
