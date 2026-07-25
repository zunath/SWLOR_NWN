using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

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

        public static BlueprintModelReference NoneWith(string status) =>
            new() { Kind = BlueprintModelKind.None, Status = status };
    }

    /// <summary>
    /// Resolves the preview model for a blueprint document from its appearance field and the game-data
    /// lookup services, headlessly. Creatures whose appearance is a simple model (MODELTYPE S/F/W/L: the
    /// appearance.2da RACE column holds the literal model resref) resolve to a single resref; segmented
    /// player-body creatures (MODELTYPE P) resolve to a skeleton + body-part list following NWN's
    /// <c>p{gender}{race}{phenotype}</c> naming so the app can compose them with Radoub's MdlPartComposer.
    /// Placeables resolve through placeables.2da ModelName; doors through doortypes.2da Model.
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
            Func<string, bool>? partModelExists = null)
        {
            ArgumentNullException.ThrowIfNull(root);

            return type switch
            {
                ResourceType.Utc => ResolveCreature(root, appearances, itemBlueprintLoader, partModelExists),
                ResourceType.Utp => ResolvePlaceable(root, placeables),
                ResourceType.Utd => ResolveDoor(root, doors),
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
                Parts = parts
            };
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

            var chest = root.GetListOrEmpty("Equip_ItemList")
                .FirstOrDefault(item => ParseStructId(item.RawStructId) == ChestSlotStructId);
            var resRef = chest?.GetStringOrNull("EquippedRes");

            return string.IsNullOrWhiteSpace(resRef) ? null : itemBlueprintLoader(resRef);
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

        private static BlueprintModelReference ResolveDoor(JsonGffStruct root, DoorTypeService? doors)
        {
            if (doors == null)
                return BlueprintModelReference.NoneWith("Door preview unavailable (door-type data not loaded).");

            // 'Appearance' is always 0 for doors. The door type lives in GenericType_New (word) on
            // anything the module authored, and in GenericType (byte) on the base game's own doors -
            // BioWare added the wider field once the byte ran out of door types, and old blueprints kept
            // the old one. Reading only GenericType_New left all 86 base-game doors resolving nothing.
            var genericType = root.GetIntOrNull("GenericType_New")
                              ?? root.GetIntOrNull("GenericType")
                              ?? -1;
            var row = doors.GetAll().FirstOrDefault(r => r.Id == genericType);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown door type {genericType}.");

            if (string.IsNullOrWhiteSpace(row.Model))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: no model in doortypes.2da.");

            return new BlueprintModelReference
            {
                Kind = BlueprintModelKind.Simple,
                Status = $"{row.DisplayName} ({row.Model}.mdl)",
                ModelResRef = row.Model
            };
        }

        /// <summary>NWN body-part MDL naming: <c>{prefix}_{partType}{number:D3}</c>, e.g. <c>pmh0_chest001</c>.</summary>
        private static string BuildPartName(string prefix, string partType, int number) =>
            $"{prefix}_{partType}{number:D3}";
    }
}
