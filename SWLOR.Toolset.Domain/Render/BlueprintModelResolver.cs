using SWLOR.Toolset.Domain.GameData.Lookups;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using SWLOR.Toolset.Domain.Gff;
using Nwn.Authoring.Appearances;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>How a blueprint's preview model is assembled.</summary>
    public enum BlueprintModelKind
    {
        /// <summary>No model could be resolved; <see cref="BlueprintModelReference.Status"/> explains why.</summary>
        None,

        /// <summary>A single MDL resref (<see cref="BlueprintModelReference.ModelResRef"/>) is parsed and rendered directly.</summary>
        Simple,

        /// <summary>
        /// A segmented player-body model: a skeleton (<see cref="BlueprintModelReference.SkeletonResRef"/>) plus
        /// per-bone body parts (<see cref="BlueprintModelReference.Parts"/>), composed at render time.
        /// </summary>
        Segmented,

        /// <summary>
        /// A composite item's fixed-position part models (a ModelType 2 weapon's bottom/middle/top,
        /// <see cref="BlueprintModelReference.Parts"/>), merged with no skeleton at render time -
        /// <c>MdlPartComposer.ComposeFlat</c>.
        /// </summary>
        ItemComposite
    }

    /// <summary>
    /// One resolved body/equipment part: the MdlPartComposer attachment type, its MDL resref, any
    /// item-specific PLT palette choices, and an optional texture selected independently of geometry.
    /// Equipment palettes live here because a cloak and the chest armor beneath it can intentionally
    /// use different dye rows; cloakmodel.2da similarly lets multiple appearances share geometry while
    /// selecting different surfaces.
    /// </summary>
    public readonly record struct BlueprintModelPart(
        string PartType,
        string ModelResRef,
        IReadOnlyDictionary<int, int>? LayerColorIndices = null,
        string? TextureResRef = null,
        bool UsesItemTintOverrides = false,
        IReadOnlyDictionary<string, int>? TintMapOverrides = null,
        AppearanceArmor ArmorPart = AppearanceArmor.Invalid);

    /// <summary>
    /// The resolved preview-model description for a blueprint, produced by <see cref="BlueprintModelResolver"/>.
    /// Pure data: it names resrefs and (for segmented creatures) the skeleton + part list, but never touches
    /// the resource index, MDL parser, or GL — those live in the app layer that consumes this.
    /// </summary>
    public sealed class BlueprintModelReference
    {
        public required BlueprintModelKind Kind { get; init; }

        /// <summary>A human-readable note for status display (the appearance label, or why nothing resolved).</summary>
        public required string Status { get; init; }

        /// <summary>The single model resref for <see cref="BlueprintModelKind.Simple"/>; null otherwise.</summary>
        public string? ModelResRef { get; init; }

        /// <summary>
        /// The meshes in <see cref="ModelResRef"/> belong to the item being previewed. This is
        /// separate from <see cref="BlueprintModelPart.UsesItemTintOverrides"/> because a simple
        /// ModelType 0/1 UTI has no composed part record.
        /// </summary>
        public bool RootUsesItemTintOverrides { get; init; }

        /// <summary>
        /// The resolved door row declares <c>VisibleModel=0</c>. These are area-transition planes:
        /// invisible at runtime, but drawn translucently by the toolset from the model's hidden
        /// selection geometry.
        /// </summary>
        public bool IsDoorTransition { get; init; }

        /// <summary>The skeleton/supermodel resref for <see cref="BlueprintModelKind.Segmented"/>; null otherwise.</summary>
        public string? SkeletonResRef { get; init; }

        /// <summary>
        /// Body parts for <see cref="BlueprintModelKind.Segmented"/>, or visible equipment attached
        /// to a <see cref="BlueprintModelKind.Simple"/> creature (MdlPartComposer part type → resref).
        /// </summary>
        public IReadOnlyList<BlueprintModelPart> Parts { get; init; } = Array.Empty<BlueprintModelPart>();

        /// <summary>
        /// PLT layer id to palette-row index for segmented creature textures. Empty for models that
        /// do not carry creature/armor palette choices.
        /// </summary>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
            new Dictionary<int, int>();

        public static BlueprintModelReference NoneWith(string status) =>
            new() { Kind = BlueprintModelKind.None, Status = status };
    }

    /// <summary>
    /// Resolves the preview model for a blueprint document from its appearance field and the game-data
    /// lookup services, headlessly, through the shared <see cref="ModelReferenceResolver"/> with SWLOR's
    /// rules supplied by <see cref="SwlorModelResolutionHost"/>. Creatures whose appearance is a simple model (MODELTYPE S/F/W/L: the
    /// appearance.2da RACE column holds the literal model resref) resolve to a single resref; segmented
    /// player-body creatures (MODELTYPE P) resolve to a skeleton + body-part list following NWN's
    /// <c>p{gender}{race}{phenotype}</c> naming so the app can compose them at render time.
    /// Placeables resolve through placeables.2da ModelName. Doors use genericdoors.2da for their
    /// generic appearance, or doortypes.2da when the specific Appearance field is non-zero.
    /// </summary>
    public static class BlueprintModelResolver
    {

        /// <summary>
        /// Parts a FULL-BODY robe replaces (same set as Quartermaster's RobePartSuppression):
        /// everything except head, neck, feet, and belt. Whether a given robe is actually
        /// full-body is a geometry question (<see cref="RobeCoverage.IsFullBodyRobe"/>) the
        /// renderer answers after loading the robe model — SWLOR's partial robes (loincloths,
        /// tabards) must NOT suppress anything. The resolver therefore always emits robe + all
        /// body parts; consumers filter with this set only when the robe proves full-body.
        /// </summary>
        public static readonly IReadOnlySet<string> RobeCoveredParts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "chest", "pelvis", "legl", "legr", "shol", "shor", "bicepl", "bicepr",
            "forel", "forer", "handl", "handr", "shinl", "shinr",
        };

        /// <summary>
        /// Resolves the preview model for a blueprint. Returns a <see cref="BlueprintModelKind.None"/>
        /// reference (never throws, never null) when the type is not previewable, a needed service is
        /// absent, or the appearance cannot be resolved.
        /// </summary>
        /// <param name="itemBlueprintLoader">
        /// Loads an item blueprint's root struct by resref (null / not found tolerated). Used to apply
        /// the equipped chest armor's ArmorPart_* overrides to segmented creatures - without it they
        /// resolve as their naked body.
        /// </param>
        /// <param name="partModelExists">
        /// Tests whether a body-part MDL resref exists. Only consulted for robe activation (a robe that
        /// doesn't resolve must not suppress the body parts it would have covered) and for a
        /// ModelType 0/1 item's single ground model. Null = assume exists.
        /// </param>
        /// <param name="baseItems">
        /// baseitems.2da row lookup by BaseItem id, for <see cref="ResourceType.Uti"/> and visible
        /// creature equipment. Null = item models cannot be resolved (the caller has no 2DA layer loaded).
        /// </param>
        public static BlueprintModelReference Resolve(
            ResourceType type,
            JsonGffStruct root,
            AppearanceService? appearances,
            PlaceableAppearanceService? placeables,
            DoorTypeService? doors,
            Func<string, JsonGffStruct?>? itemBlueprintLoader = null,
            Func<string, bool>? partModelExists = null,
            WaypointAppearanceService? waypoints = null,
            Func<int, BaseItemIconRow?>? baseItems = null,
            bool armorPreviewFemale = false,
            CloakModelService? cloakModels = null,
            CreatureAttachmentModelService? creatureAttachmentModels = null)
        {
            ArgumentNullException.ThrowIfNull(root);

            var host = new SwlorModelResolutionHost(
                appearances, placeables, doors, waypoints, baseItems, itemBlueprintLoader,
                partModelExists, cloakModels, creatureAttachmentModels);
            return ToBlueprintReference(ModelReferenceResolver.Resolve(type, root, host, armorPreviewFemale));
        }

        /// <summary>
        /// Adds SWLOR's per-part armor slot and per-item tint-map overrides to the shared description.
        /// Tint maps are read once per item so the parts of one item share the same dictionary.
        /// </summary>
        private static BlueprintModelReference ToBlueprintReference(ModelReference reference)
        {
            var tintMaps = new Dictionary<JsonGffStruct, IReadOnlyDictionary<string, int>>(
                ReferenceEqualityComparer.Instance);
            var parts = reference.Parts
                .Select(part => ToBlueprintPart(part, tintMaps))
                .ToArray();

            return new BlueprintModelReference
            {
                Kind = (BlueprintModelKind)(int)reference.Kind,
                Status = reference.Status,
                ModelResRef = reference.ModelResRef,
                RootUsesItemTintOverrides = reference.RootUsesItemTintOverrides,
                IsDoorTransition = reference.IsDoorTransition,
                SkeletonResRef = reference.SkeletonResRef,
                Parts = parts,
                LayerColorIndices = reference.LayerColorIndices
            };
        }

        private static BlueprintModelPart ToBlueprintPart(
            ModelPartReference part,
            Dictionary<JsonGffStruct, IReadOnlyDictionary<string, int>> tintMaps)
        {
            IReadOnlyDictionary<string, int>? tintMapOverrides = null;
            if (part.TintSourceItem is { } item && !tintMaps.TryGetValue(item, out tintMapOverrides))
            {
                tintMapOverrides = TintMapOverrides.Read(new VarTable(item));
                tintMaps[item] = tintMapOverrides;
            }

            return new BlueprintModelPart(
                part.PartType,
                part.ModelResRef,
                part.LayerColorIndices,
                part.TextureResRef,
                part.UsesItemTintOverrides,
                tintMapOverrides,
                part.UsesItemTintOverrides ? GetArmorPart(part.PartType) : AppearanceArmor.Invalid);
        }

        private static AppearanceArmor GetArmorPart(string partType)
        {
            return partType switch
            {
                "footr" => AppearanceArmor.RightFoot,
                "footl" => AppearanceArmor.LeftFoot,
                "shinr" => AppearanceArmor.RightShin,
                "shinl" => AppearanceArmor.LeftShin,
                "legl" => AppearanceArmor.LeftThigh,
                "legr" => AppearanceArmor.RightThigh,
                "pelvis" => AppearanceArmor.Pelvis,
                "chest" => AppearanceArmor.Torso,
                "belt" => AppearanceArmor.Belt,
                "neck" => AppearanceArmor.Neck,
                "forer" => AppearanceArmor.RightForearm,
                "forel" => AppearanceArmor.LeftForearm,
                "bicepr" => AppearanceArmor.RightBicep,
                "bicepl" => AppearanceArmor.LeftBicep,
                "shor" => AppearanceArmor.RightShoulder,
                "shol" => AppearanceArmor.LeftShoulder,
                "handr" => AppearanceArmor.RightHand,
                "handl" => AppearanceArmor.LeftHand,
                "robe" => AppearanceArmor.Robe,
                _ => AppearanceArmor.Invalid
            };
        }

        /// <summary>
        /// The item blueprint resref supplying a segmented creature's visible armor, if any. Shared
        /// with thumbnail caching so the cache observes the same dependency as model resolution.
        /// </summary>
        public static string? GetEquippedChestArmorResRef(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);
            return CreatureEquipmentResolver.Resolve(root).Armor?.BlueprintResRef;
        }

        /// <summary>Visible equipment references used to invalidate dependent creature previews.</summary>
        public static IReadOnlyList<string> GetVisibleEquippedItemResRefs(JsonGffStruct root) =>
            CreatureEquipmentResolver.GetVisibleBlueprintResRefs(root);
    }
}
