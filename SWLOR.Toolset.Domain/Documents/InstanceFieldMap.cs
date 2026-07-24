using System.Globalization;
using System.Numerics;
using System.Text;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Builds a new placed-instance struct (an element to append/insert into a .git list, e.g.
    /// GitDocument.Creatures) from a blueprint document. The mapping was derived by diffing real
    /// corpus .git instances against their source blueprint, generalizing the approach
    /// SWLOR.CLI's StoreInstanceSync uses for stores to every placeable instance list this
    /// package supports:
    /// <list type="bullet">
    /// <item>Every blueprint field is copied onto the instance verbatim (same GFF type), except a
    /// small set of blueprint-only bookkeeping fields ("Comment"/"PaletteID", or "Comment"/"ID"
    /// for stores) that placed instances never carry.</item>
    /// <item>The instance list's corpus-verified "__struct_id" is stamped on the new struct.</item>
    /// <item>The position/orientation fields the instance shape requires - which the blueprint
    /// never carries - are added with the given placement. Creatures/waypoints/stores/triggers
    /// use XPosition/YPosition/ZPosition plus an XOrientation/YOrientation heading vector (with
    /// triggers also carrying a ZOrientation, always 0 in the corpus, and an instance-only
    /// "Geometry" point list, initialized to a usable 2m square around the placement);
    /// placeables/doors use X/Y/Z plus a single "Bearing" angle (radians,
    /// atan2(yOrientation, xOrientation), matching the corpus range of -pi..pi); ambient sounds
    /// use XPosition/YPosition/ZPosition plus an instance-only "GeneratedType" dword (always 0 in
    /// the corpus).</item>
    /// </list>
    /// </summary>
    public static class InstanceFieldMap
    {
        private static readonly HashSet<string> DefaultExcludedBlueprintFields =
            new(StringComparer.Ordinal) { "Comment", "PaletteID" };

        private static readonly HashSet<string> StoreExcludedBlueprintFields =
            new(StringComparer.Ordinal) { "Comment", "ID" };

        /// <summary>
        /// Creates a new placed-instance struct for <paramref name="type"/> from
        /// <paramref name="blueprint"/>, stamping <paramref name="templateResRef"/> as the
        /// blueprint it was selected from, and positioning it at (<paramref name="x"/>,
        /// <paramref name="y"/>, <paramref name="z"/>) with the given heading vector. The returned
        /// struct is detached - callers insert it into the target list field (e.g. via
        /// <c>JsonGffField.InsertElement</c>) inside their own DocumentTransaction.
        /// </summary>
        public static JsonGffStruct CreateInstance(
            ResourceType type,
            JsonGffDocument blueprint,
            string templateResRef,
            double x, double y, double z,
            double xOrientation = 1.0, double yOrientation = 0.0)
        {
            if (blueprint == null)
                throw new ArgumentNullException(nameof(blueprint));
            if (string.IsNullOrWhiteSpace(templateResRef))
                throw new ArgumentException("A selected blueprint resref is required.", nameof(templateResRef));

            var structId = GetListStructId(type);
            var excluded = GetExcludedBlueprintFields(type);

            var instance = JsonGffField.CreateStruct(structId).Struct!;

            foreach (var (name, field) in blueprint.Root.Entries)
            {
                if (excluded.Contains(name))
                    continue;

                instance.Add(name, CloneField(field));
            }

            // The file/palette selection is authoritative. Some legacy blueprints carry stale
            // internal TemplateResRef values that no longer match their file resref.
            instance.SetString(
                GetInstanceTemplateField(type),
                GffFieldType.ResRef,
                templateResRef);
            ApplyPlacement(type, instance, x, y, z, xOrientation, yOrientation);

            return instance;
        }

        /// <summary>The blueprint-resref field name for the given type: stores use "ResRef";
        /// every other supported type uses "TemplateResRef".</summary>
        public static string GetInstanceTemplateField(ResourceType type)
        {
            return type == ResourceType.Utm ? "ResRef" : "TemplateResRef";
        }

        /// <summary>The blueprint resref an instance struct was placed from.</summary>
        public static string? GetTemplateResRef(ResourceType type, JsonGffStruct instance)
        {
            return instance.GetStringOrNull(GetInstanceTemplateField(type));
        }

        /// <summary>Every supported instance list type carries a "Tag" field.</summary>
        public static string? GetTag(JsonGffStruct instance)
        {
            return instance.GetStringOrNull("Tag");
        }

        public static void SetTag(JsonGffStruct instance, string value)
        {
            instance.SetString("Tag", GffFieldType.CExoString, value);
        }

        /// <summary>Reads the instance's world position, normalizing the two corpus field
        /// schemes ("X"/"Y"/"Z" for placeables/doors, "XPosition"/"YPosition"/"ZPosition"
        /// otherwise) behind one shape-agnostic accessor.</summary>
        public static (float X, float Y, float Z) GetPosition(ResourceType type, JsonGffStruct instance)
        {
            var (xField, yField, zField) = PositionFieldNames(type);
            return (
                instance.GetSingleOrNull(xField) ?? 0f,
                instance.GetSingleOrNull(yField) ?? 0f,
                instance.GetSingleOrNull(zField) ?? 0f);
        }

        public static void SetPosition(ResourceType type, JsonGffStruct instance, float x, float y, float z)
        {
            var (xField, yField, zField) = PositionFieldNames(type);
            instance.SetSingle(xField, x);
            instance.SetSingle(yField, y);
            instance.SetSingle(zField, z);
        }

        /// <summary>
        /// Reads the instance's heading as an (x,y) vector, normalizing the corpus's two
        /// schemes: placeables/doors store a single "Bearing" angle (converted here to
        /// cos/sin); every other supported type stores XOrientation/YOrientation directly.
        /// Ambient sounds carry no heading at all - they always report the identity vector.
        /// </summary>
        public static (float XOrientation, float YOrientation) GetOrientation(ResourceType type, JsonGffStruct instance)
        {
            switch (type)
            {
                case ResourceType.Utd:
                case ResourceType.Utp:
                    var bearing = instance.GetSingleOrNull("Bearing") ?? 0f;
                    return ((float)Math.Cos(bearing), (float)Math.Sin(bearing));
                case ResourceType.Uts:
                    return (1f, 0f);
                default:
                    return (
                        instance.GetSingleOrNull("XOrientation") ?? 1f,
                        instance.GetSingleOrNull("YOrientation") ?? 0f);
            }
        }

        public static void SetOrientation(ResourceType type, JsonGffStruct instance, float xOrientation, float yOrientation)
        {
            switch (type)
            {
                case ResourceType.Utd:
                case ResourceType.Utp:
                    instance.SetSingle("Bearing", (float)Math.Atan2(yOrientation, xOrientation));
                    break;
                case ResourceType.Uts:
                    // Ambient sounds have no heading; nothing to set.
                    break;
                default:
                    instance.SetSingle("XOrientation", xOrientation);
                    instance.SetSingle("YOrientation", yOrientation);
                    break;
            }
        }

        /// <summary>
        /// Reads an instance's optional enhanced-edition VisualTransform. Rotations are stored in
        /// degrees; missing scale components mean 1 while missing rotation/translation components
        /// mean 0. The returned matrix is local to the instance and must be composed before its
        /// heading and world-position transforms.
        /// </summary>
        public static Matrix4x4 GetVisualTransform(JsonGffStruct instance)
        {
            if (instance.GetOrNull("VisualTransform")?.Struct is not { } transform)
                return Matrix4x4.Identity;

            const float degreesToRadians = MathF.PI / 180f;
            var scale = new Vector3(
                transform.GetSingleOrNull("ScaleX") ?? 1f,
                transform.GetSingleOrNull("ScaleY") ?? 1f,
                transform.GetSingleOrNull("ScaleZ") ?? 1f);
            var rotation = new Vector3(
                (transform.GetSingleOrNull("RotateX") ?? 0f) * degreesToRadians,
                (transform.GetSingleOrNull("RotateY") ?? 0f) * degreesToRadians,
                (transform.GetSingleOrNull("RotateZ") ?? 0f) * degreesToRadians);
            var translation = new Vector3(
                transform.GetSingleOrNull("TranslateX") ?? 0f,
                transform.GetSingleOrNull("TranslateY") ?? 0f,
                transform.GetSingleOrNull("TranslateZ") ?? 0f);

            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateRotationX(rotation.X) *
                   Matrix4x4.CreateRotationY(rotation.Y) *
                   Matrix4x4.CreateRotationZ(rotation.Z) *
                   Matrix4x4.CreateTranslation(translation);
        }

        private static (string X, string Y, string Z) PositionFieldNames(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utd or ResourceType.Utp => ("X", "Y", "Z"),
                _ => ("XPosition", "YPosition", "ZPosition")
            };
        }

        /// <summary>Deep-clones a placed instance struct (e.g. for a "Duplicate" command),
        /// sharing no mutable state with the source.</summary>
        public static JsonGffStruct Duplicate(JsonGffStruct source)
        {
            var structId = ParseStructId(source.RawStructId);
            var clone = JsonGffField.CreateStruct(structId).Struct!;
            CopyStructFields(source, clone);
            return clone;
        }

        private static uint GetListStructId(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utt => 1,
                ResourceType.Utc => 4,
                ResourceType.Utw => 5,
                ResourceType.Uts => 6,
                ResourceType.Utd => 8,
                ResourceType.Utp => 9,
                ResourceType.Utm => 11,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type), type, $"{type} does not have a supported placed-instance list struct id.")
            };
        }

        private static HashSet<string> GetExcludedBlueprintFields(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utm => StoreExcludedBlueprintFields,
                ResourceType.Utc or ResourceType.Utw or ResourceType.Uts
                    or ResourceType.Utt or ResourceType.Utd or ResourceType.Utp => DefaultExcludedBlueprintFields,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type), type, $"{type} is not a supported placed-instance list type.")
            };
        }

        private static void ApplyPlacement(
            ResourceType type, JsonGffStruct instance,
            double x, double y, double z, double xOrientation, double yOrientation)
        {
            switch (type)
            {
                case ResourceType.Utc:
                case ResourceType.Utw:
                case ResourceType.Utm:
                    instance.SetSingle("XPosition", (float)x);
                    instance.SetSingle("YPosition", (float)y);
                    instance.SetSingle("ZPosition", (float)z);
                    instance.SetSingle("XOrientation", (float)xOrientation);
                    instance.SetSingle("YOrientation", (float)yOrientation);
                    break;

                case ResourceType.Uts:
                    instance.SetSingle("XPosition", (float)x);
                    instance.SetSingle("YPosition", (float)y);
                    instance.SetSingle("ZPosition", (float)z);
                    instance.SetInt("GeneratedType", GffFieldType.Dword, 0);
                    break;

                case ResourceType.Utt:
                    instance.SetSingle("XPosition", (float)x);
                    instance.SetSingle("YPosition", (float)y);
                    instance.SetSingle("ZPosition", (float)z);
                    instance.SetSingle("XOrientation", (float)xOrientation);
                    instance.SetSingle("YOrientation", (float)yOrientation);
                    instance.SetSingle("ZOrientation", 0f);
                    if (!instance.Contains("Geometry"))
                        instance.Add("Geometry", CreateDefaultTriggerGeometry());
                    break;

                case ResourceType.Utd:
                case ResourceType.Utp:
                    instance.SetSingle("X", (float)x);
                    instance.SetSingle("Y", (float)y);
                    instance.SetSingle("Z", (float)z);
                    instance.SetSingle("Bearing", (float)Math.Atan2(yOrientation, xOrientation));
                    break;
            }
        }

        private static JsonGffField CreateDefaultTriggerGeometry()
        {
            var geometry = JsonGffField.CreateList();
            foreach (var (x, y) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                var point = JsonGffField.CreateStruct(3).Struct!;
                point.SetSingle("PointX", x);
                point.SetSingle("PointY", y);
                point.SetSingle("PointZ", 0.025f);
                geometry.InsertElement(geometry.Elements!.Count, point);
            }

            return geometry;
        }

        /// <summary>Deep-clones a field (recursively for struct/list children) so the new
        /// instance shares no mutable state with the source blueprint document.</summary>
        private static JsonGffField CloneField(JsonGffField source)
        {
            JsonGffField clone;
            switch (source.Type)
            {
                case GffFieldType.Struct:
                {
                    var structId = source.GetStructId() ?? ParseStructId(source.Struct?.RawStructId);
                    clone = JsonGffField.CreateStruct(structId);
                    CopyStructFields(source.Struct!, clone.Struct!);
                    break;
                }
                case GffFieldType.List:
                {
                    clone = JsonGffField.CreateList();
                    foreach (var element in source.Elements ?? new List<JsonGffStruct>())
                    {
                        var elementStructId = ParseStructId(element.RawStructId);
                        var clonedElement = JsonGffField.CreateStruct(elementStructId).Struct!;
                        CopyStructFields(element, clonedElement);
                        clone.InsertElement(clone.Elements!.Count, clonedElement);
                    }

                    break;
                }
                case GffFieldType.CExoLocString:
                {
                    clone = JsonGffField.CreateLocString();
                    foreach (var entry in source.LocStringEntries ?? new List<LocStringEntry>())
                        clone.AddLocStringEntry(new LocStringEntry(entry.LanguageKey, CloneBytes(entry.RawText)!));

                    break;
                }
                default:
                    clone = JsonGffField.CreateScalar(source.Type, CloneBytes(source.RawValue)!);
                    break;
            }

            clone.RawLocStringId = CloneBytes(source.RawLocStringId);
            return clone;
        }

        private static void CopyStructFields(JsonGffStruct source, JsonGffStruct target)
        {
            foreach (var (name, field) in source.Entries)
                target.Add(name, CloneField(field));
        }

        private static uint ParseStructId(byte[]? raw)
        {
            return raw == null ? 0u : uint.Parse(Encoding.ASCII.GetString(raw), CultureInfo.InvariantCulture);
        }

        private static byte[]? CloneBytes(byte[]? source)
        {
            return source == null ? null : (byte[])source.Clone();
        }
    }
}
