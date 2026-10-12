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
    /// Lists the blueprints the DM creator can spawn and creates objects from them.
    /// Names are read from the templates a chunk at a time the first time a category is opened,
    /// so the first open never stalls the server.
    /// </summary>
    public static class DMBlueprintCatalog
    {
        private const int TemplatesPerChunk = 200;
        private const float ChunkDelaySeconds = 0.1f;

        private class CategoryState
        {
            public List<(string Resref, DMBlueprintSource Source)> Pending { get; set; } = new();
            public List<DMBlueprint> Blueprints { get; set; } = new();
            public int Resolved { get; set; }
            public bool IsLoading { get; set; }
            public bool IsLoaded { get; set; }
            public List<Action> OnLoaded { get; } = new();
        }

        private static readonly Dictionary<DMBlueprintCategory, CategoryState> _states = new();

        /// <summary>
        /// Returns true once every blueprint name in the category has been read.
        /// </summary>
        public static bool IsLoaded(DMBlueprintCategory category)
        {
            return _states.TryGetValue(category, out var state) && state.IsLoaded;
        }

        /// <summary>
        /// Returns how many of the category's templates have been read so far.
        /// </summary>
        public static (int Resolved, int Total) GetLoadProgress(DMBlueprintCategory category)
        {
            return _states.TryGetValue(category, out var state)
                ? (state.Resolved, state.Pending.Count)
                : (0, 0);
        }

        /// <summary>
        /// Returns the category's blueprints sorted by name, or an empty list while it is still loading.
        /// </summary>
        public static IReadOnlyList<DMBlueprint> GetBlueprints(DMBlueprintCategory category)
        {
            return _states.TryGetValue(category, out var state) && state.IsLoaded
                ? state.Blueprints
                : new List<DMBlueprint>();
        }

        /// <summary>
        /// Starts reading the category's templates if needed and runs the action once they are all read.
        /// The action runs right away when the category is already loaded.
        /// </summary>
        /// <param name="category">The category to load.</param>
        /// <param name="onLoaded">Runs after loading finishes.</param>
        public static void Load(DMBlueprintCategory category, Action onLoaded)
        {
            if (!_states.TryGetValue(category, out var state))
            {
                state = new CategoryState();
                _states[category] = state;
            }

            if (state.IsLoaded)
            {
                onLoaded();
                return;
            }

            state.OnLoaded.Add(onLoaded);
            if (state.IsLoading)
                return;

            state.IsLoading = true;
            var resrefType = GetResRefType(category);
            var custom = ListResrefs(resrefType, true);
            var standard = ListResrefs(resrefType, false)
                .Where(resref => !custom.Contains(resref));

            state.Pending = custom
                .Select(resref => (resref, DMBlueprintSource.Custom))
                .Concat(standard.Select(resref => (resref, DMBlueprintSource.Standard)))
                .ToList();

            ReadNextChunk(category);
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

        private static void ReadNextChunk(DMBlueprintCategory category)
        {
            var state = _states[category];
            var end = Math.Min(state.Resolved + TemplatesPerChunk, state.Pending.Count);
            for (var index = state.Resolved; index < end; index++)
            {
                var (resref, source) = state.Pending[index];
                state.Blueprints.Add(ReadBlueprint(resref, category, source));
            }

            state.Resolved = end;
            if (end < state.Pending.Count)
            {
                AssignCommand(GetModule(), () => DelayCommand(ChunkDelaySeconds, () => ReadNextChunk(category)));
                return;
            }

            state.Blueprints = state.Blueprints
                .OrderBy(blueprint => blueprint.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(blueprint => blueprint.Resref, StringComparer.OrdinalIgnoreCase)
                .ToList();
            state.IsLoading = false;
            state.IsLoaded = true;

            var callbacks = state.OnLoaded.ToList();
            state.OnLoaded.Clear();
            foreach (var callback in callbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception exception)
                {
                    Log.Write(LogGroup.Error, $"DM creator refresh failed after loading {category}: {exception}");
                }
            }
        }

        private static HashSet<string> ListResrefs(ResRefType type, bool customOnly)
        {
            var resrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var resref = UtilPlugin.GetFirstResRef(type, string.Empty, customOnly);
                 !string.IsNullOrEmpty(resref);
                 resref = UtilPlugin.GetNextResRef())
            {
                resrefs.Add(resref);
            }

            return resrefs;
        }

        private static DMBlueprint ReadBlueprint(string resref, DMBlueprintCategory category, DMBlueprintSource source)
        {
            var template = TemplateToJson(resref, GetResType(category));
            if (JsonGetType(template) != JsonType.Object)
                return new DMBlueprint(resref, resref, string.Empty, category, source);

            var name = category switch
            {
                DMBlueprintCategory.Creatures =>
                    $"{ReadLocString(template, "FirstName")} {ReadLocString(template, "LastName")}".Trim(),
                DMBlueprintCategory.Items => ReadLocString(template, "LocalizedName"),
                DMBlueprintCategory.Waypoints => ReadLocString(template, "LocalizedName"),
                _ => ReadLocString(template, "LocName")
            };

            name = UtilPlugin.StripColors(name).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = resref;

            return new DMBlueprint(resref, name, ReadString(template, "Tag"), category, source);
        }

        private static string ReadString(Json template, string field)
        {
            var value = JsonObjectGet(JsonObjectGet(template, field), "value");
            return JsonGetType(value) == JsonType.String
                ? JsonGetString(value)
                : string.Empty;
        }

        // Localized strings hold text per language and/or a TLK string reference. TemplateToJson
        // nests the reference in "value" as "id"; GFF JSON tools put it beside "value".
        private static string ReadLocString(Json template, string field)
        {
            var locString = JsonObjectGet(template, field);
            if (JsonGetType(locString) != JsonType.Object)
                return string.Empty;

            var value = JsonObjectGet(locString, "value");
            if (JsonGetType(value) == JsonType.Object)
            {
                var keys = JsonObjectKeys(value);
                for (var index = 0; index < JsonGetLength(keys); index++)
                {
                    var key = JsonGetString(JsonArrayGet(keys, index));
                    if (key == "id")
                        continue;

                    var text = JsonGetString(JsonObjectGet(value, key));
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }

            foreach (var id in new[] { JsonObjectGet(value, "id"), JsonObjectGet(locString, "id") })
            {
                if (JsonGetType(id) != JsonType.Integer)
                    continue;

                var strRef = JsonGetInt(id);
                if (strRef > 0)
                    return GetStringByStrRef(strRef);
            }

            return string.Empty;
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

        private static ObjectType GetObjectType(DMBlueprintCategory category)
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
