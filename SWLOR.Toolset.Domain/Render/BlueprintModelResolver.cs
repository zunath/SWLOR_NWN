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
        /// utc BodyPart_* field name → the MdlPartComposer / MdlPartBoneMap part-type key. Head is handled
        /// separately (its utc field is Appearance_Head, not a BodyPart_* field).
        /// </summary>
        private static readonly (string Field, string PartType)[] BodyPartFields =
        {
            ("BodyPart_Neck", "neck"),
            ("BodyPart_Torso", "chest"),
            ("BodyPart_Belt", "belt"),
            ("BodyPart_Pelvis", "pelvis"),
            ("BodyPart_LShoul", "shol"),
            ("BodyPart_RShoul", "shor"),
            ("BodyPart_LBicep", "bicepl"),
            ("BodyPart_RBicep", "bicepr"),
            ("BodyPart_LFArm", "forel"),
            ("BodyPart_RFArm", "forer"),
            ("BodyPart_LHand", "handl"),
            ("BodyPart_RHand", "handr"),
            ("BodyPart_LThigh", "legl"),
            ("BodyPart_RThigh", "legr"),
            ("BodyPart_LShin", "shinl"),
            ("BodyPart_RShin", "shinr"),
            ("BodyPart_LFoot", "footl"),
            ("BodyPart_RFoot", "footr"),
        };

        /// <summary>
        /// Resolves the preview model for a blueprint. Returns a <see cref="BlueprintModelKind.None"/>
        /// reference (never throws, never null) when the type is not previewable, a needed service is
        /// absent, or the appearance cannot be resolved.
        /// </summary>
        public static BlueprintModelReference Resolve(
            ResourceType type,
            JsonGffStruct root,
            AppearanceService? appearances,
            PlaceableAppearanceService? placeables,
            DoorTypeService? doors)
        {
            ArgumentNullException.ThrowIfNull(root);

            return type switch
            {
                ResourceType.Utc => ResolveCreature(root, appearances),
                ResourceType.Utp => ResolvePlaceable(root, placeables),
                ResourceType.Utd => ResolveDoor(root, doors),
                _ => BlueprintModelReference.NoneWith("No model preview for this blueprint type.")
            };
        }

        private static BlueprintModelReference ResolveCreature(JsonGffStruct root, AppearanceService? appearances)
        {
            if (appearances == null)
                return BlueprintModelReference.NoneWith("Creature preview unavailable (appearance data not loaded).");

            var appearanceId = root.GetIntOrNull("Appearance_Type") ?? -1;
            var row = appearances.GetAll().FirstOrDefault(r => r.Id == appearanceId);
            if (row == null)
                return BlueprintModelReference.NoneWith($"Unknown appearance id {appearanceId}.");

            if (string.Equals(row.ModelType, "P", StringComparison.OrdinalIgnoreCase))
                return ResolveSegmentedCreature(root, row);

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

        private static BlueprintModelReference ResolveSegmentedCreature(JsonGffStruct root, AppearanceRow row)
        {
            var raceLetter = row.Race;
            if (string.IsNullOrWhiteSpace(raceLetter))
                return BlueprintModelReference.NoneWith($"{row.DisplayName}: segmented appearance has no race letter.");

            // NWN player-body prefix: p{gender}{race}{phenotype}, e.g. "pmh0" (player, male, human, phenotype 0).
            var gender = (root.GetIntOrNull("Gender") ?? 0) == 1 ? 'f' : 'm';
            var phenotype = root.GetIntOrNull("Phenotype") ?? 0;
            var prefix = $"p{gender}{char.ToLowerInvariant(raceLetter[0])}{phenotype}";

            var parts = new List<BlueprintModelPart>();

            var head = root.GetIntOrNull("Appearance_Head");
            if (head is > 0)
                parts.Add(new BlueprintModelPart("head", BuildPartName(prefix, "head", head.Value)));

            foreach (var (field, partType) in BodyPartFields)
            {
                var number = root.GetIntOrNull(field);
                if (number is > 0)
                    parts.Add(new BlueprintModelPart(partType, BuildPartName(prefix, partType, number.Value)));
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

            // The corpus 'Appearance' field is always 0 for doors; the real door type is GenericType_New.
            var genericType = root.GetIntOrNull("GenericType_New") ?? -1;
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
