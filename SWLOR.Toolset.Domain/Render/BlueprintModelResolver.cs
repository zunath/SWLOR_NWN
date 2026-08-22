using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.NWN.Formats.Plt;

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
        string? TextureResRef = null);

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
    /// lookup services, headlessly. Creatures whose appearance is a simple model (MODELTYPE S/F/W/L: the
    /// appearance.2da RACE column holds the literal model resref) resolve to a single resref; segmented
    /// player-body creatures (MODELTYPE P) resolve to a skeleton + body-part list following NWN's
    /// <c>p{gender}{race}{phenotype}</c> naming so the app can compose them at render time.
    /// Placeables resolve through placeables.2da ModelName. Doors use genericdoors.2da for their
    /// generic appearance, or doortypes.2da when the specific Appearance field is non-zero.
    /// </summary>
    public static class BlueprintModelResolver
    {
        /// <summary>
        /// Creature field → armor-override key → MdlPartComposer / MdlPartBoneMap part type. Head is
        /// handled separately (utc field Appearance_Head; armor never overrides it). Note the Aurora
        /// format quirk on the right foot: the CREATURE's right-foot part number is stored under
        /// "ArmorPart_RFoot" on the utc root — "BodyPart_RFoot" does not exist anywhere in the format
        /// (corpus-verified: 447 utcs carry ArmorPart_RFoot, zero carry BodyPart_RFoot).
        /// </summary>
        private static readonly (string CreatureField, string ArmorKey, string PartType)[] BodyPartFields =
        {
            ("BodyPart_Neck", "Neck", "neck"),
            ("BodyPart_Torso", "Torso", "chest"),
            ("BodyPart_Belt", "Belt", "belt"),
            ("BodyPart_Pelvis", "Pelvis", "pelvis"),
            ("BodyPart_LShoul", "LShoul", "shol"),
            ("BodyPart_RShoul", "RShoul", "shor"),
            ("BodyPart_LBicep", "LBicep", "bicepl"),
            ("BodyPart_RBicep", "RBicep", "bicepr"),
            ("BodyPart_LFArm", "LFArm", "forel"),
            ("BodyPart_RFArm", "RFArm", "forer"),
            ("BodyPart_LHand", "LHand", "handl"),
            ("BodyPart_RHand", "RHand", "handr"),
            ("BodyPart_LThigh", "LThigh", "legl"),
            ("BodyPart_RThigh", "RThigh", "legr"),
            ("BodyPart_LShin", "LShin", "shinl"),
            ("BodyPart_RShin", "RShin", "shinr"),
            ("BodyPart_LFoot", "LFoot", "footl"),
            ("ArmorPart_RFoot", "RFoot", "footr"),
        };

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

        /// <summary>Visible Equip_ItemList struct ids (bit flags per the Aurora UTC/GIT format).</summary>
        private const int HeadSlotStructId = 1;
        private const int ChestSlotStructId = 2;
        private const int RightHandSlotStructId = 16;
        private const int LeftHandSlotStructId = 32;
        private const int CloakSlotStructId = 64;

        /// <summary>
        /// Resolves the preview model for a blueprint. Returns a <see cref="BlueprintModelKind.None"/>
        /// reference (never throws, never null) when the type is not previewable, a needed service is
        /// absent, or the appearance cannot be resolved.
        /// </summary>
        /// <param name="itemBlueprintLoader">
        /// Loads an item blueprint's root struct by resref (null / not found tolerated). Used to apply
        /// the equipped chest armor's ArmorPart_* overrides to segmented creatures — without it they
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
            CloakModelService? cloakModels = null)
        {
            ArgumentNullException.ThrowIfNull(root);

            return type switch
            {
                ResourceType.Utc => ResolveCreature(
                    root, appearances, itemBlueprintLoader, partModelExists, baseItems, cloakModels),
                ResourceType.Utp => ResolvePlaceable(root, placeables),
                ResourceType.Utd => ResolveDoor(root, doors),
                ResourceType.Utw => ResolveWaypoint(root, waypoints),
                ResourceType.Uti => ResolveItem(
                    root, baseItems, partModelExists, armorPreviewFemale, cloakModels),
                _ => BlueprintModelReference.NoneWith("No model preview for this blueprint type.")
            };
        }

        /// <summary>
        /// Resolves an item's preview model by its base item's ModelType. A ModelType 2 (composite)
        /// weapon resolves to its three fixed-position bottom/middle/top parts, named
        /// <c>{ItemClass}_b_{ModelPart1:D3}</c> / <c>_m_{ModelPart2:D3}</c> / <c>_t_{ModelPart3:D3}</c> -
        /// the same naming <see cref="Icons.ItemIconResolver"/> uses for the composite icon, minus its
        /// leading "i" (verified against the corpus: <c>wswls_b_015.mdl</c> sits beside
        /// <c>iwswls_b_015.tga</c> in sw_weapon). A ModelType 0/1 item resolves to a single ground
        /// model <c>{ItemClass}_{ModelPart1:D3}</c> when <paramref name="partModelExists"/> confirms it
        /// (also corpus-verified: <c>it_torch_015.mdl</c>, <c>helm_001.mdl</c>). ModelType 3 (armor)
        /// is assembled on a male or female mannequin from its body-part fields. An unrecognised
        /// ModelType degrades to the standard loot-bag model.
        /// </summary>
        private static BlueprintModelReference ResolveItem(
            JsonGffStruct root,
            Func<int, BaseItemIconRow?>? baseItems,
            Func<string, bool>? partModelExists,
            bool armorPreviewFemale = false,
            CloakModelService? cloakModels = null)
        {
            if (baseItems == null)
                return BlueprintModelReference.NoneWith("Item preview unavailable (base item data not loaded).");

            var baseItem = root.GetIntOrNull("BaseItem") ?? -1;
            var row = baseItem < 0 ? null : baseItems(baseItem);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown base item {baseItem}.");

            var itemClass = row.ItemClass;
            if (string.IsNullOrWhiteSpace(itemClass))
                return BlueprintModelReference.NoneWith($"Base item {baseItem}: no item class in baseitems.2da.");

            switch (row.ModelType)
            {
                case 2:
                {
                    var part1 = ItemAppearanceValues.Read(root, "ModelPart1") ?? 0;
                    var part2 = ItemAppearanceValues.Read(root, "ModelPart2") ?? 0;
                    var part3 = ItemAppearanceValues.Read(root, "ModelPart3") ?? 0;
                    var parts = new[]
                    {
                        new BlueprintModelPart("bottom", $"{itemClass}_b_{part1:D3}"),
                        new BlueprintModelPart("middle", $"{itemClass}_m_{part2:D3}"),
                        new BlueprintModelPart("top", $"{itemClass}_t_{part3:D3}")
                    };

                    if (partModelExists != null && !parts.Any(part => partModelExists(part.ModelResRef)))
                        return LootBagFallback(itemClass, partModelExists, "no composite part model resolves");

                    return new BlueprintModelReference
                    {
                        Kind = BlueprintModelKind.ItemComposite,
                        Status = $"{itemClass} (composite {part1}-{part2}-{part3})",
                        Parts = parts
                    };
                }

                case 0:
                case 1:
                {
                    var part1 = ItemAppearanceValues.Read(root, "ModelPart1") ?? 0;

                    // A cloak's own model is a skinmesh weighted to the skeleton's cloak chain - drawn
                    // by itself it is a flat sheet in mid-air. Worn on the mannequin it hangs where it
                    // is meant to, which is the only way to judge one.
                    if (string.Equals(itemClass, CloakItemClass, StringComparison.OrdinalIgnoreCase))
                    {
                        var cloakMapping = cloakModels?.GetOrNull(part1);
                        var cloakModel = cloakMapping?.Model ?? part1;
                        var cloakTexture = cloakMapping?.Texture ?? part1;
                        return ResolveCapeMannequin(
                            root, itemClass, armorPreviewFemale, partModelExists,
                            cloakModel, cloakTexture);
                    }

                    var modelResRef = $"{itemClass}_{part1:D3}";
                    if (partModelExists != null && !partModelExists(modelResRef))
                        return LootBagFallback(itemClass, partModelExists, $"no ground model '{modelResRef}'");

                    return new BlueprintModelReference
                    {
                        Kind = BlueprintModelKind.Simple,
                        Status = $"{modelResRef}.mdl",
                        ModelResRef = modelResRef,
                        LayerColorIndices = ResolveLayerColors(root, root)
                    };
                }

                case 3:
                    return ResolveArmorMannequin(root, itemClass, armorPreviewFemale, partModelExists);

                default:
                    return LootBagFallback(
                        itemClass, partModelExists, $"unsupported model type {row.ModelType}");
            }
        }

        /// <summary>
        /// The model NWN itself drops on the ground for an item with no ground model of its own:
        /// the loot bag (baseitems.2da's near-universal DefaultModel). Every item therefore always
        /// has SOMETHING to show in a 3D preview; only a session that cannot resolve even the bag
        /// (no base-game data) degrades to no model at all.
        /// </summary>
        private static BlueprintModelReference LootBagFallback(
            string itemClass, Func<string, bool>? partModelExists, string why)
        {
            const string BagModel = "it_bag";
            if (partModelExists != null && !partModelExists(BagModel))
                return BlueprintModelReference.NoneWith($"{itemClass}: {why}, and no loot bag model.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{itemClass}: {why} - showing the loot bag.",
                ModelResRef = BagModel
            };
        }

        /// <summary>
        /// Dresses a default human mannequin (<c>pmh0</c>/<c>pfh0</c>) with the armor blueprint's
        /// own parts - the "worn" preview the item editor shows for a ModelType 3 base item. The
        /// mannequin's naked baseline is part 1 for every body piece (head included) and none for
        /// the shoulders; each ArmorPart_* the blueprint carries overrides its slot, the robe is
        /// added when one is set, and the six dye channels come from the blueprint's color fields
        /// through the same PLT layer mapping a dressed creature uses.
        /// </summary>
        private static BlueprintModelReference ResolveArmorMannequin(
            JsonGffStruct root,
            string itemClass,
            bool female,
            Func<string, bool>? partModelExists)
        {
            var prefix = female ? "pfh0" : "pmh0";
            var parts = new List<BlueprintModelPart>();

            var robeNumber = ItemAppearanceValues.Read(root, "ArmorPart_Robe") ?? 0;
            if (robeNumber > 0)
            {
                var robeResRef = BuildPartName(prefix, "robe", robeNumber);
                if (partModelExists?.Invoke(robeResRef) ?? true)
                    parts.Add(new BlueprintModelPart("robe", robeResRef));
            }

            parts.Add(new BlueprintModelPart("head", BuildPartName(prefix, "head", 1)));

            foreach (var (_, armorKey, partType) in BodyPartFields)
            {
                var armorValue = ItemAppearanceValues.Read(root, "ArmorPart_" + armorKey) ?? 0;

                // Unlike a dressed creature (where a creature part of 0 means "this body has no
                // such part"), the mannequin exists to SHOW the armor: an armor part always wins,
                // and only the armor-less slots fall back to the bare body (shoulders have no
                // bare-body piece at all).
                var number = armorValue > 0
                    ? armorValue
                    : partType is "shol" or "shor" ? 0 : 1;
                if (number > 0)
                    parts.Add(new BlueprintModelPart(partType, BuildPartName(prefix, partType, number)));
            }

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Segmented,
                Status = $"{itemClass} on a {(female ? "female" : "male")} mannequin ({prefix})",
                SkeletonResRef = prefix,
                Parts = parts,
                // The item struct carries no Color_* creature fields, so skin/hair fall to palette
                // row 0; the armor dye channels come from the blueprint itself.
                LayerColorIndices = ResolveLayerColors(root, root)
            };
        }

        /// <summary>baseitems.2da's ItemClass for a cloak - the same string ItemFamilyClassifier reads.</summary>
        private const string CloakItemClass = "cloak";

        /// <summary>
        /// A cape dressed on a plain mannequin: the bare body, plus the cloak at the number the
        /// blueprint names. Cloak part resources are spelled with an underscore before the number
        /// (pmh0_cloak_001, seven of them), unlike every other body part.
        /// </summary>
        private static BlueprintModelReference ResolveCapeMannequin(
            JsonGffStruct root,
            string itemClass,
            bool female,
            Func<string, bool>? partModelExists,
            int cloakNumber,
            int cloakTextureNumber)
        {
            var prefix = female ? "pfh0" : "pmh0";
            var cloakResRef = $"{prefix}_cloak_{cloakNumber:D3}";
            var cloakTextureResRef = $"{prefix}_cloak_{cloakTextureNumber:D3}";
            if (partModelExists != null && !partModelExists(cloakResRef))
                return LootBagFallback(itemClass, partModelExists, $"no cloak model '{cloakResRef}'");

            var parts = new List<BlueprintModelPart>
            {
                new("cloak", cloakResRef, TextureResRef: cloakTextureResRef)
            };
            parts.Add(new BlueprintModelPart("head", BuildPartName(prefix, "head", 1)));
            foreach (var (_, _, partType) in BodyPartFields)
            {
                // The body is here to hang the cape on, so it stays plain: part 1 everywhere it
                // exists, and shoulders (which have no bare-body piece) left off.
                if (partType is "shol" or "shor")
                    continue;

                parts.Add(new BlueprintModelPart(partType, BuildPartName(prefix, partType, 1)));
            }

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Segmented,
                Status = $"{itemClass} on a {(female ? "female" : "male")} mannequin ({prefix})",
                SkeletonResRef = prefix,
                Parts = parts,
                LayerColorIndices = ResolveLayerColors(root, root)
            };
        }

        private static BlueprintModelReference ResolveCreature(
            JsonGffStruct root,
            AppearanceService? appearances,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists,
            Func<int, BaseItemIconRow?>? baseItems,
            CloakModelService? cloakModels)
        {
            if (appearances == null)
                return BlueprintModelReference.NoneWith("Creature preview unavailable (appearance data not loaded).");

            var appearanceId = root.GetIntOrNull("Appearance_Type") ?? -1;
            var row = appearances.GetAll().FirstOrDefault(r => r.Id == appearanceId);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown appearance id {appearanceId}.");

            if (string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
            {
                var prefix = SegmentedCreaturePrefix(root, row);
                if (prefix == null)
                    return BlueprintModelReference.NoneWith(
                        $"{row.DisplayName}: segmented appearance has no race letter.");

                var visibleEquipment = ResolveVisibleEquipment(
                    root, itemBlueprintLoader, partModelExists, baseItems, cloakModels, prefix);
                return ResolveSegmentedCreature(
                    root, row, prefix, itemBlueprintLoader, partModelExists, visibleEquipment);
            }

            var modelResRef = row.Race;
            if (string.IsNullOrWhiteSpace(modelResRef))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: no model ResRef in appearance.2da.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{row.DisplayName} ({modelResRef}.mdl)",
                ModelResRef = modelResRef,
                Parts = ResolveVisibleEquipment(
                    root, itemBlueprintLoader, partModelExists, baseItems, cloakModels,
                    wearerPrefix: null).Parts
            };
        }

        private static string? SegmentedCreaturePrefix(JsonGffStruct root, AppearanceRow row)
        {
            if (string.IsNullOrWhiteSpace(row.Race))
                return null;

            // NWN player-body prefix: p{gender}{race}{phenotype}, e.g. "pmh0".
            var gender = (root.GetIntOrNull("Gender") ?? 0) == 1 ? 'f' : 'm';
            var phenotype = root.GetIntOrNull("Phenotype") ?? 0;
            return $"p{gender}{char.ToLowerInvariant(row.Race[0])}{phenotype}";
        }

        private static BlueprintModelReference ResolveSegmentedCreature(
            JsonGffStruct root,
            AppearanceRow row,
            string prefix,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists,
            VisibleEquipment visibleEquipment)
        {
            var armor = LoadEquippedChestArmor(root, itemBlueprintLoader);
            var parts = new List<BlueprintModelPart>();

            // Robe first (armor-only; creatures have no robe body part), when its model resolves.
            // ALL body parts are still emitted alongside it — whether the robe replaces the parts
            // it covers depends on its geometry (RobeCoverage.IsFullBodyRobe), which the renderer
            // decides after loading the model; partial robes (loincloths, tabards) cover nothing.
            var robeNumber = armor == null
                ? 0
                : ItemAppearanceValues.Read(armor, "ArmorPart_Robe") ?? 0;
            if (robeNumber > 0)
            {
                var robeResRef = BuildPartName(prefix, "robe", robeNumber);
                if (partModelExists?.Invoke(robeResRef) ?? true)
                    parts.Add(new BlueprintModelPart("robe", robeResRef));
            }

            var head = root.GetIntOrNull("Appearance_Head");
            if (head is > 0)
                parts.Add(new BlueprintModelPart("head", BuildPartName(prefix, "head", head.Value)));

            foreach (var (creatureField, armorKey, partType) in BodyPartFields)
            {
                if (visibleEquipment.HiddenBodyParts.Contains(partType))
                    continue;

                var number = ResolvePartNumber(
                    root.GetIntOrNull(creatureField) ?? 0,
                    armor == null
                        ? 0
                        : ItemAppearanceValues.Read(armor, "ArmorPart_" + armorKey) ?? 0);
                if (number > 0)
                    parts.Add(new BlueprintModelPart(partType, BuildPartName(prefix, partType, number)));
            }

            parts.AddRange(visibleEquipment.Parts);

            if (parts.Count == 0)
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: segmented creature has no body parts.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Segmented,
                Status = $"{row.DisplayName} (segmented {prefix}, {parts.Count} parts)",
                SkeletonResRef = prefix,
                Parts = parts,
                LayerColorIndices = ResolveLayerColors(root, armor)
            };
        }

        private static IReadOnlyDictionary<int, int> ResolveLayerColors(
            JsonGffStruct creature,
            JsonGffStruct? armor)
        {
            var colors = Enumerable.Range(0, 10).ToDictionary(layer => layer, _ => 0);

            // Absent means row 0, which is what Aurora shows: its item preview dresses a default
            // mannequin whose unspecified layers take the palette's first row. Picking a
            // "nicer" mid-palette row instead turned the head and hands red, because a palette row
            // is a gradient and only its brightest column is the pale tone I had sampled.
            colors[PltLayers.Skin] = creature.GetIntOrNull("Color_Skin") ?? 0;
            colors[PltLayers.Hair] = creature.GetIntOrNull("Color_Hair") ?? 0;
            colors[PltLayers.Tattoo1] = creature.GetIntOrNull("Color_Tattoo1") ?? 0;
            colors[PltLayers.Tattoo2] = creature.GetIntOrNull("Color_Tattoo2") ?? 0;

            if (armor != null)
            {
                colors[PltLayers.Metal1] = armor.GetIntOrNull("Metal1Color") ?? 0;
                colors[PltLayers.Metal2] = armor.GetIntOrNull("Metal2Color") ?? 0;
                colors[PltLayers.Cloth1] = armor.GetIntOrNull("Cloth1Color") ?? 0;
                colors[PltLayers.Cloth2] = armor.GetIntOrNull("Cloth2Color") ?? 0;
                colors[PltLayers.Leather1] = armor.GetIntOrNull("Leather1Color") ?? 0;
                colors[PltLayers.Leather2] = armor.GetIntOrNull("Leather2Color") ?? 0;
            }

            return colors;
        }

        /// <summary>
        /// Part-number precedence, matching Quartermaster's creature renderer: a creature value of 0
        /// (none/invisible) always wins; otherwise the equipped armor's part overrides the creature's
        /// naked body part; otherwise the creature value stands.
        /// </summary>
        private static int ResolvePartNumber(int creatureValue, int armorValue)
        {
            if (creatureValue == 0)
                return 0;

            return armorValue > 0 ? armorValue : creatureValue;
        }

        /// <summary>
        /// Resolves the models for equipment the game draws on a creature. Ordinary right- and
        /// left-hand items are held props; creature natural-weapon/stat slots are deliberately not.
        /// </summary>
        private readonly record struct VisibleEquipment(
            IReadOnlyList<BlueprintModelPart> Parts,
            IReadOnlySet<string> HiddenBodyParts);

        private static VisibleEquipment ResolveVisibleEquipment(
            JsonGffStruct root,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists,
            Func<int, BaseItemIconRow?>? baseItems,
            CloakModelService? cloakModels,
            string? wearerPrefix)
        {
            if (baseItems == null)
            {
                return new VisibleEquipment(
                    Array.Empty<BlueprintModelPart>(),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }

            var parts = new List<BlueprintModelPart>();
            AddVisibleEquipmentPart(
                parts, root, HeadSlotStructId, "helmet",
                itemBlueprintLoader, partModelExists, baseItems, cloakModels, wearerPrefix);
            AddVisibleEquipmentPart(
                parts, root, CloakSlotStructId, "cloak",
                itemBlueprintLoader, partModelExists, baseItems, cloakModels, wearerPrefix);
            AddVisibleEquipmentPart(
                parts, root, RightHandSlotStructId, "weaponr",
                itemBlueprintLoader, partModelExists, baseItems, cloakModels, wearerPrefix);
            AddVisibleEquipmentPart(
                parts, root, LeftHandSlotStructId, "weaponl",
                itemBlueprintLoader, partModelExists, baseItems, cloakModels, wearerPrefix);
            return new VisibleEquipment(
                parts,
                ResolveCloakHiddenBodyParts(root, itemBlueprintLoader, cloakModels));
        }

        private static IReadOnlySet<string> ResolveCloakHiddenBodyParts(
            JsonGffStruct creature,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            CloakModelService? cloakModels)
        {
            var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cloak = LoadEquippedItem(creature, CloakSlotStructId, itemBlueprintLoader);
            var appearance = cloak == null
                ? null
                : ItemAppearanceValues.Read(cloak, "ModelPart1");
            var mapping = appearance is { } value ? cloakModels?.GetOrNull(value) : null;
            if (mapping?.HideLeftShoulder == true)
                hidden.Add("shol");
            if (mapping?.HideRightShoulder == true)
                hidden.Add("shor");
            return hidden;
        }

        private static void AddVisibleEquipmentPart(
            ICollection<BlueprintModelPart> destination,
            JsonGffStruct creature,
            int slot,
            string attachmentType,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists,
            Func<int, BaseItemIconRow?> baseItems,
            CloakModelService? cloakModels,
            string? wearerPrefix)
        {
            var item = LoadEquippedItem(creature, slot, itemBlueprintLoader);
            if (item == null)
                return;

            var reference = ResolveItem(
                item, baseItems, partModelExists, armorPreviewFemale: false,
                cloakModels: cloakModels);
            if (attachmentType == "cloak")
            {
                foreach (var part in reference.Parts.Where(
                             part => part.PartType.Equals("cloak", StringComparison.OrdinalIgnoreCase)))
                {
                    var modelResRef = part.ModelResRef;
                    var textureResRef = part.TextureResRef;
                    if (!string.IsNullOrWhiteSpace(wearerPrefix))
                    {
                        var cloakSuffix = modelResRef.IndexOf("_cloak_", StringComparison.OrdinalIgnoreCase);
                        if (cloakSuffix >= 0)
                            modelResRef = wearerPrefix + modelResRef[cloakSuffix..];

                        var textureSuffix = textureResRef?.IndexOf(
                            "_cloak_", StringComparison.OrdinalIgnoreCase) ?? -1;
                        if (textureSuffix >= 0)
                            textureResRef = wearerPrefix + textureResRef![textureSuffix..];
                    }

                    if (partModelExists?.Invoke(modelResRef) ?? true)
                    {
                        destination.Add(new BlueprintModelPart(
                            attachmentType, modelResRef, reference.LayerColorIndices, textureResRef));
                    }
                }

                return;
            }

            if (reference.Kind == BlueprintModelKind.ItemComposite)
            {
                foreach (var part in reference.Parts)
                {
                    destination.Add(new BlueprintModelPart(
                        attachmentType, part.ModelResRef, reference.LayerColorIndices));
                }
                return;
            }

            if (reference.Kind == BlueprintModelKind.Simple &&
                !string.IsNullOrWhiteSpace(reference.ModelResRef) &&
                !reference.ModelResRef.Equals("it_bag", StringComparison.OrdinalIgnoreCase))
            {
                destination.Add(new BlueprintModelPart(
                    attachmentType, reference.ModelResRef, reference.LayerColorIndices));
            }
        }

        /// <summary>Loads the equipped chest-slot armor's embedded item or referenced blueprint.</summary>
        private static JsonGffStruct? LoadEquippedChestArmor(
            JsonGffStruct root, Func<string, JsonGffStruct?>? itemBlueprintLoader)
        {
            return LoadEquippedItem(root, ChestSlotStructId, itemBlueprintLoader);
        }

        /// <summary>
        /// GIT creatures embed the complete equipped UTI struct; UTC blueprints usually store only
        /// an EquippedRes reference. Prefer the embedded copy because it carries per-instance part
        /// and dye overrides, then fall back to loading either supported resref spelling.
        /// </summary>
        private static JsonGffStruct? LoadEquippedItem(
            JsonGffStruct root,
            int slot,
            Func<string, JsonGffStruct?>? itemBlueprintLoader)
        {
            var equipped = root.GetListOrEmpty("Equip_ItemList")
                .FirstOrDefault(item => ParseStructId(item.RawStructId) == slot);
            if (equipped == null)
                return null;

            if (equipped.GetIntOrNull("BaseItem").HasValue ||
                equipped.GetIntOrNull("ArmorPart_Torso").HasValue ||
                equipped.GetIntOrNull("ModelPart1").HasValue)
            {
                return IsDegenerateEmbeddedItem(equipped) ? null : equipped;
            }

            var resRef = GetEquippedItemResRef(equipped);
            return string.IsNullOrWhiteSpace(resRef) || itemBlueprintLoader == null
                ? null
                : itemBlueprintLoader(resRef);
        }

        /// <summary>
        /// Several shipped areas carry a leftover equipped-slot struct with a blank TemplateResRef,
        /// BaseItem 0, and every appearance part zeroed - no blueprint identity and nothing to
        /// draw. Rendering it would fabricate a part-000 prop no model exists for, so it counts as
        /// an empty hand instead. Anything with a resref or a real appearance value is an item.
        /// </summary>
        private static bool IsDegenerateEmbeddedItem(JsonGffStruct equipped)
        {
            if (!string.IsNullOrWhiteSpace(equipped.GetStringOrNull("TemplateResRef")))
                return false;

            if ((equipped.GetIntOrNull("BaseItem") ?? 0) != 0 ||
                equipped.GetIntOrNull("ArmorPart_Torso").HasValue)
            {
                return false;
            }

            return (ItemAppearanceValues.Read(equipped, "ModelPart1") ?? 0) == 0 &&
                   (ItemAppearanceValues.Read(equipped, "ModelPart2") ?? 0) == 0 &&
                   (ItemAppearanceValues.Read(equipped, "ModelPart3") ?? 0) == 0;
        }

        /// <summary>
        /// The item blueprint resref supplying a segmented creature's visible armor, if any. Shared
        /// with thumbnail caching so the cache observes the same dependency as model resolution.
        /// </summary>
        public static string? GetEquippedChestArmorResRef(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var chest = root.GetListOrEmpty("Equip_ItemList")
                .FirstOrDefault(item => ParseStructId(item.RawStructId) == ChestSlotStructId);
            return chest == null ? null : GetEquippedItemResRef(chest);
        }

        /// <summary>
        /// Every equipped item blueprint that can change a creature preview: chest armor, helmet,
        /// cloak, and both held items. Thumbnail dependency tracking uses the same slot set as model
        /// resolution so editing any visible item invalidates every creature wearing it.
        /// </summary>
        public static IReadOnlyList<string> GetVisibleEquippedItemResRefs(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var visibleSlots = new HashSet<int>
            {
                HeadSlotStructId,
                ChestSlotStructId,
                RightHandSlotStructId,
                LeftHandSlotStructId,
                CloakSlotStructId
            };

            return root.GetListOrEmpty("Equip_ItemList")
                .Where(item => visibleSlots.Contains(ParseStructId(item.RawStructId)))
                .Select(GetEquippedItemResRef)
                .Where(resRef => !string.IsNullOrWhiteSpace(resRef))
                .Select(resRef => resRef!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? GetEquippedItemResRef(JsonGffStruct item) =>
            item.GetStringOrNull("EquippedRes") ?? item.GetStringOrNull("TemplateResRef");

        private static int ParseStructId(byte[]? raw)
        {
            return raw != null &&
                   int.TryParse(System.Text.Encoding.ASCII.GetString(raw),
                       System.Globalization.NumberStyles.Integer,
                       System.Globalization.CultureInfo.InvariantCulture, out var id)
                ? id
                : -1;
        }

        private static BlueprintModelReference ResolvePlaceable(JsonGffStruct root, PlaceableAppearanceService? placeables)
        {
            if (placeables == null)
                return BlueprintModelReference.NoneWith("Placeable preview unavailable (placeable data not loaded).");

            var appearanceId = root.GetIntOrNull("Appearance") ?? -1;
            var row = placeables.GetAll().FirstOrDefault(r => r.Id == appearanceId);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown placeable appearance id {appearanceId}.");

            if (string.IsNullOrWhiteSpace(row.ModelName))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: no model in placeables.2da.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{row.DisplayName} ({row.ModelName}.mdl)",
                ModelResRef = row.ModelName
            };
        }

        /// <summary>
        /// A waypoint's marker model, from waypoint.2da.
        /// </summary>
        /// <remarks>
        /// Unlike placeables.2da there is no separate model column - the row's RESREF is the model.
        /// </remarks>
        private static BlueprintModelReference ResolveWaypoint(
            JsonGffStruct root, WaypointAppearanceService? waypoints)
        {
            if (waypoints == null)
                return BlueprintModelReference.NoneWith("Waypoint preview unavailable (waypoint data not loaded).");

            var appearanceId = root.GetIntOrNull("Appearance") ?? -1;
            if (!waypoints.TryGet(appearanceId, out var row))
                return BlueprintModelReference.NoneWith($"Unknown waypoint appearance {appearanceId}.");

            if (string.IsNullOrWhiteSpace(row.ModelName))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: no model in waypoint.2da.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{row.DisplayName} ({row.ModelName}.mdl)",
                ModelResRef = row.ModelName
            };
        }

        private static BlueprintModelReference ResolveDoor(JsonGffStruct root, DoorTypeService? doors)
        {
            if (doors == null)
                return BlueprintModelReference.NoneWith("Door preview unavailable (door-type data not loaded).");

            // Appearance names a specific doortypes.2da model when non-zero. Otherwise
            // GenericType_New (or legacy GenericType) indexes genericdoors.2da.
            var specificId = root.GetIntOrNull("Appearance") ?? 0;
            var specific = specificId > 0
                ? doors.GetAll().FirstOrDefault(row => row.Id == specificId)
                : null;
            var genericId = root.GetIntOrNull("GenericType_New")
                            ?? root.GetIntOrNull("GenericType")
                            ?? 0;
            var generic = specificId == 0
                ? doors.GetGenericAll().FirstOrDefault(row => row.Id == genericId)
                : null;
            var displayName = specific?.DisplayName ?? generic?.DisplayName;
            var model = specific?.Model ?? generic?.Model;
            var visibleModel = specific?.VisibleModel ?? generic?.VisibleModel ?? true;
            var table = specific != null ? "doortypes.2da" : "genericdoors.2da";

            if (displayName == null)
                return BlueprintModelReference.NoneWith(
                    $"Unknown {(specificId > 0 ? "specific" : "generic")} door type " +
                    $"{(specificId > 0 ? specificId : genericId)}.");

            if (string.IsNullOrWhiteSpace(model))
                return BlueprintModelReference.NoneWith($"{displayName}: no model in {table}.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{displayName} ({model}.mdl)",
                ModelResRef = model,
                IsDoorTransition = !visibleModel
            };
        }

        /// <summary>NWN body-part MDL naming: <c>{prefix}_{partType}{number:D3}</c>, e.g. <c>pmh0_chest001</c>.</summary>
        private static string BuildPartName(string prefix, string partType, int number) =>
            $"{prefix}_{partType}{number:D3}";
    }
}
