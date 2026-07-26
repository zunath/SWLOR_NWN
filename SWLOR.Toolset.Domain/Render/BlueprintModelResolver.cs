using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using Radoub.Formats.Plt;

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
        Segmented
    }

    /// <summary>One resolved body part of a segmented creature: the MdlPartComposer bone-part type and its MDL resref.</summary>
    public readonly record struct BlueprintModelPart(string PartType, string ModelResRef);

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

        /// <summary>The skeleton/supermodel resref for <see cref="BlueprintModelKind.Segmented"/>; null otherwise.</summary>
        public string? SkeletonResRef { get; init; }

        /// <summary>The body parts (MdlPartComposer part type → resref) for <see cref="BlueprintModelKind.Segmented"/>.</summary>
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
    /// <c>p{gender}{race}{phenotype}</c> naming so the app can compose them with Radoub's MdlPartComposer.
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

        /// <summary>Equip_ItemList struct id for the chest slot (bit flags per the Aurora UTC format).</summary>
        private const int ChestSlotStructId = 2;

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
        /// doesn't resolve must not suppress the body parts it would have covered). Null = assume exists.
        /// </param>
        public static BlueprintModelReference Resolve(
            ResourceType type,
            JsonGffStruct root,
            AppearanceService? appearances,
            PlaceableAppearanceService? placeables,
            DoorTypeService? doors,
            Func<string, JsonGffStruct?>? itemBlueprintLoader = null,
            Func<string, bool>? partModelExists = null,
            WaypointAppearanceService? waypoints = null)
        {
            ArgumentNullException.ThrowIfNull(root);

            return type switch
            {
                ResourceType.Utc => ResolveCreature(root, appearances, itemBlueprintLoader, partModelExists),
                ResourceType.Utp => ResolvePlaceable(root, placeables),
                ResourceType.Utd => ResolveDoor(root, doors),
                ResourceType.Utw => ResolveWaypoint(root, waypoints),
                _ => BlueprintModelReference.NoneWith("No model preview for this blueprint type.")
            };
        }

        private static BlueprintModelReference ResolveCreature(
            JsonGffStruct root,
            AppearanceService? appearances,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists)
        {
            if (appearances == null)
                return BlueprintModelReference.NoneWith("Creature preview unavailable (appearance data not loaded).");

            var appearanceId = root.GetIntOrNull("Appearance_Type") ?? -1;
            var row = appearances.GetAll().FirstOrDefault(r => r.Id == appearanceId);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown appearance id {appearanceId}.");

            if (string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
                return ResolveSegmentedCreature(root, row, itemBlueprintLoader, partModelExists);

            var modelResRef = row.Race;
            if (string.IsNullOrWhiteSpace(modelResRef))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: no model resref in appearance.2da.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{row.DisplayName} ({modelResRef}.mdl)",
                ModelResRef = modelResRef
            };
        }

        private static BlueprintModelReference ResolveSegmentedCreature(
            JsonGffStruct root,
            AppearanceRow row,
            Func<string, JsonGffStruct?>? itemBlueprintLoader,
            Func<string, bool>? partModelExists)
        {
            var raceLetter = row.Race;
            if (string.IsNullOrWhiteSpace(raceLetter))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: segmented appearance has no race letter.");

            // NWN player-body prefix: p{gender}{race}{phenotype}, e.g. "pmh0" (player, male, human, phenotype 0).
            var gender = (root.GetIntOrNull("Gender") ?? 0) == 1 ? 'f' : 'm';
            var phenotype = root.GetIntOrNull("Phenotype") ?? 0;
            var prefix = $"p{gender}{char.ToLowerInvariant(raceLetter[0])}{phenotype}";

            var armor = LoadEquippedChestArmor(root, itemBlueprintLoader);
            var parts = new List<BlueprintModelPart>();

            // Robe first (armor-only; creatures have no robe body part), when its model resolves.
            // ALL body parts are still emitted alongside it — whether the robe replaces the parts
            // it covers depends on its geometry (RobeCoverage.IsFullBodyRobe), which the renderer
            // decides after loading the model; partial robes (loincloths, tabards) cover nothing.
            var robeNumber = armor?.GetIntOrNull("ArmorPart_Robe") ?? 0;
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
                var number = ResolvePartNumber(
                    root.GetIntOrNull(creatureField) ?? 0,
                    armor?.GetIntOrNull("ArmorPart_" + armorKey) ?? 0);
                if (number > 0)
                    parts.Add(new BlueprintModelPart(partType, BuildPartName(prefix, partType, number)));
            }

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

        /// <summary>Loads the equipped chest-slot armor's root struct from Equip_ItemList, if any.</summary>
        private static JsonGffStruct? LoadEquippedChestArmor(
            JsonGffStruct root, Func<string, JsonGffStruct?>? itemBlueprintLoader)
        {
            if (itemBlueprintLoader == null)
                return null;

            var resRef = GetEquippedChestArmorResRef(root);

            return string.IsNullOrWhiteSpace(resRef) ? null : itemBlueprintLoader(resRef);
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
            return chest?.GetStringOrNull("EquippedRes");
        }

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
                ModelResRef = model
            };
        }

        /// <summary>NWN body-part MDL naming: <c>{prefix}_{partType}{number:D3}</c>, e.g. <c>pmh0_chest001</c>.</summary>
        private static string BuildPartName(string prefix, string partType, int number) =>
            $"{prefix}_{partType}{number:D3}";
    }
}
