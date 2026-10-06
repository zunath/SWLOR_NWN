using Nwn.Authoring.Appearances;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Supplies SWLOR's game-data services and rules to the shared <see cref="ModelReferenceResolver"/>:
    /// its lookup services as 2DA rows, extended (above-byte) item part numbers, the loot bag stand-in
    /// for an item with no model, and creature wings and tails.
    /// </summary>
    internal sealed class SwlorModelResolutionHost : IModelResolutionHost
    {
        private const string LootBagModel = "it_bag";

        private readonly AppearanceService? _appearances;
        private readonly PlaceableAppearanceService? _placeables;
        private readonly DoorTypeService? _doors;
        private readonly WaypointAppearanceService? _waypoints;
        private readonly Func<int, BaseItemIconRow?>? _baseItems;
        private readonly Func<string, JsonGffStruct?>? _itemBlueprintLoader;
        private readonly Func<string, bool>? _partModelExists;
        private readonly CloakModelService? _cloakModels;
        private readonly CreatureAttachmentModelService? _creatureAttachmentModels;

        internal SwlorModelResolutionHost(
            AppearanceService? appearances,
            PlaceableAppearanceService? placeables,
            DoorTypeService? doors,
            WaypointAppearanceService? waypoints,
            Func<int, BaseItemIconRow?>? baseItems,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists,
            CloakModelService? cloakModels,
            CreatureAttachmentModelService? creatureAttachmentModels)
        {
            _appearances = appearances;
            _placeables = placeables;
            _doors = doors;
            _waypoints = waypoints;
            _baseItems = baseItems;
            _itemBlueprintLoader = itemBlueprintLoader;
            _partModelExists = partModelExists;
            _cloakModels = cloakModels;
            _creatureAttachmentModels = creatureAttachmentModels;
        }

        public bool IsTableAvailable(ModelResolutionTable table) => table switch
        {
            ModelResolutionTable.CreatureAppearances => _appearances != null,
            ModelResolutionTable.Placeables => _placeables != null,
            ModelResolutionTable.Doors => _doors != null,
            ModelResolutionTable.Waypoints => _waypoints != null,
            ModelResolutionTable.BaseItems => _baseItems != null,
            _ => false
        };

        public CreatureAppearanceModelRow? GetCreatureAppearance(int appearanceId)
        {
            var row = _appearances?.GetAll().FirstOrDefault(candidate => candidate.Id == appearanceId);
            return row == null
                ? null
                : new CreatureAppearanceModelRow(row.Id, row.DisplayName, row.ModelType, row.Race);
        }

        public ModelNameRow? GetPlaceable(int appearanceId)
        {
            var row = _placeables?.GetAll().FirstOrDefault(candidate => candidate.Id == appearanceId);
            return row == null ? null : new ModelNameRow(row.DisplayName, row.ModelName);
        }

        public ModelNameRow? GetWaypoint(int appearanceId)
        {
            if (_waypoints == null || !_waypoints.TryGet(appearanceId, out var row))
                return null;
            return new ModelNameRow(row.DisplayName, row.ModelName);
        }

        public DoorModelRow? GetSpecificDoor(int appearanceId) =>
            ToDoorRow(_doors?.GetAll().FirstOrDefault(row => row.Id == appearanceId));

        public DoorModelRow? GetGenericDoor(int genericType) =>
            ToDoorRow(_doors?.GetGenericAll().FirstOrDefault(row => row.Id == genericType));

        public BaseItemModelRow? GetBaseItem(int baseItemId)
        {
            var row = _baseItems?.Invoke(baseItemId);
            return row == null ? null : new BaseItemModelRow(row.ItemClass, row.ModelType);
        }

        public CloakModelRow? GetCloak(int appearance)
        {
            var mapping = _cloakModels?.GetOrNull(appearance);
            return mapping is { } value
                ? new CloakModelRow(value.Model, value.Texture, value.HideLeftShoulder, value.HideRightShoulder)
                : null;
        }

        public JsonGffStruct? LoadItemBlueprint(string resRef) => _itemBlueprintLoader?.Invoke(resRef);

        /// <summary>A host with no resource index assumes every part model exists.</summary>
        public bool PartModelExists(string modelResRef) => _partModelExists?.Invoke(modelResRef) ?? true;

        public int? ReadItemAppearanceValue(JsonGffStruct item, string field) =>
            ItemAppearanceValues.Read(item, field);

        public bool IsShield(int baseItemId, string itemClass) =>
            (BaseItem)baseItemId is BaseItem.SmallShield or BaseItem.LargeShield or BaseItem.TowerShield;

        public string? FallbackItemModelResRef => LootBagModel;

        public IReadOnlyList<ModelPartReference> GetCreatureAttachments(
            JsonGffStruct creature,
            JsonGffStruct? armor,
            IReadOnlyDictionary<int, int> layerColorIndices)
        {
            if (_creatureAttachmentModels == null)
                return Array.Empty<ModelPartReference>();

            var parts = new List<ModelPartReference>();
            AddAttachment(
                parts, "wing", _creatureAttachmentModels.GetWingOrNull(creature.GetIntOrNull("Wings_New") ?? 0),
                layerColorIndices, armor);
            AddAttachment(
                parts, "tail", _creatureAttachmentModels.GetTailOrNull(creature.GetIntOrNull("Tail_New") ?? 0),
                layerColorIndices, armor);
            return parts;
        }

        private void AddAttachment(
            ICollection<ModelPartReference> parts,
            string partType,
            string? modelResRef,
            IReadOnlyDictionary<int, int> layerColorIndices,
            JsonGffStruct? armor)
        {
            if (string.IsNullOrWhiteSpace(modelResRef) || !PartModelExists(modelResRef))
                return;

            parts.Add(new ModelPartReference(
                partType,
                modelResRef,
                LayerColorIndices: layerColorIndices,
                UsesItemTintOverrides: armor != null,
                TintSourceItem: armor));
        }

        private static DoorModelRow? ToDoorRow(DoorTypeRow? row) =>
            row == null ? null : new DoorModelRow(row.DisplayName, row.Model, row.VisibleModel);

        private static DoorModelRow? ToDoorRow(GenericDoorRow? row) =>
            row == null ? null : new DoorModelRow(row.DisplayName, row.Model, row.VisibleModel);
    }
}
