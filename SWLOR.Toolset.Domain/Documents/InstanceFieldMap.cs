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
                throw new ArgumentException("A selected ResRef is required.", nameof(templateResRef));

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

        /// <summary>
        /// The name this placement carries itself, or null when it has none and inherits its
        /// blueprint's.
        /// </summary>
        /// <remarks>
        /// Which field holds it depends on the list, verified against the corpus in Module\git:
        /// creatures split it across FirstName/LastName, placeables/doors/stores/sounds use
        /// "LocName", and waypoints/triggers/loose items use "LocalizedName".
        /// <para>
        /// Worth reading rather than going straight to the blueprint's name, because in this module
        /// the instance's own name is often the only thing separating two placements of the same
        /// blueprint: veles_exterior places <c>_mdrn_pl_carpt04</c> ("Rug, Maze (Brown/Cream)") 648
        /// times under 108 different names and 105 different appearances - as roads, lightposts and
        /// fences. Grouping those by blueprint would file all of them under the rug.
        /// </para>
        /// </remarks>
        public static string? GetDisplayName(ResourceType type, JsonGffStruct instance)
        {
            if (type == ResourceType.Utc)
            {
                var first = ReadLocString(instance, "FirstName");
                var last = ReadLocString(instance, "LastName");
                var full = $"{first} {last}".Trim();
                return full.Length == 0 ? null : full;
            }

            var text = ReadLocString(instance, NameFieldName(type));
            return text.Length == 0 ? null : text;
        }

        /// <summary>The cexolocstring field a non-creature instance keeps its own name in.</summary>
        private static string NameFieldName(ResourceType type)
        {
            return type switch
            {
                ResourceType.Utw or ResourceType.Utt or ResourceType.Uti => "LocalizedName",
                _ => "LocName"
            };
        }

        private static string ReadLocString(JsonGffStruct instance, string fieldName)
        {
            return instance.GetLocStringOrNull(fieldName)?.Text?.Trim() ?? string.Empty;
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
                    var (x, y) = NormalizeHeading(xOrientation, yOrientation);
                    instance.SetSingle("XOrientation", x);
                    instance.SetSingle("YOrientation", y);
                    break;
            }
        }

        /// <summary>
        /// Forces a heading to the (cos, sin) unit vector the rest of the pipeline assumes.
        /// </summary>
        /// <remarks>
        /// The Facing X/Y controls take two free numbers, and the viewport hides a bad one because atan2
        /// gives the same direction whatever the magnitude - so (1, 1) or (0, 0) looked fine here and
        /// reached the engine as a non-unit or degenerate heading. A zero vector has no direction to
        /// preserve, so it becomes due east, which is NWN's own default facing.
        /// </remarks>
        public static (float X, float Y) NormalizeHeading(float x, float y)
        {
            var length = MathF.Sqrt((x * x) + (y * y));
            return length < 1e-6f ? (1f, 0f) : (x / length, y / length);
        }

        /// <summary>
        /// Reads an instance's optional enhanced-edition VisualTransform. Rotations are stored in
        /// degrees; missing scale components mean 1 while missing rotation/translation components
        /// mean 0. The returned matrix is local to the instance and must be composed before its
        /// heading and world-position transforms.
        /// </summary>
        /// <remarks>
        /// Two storage shapes, both in the checked-in corpus. <c>VisualTransform</c> holds the components
        /// as plain floats. <c>VisTransformList</c> holds the same component names as structs, each with
        /// the value under <c>ValueTo</c> - the enhanced edition's animatable form. Reading only the first
        /// left 1,700+ placed objects at identity: dan_wildplain's _mdrn_pl_wdfence is scaled about 3.07
        /// through the list form alone, and rendered, picked and bounded at normal size.
        /// </remarks>
        public static Matrix4x4 GetVisualTransform(JsonGffStruct instance)
        {
            var component = ComponentReader(instance);
            if (component == null)
                return Matrix4x4.Identity;

            const float degreesToRadians = MathF.PI / 180f;
            var scale = new Vector3(
                component("ScaleX") ?? 1f,
                component("ScaleY") ?? 1f,
                component("ScaleZ") ?? 1f);
            var rotation = new Vector3(
                (component("RotateX") ?? 0f) * degreesToRadians,
                (component("RotateY") ?? 0f) * degreesToRadians,
                (component("RotateZ") ?? 0f) * degreesToRadians);
            var translation = new Vector3(
                component("TranslateX") ?? 0f,
                component("TranslateY") ?? 0f,
                component("TranslateZ") ?? 0f);

            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateRotationX(rotation.X) *
                   Matrix4x4.CreateRotationY(rotation.Y) *
                   Matrix4x4.CreateRotationZ(rotation.Z) *
                   Matrix4x4.CreateTranslation(translation);
        }

        /// <summary>
        /// A reader for one transform component, whichever shape the instance stores, or null when it
        /// stores no transform at all.
        /// </summary>
        private static Func<string, float?>? ComponentReader(JsonGffStruct instance)
        {
            if (instance.GetOrNull("VisualTransform")?.Struct is { } transform)
                return name => transform.GetSingleOrNull(name);

            var animated = instance.GetOrNull("VisTransformList")?.Elements?.FirstOrDefault();
            if (animated == null)
                return null;

            // Each component is a struct; the value that matters here is the one it settles on.
            return name => animated.GetOrNull(name)?.Struct?.GetSingleOrNull("ValueTo");
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
                // A loose item in an area is the whole .uti serialized inline under struct id 0,
                // rather than a slim instance pointing at a blueprint - which is why it also carries
                // its own PropertiesList and StackSize.
                ResourceType.Uti => 0,
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
                    or ResourceType.Utt or ResourceType.Utd or ResourceType.Utp
                    or ResourceType.Uti => DefaultExcludedBlueprintFields,
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
                case ResourceType.Uti:
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
            return CreateTriggerRectangle(2f, 2f);
        }

        /// <summary>
        /// The axis-aligned size of a placed trigger's local polygon. Returns zeroes for missing or
        /// malformed geometry so an editor can distinguish it from a usable shape.
        /// </summary>
        public static (float Width, float Height) GetTriggerGeometrySize(JsonGffStruct instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            var points = TriggerPoints(instance).ToList();
            if (points.Count < 3 || points.Count != TriggerGeometryElementCount(instance))
                return (0f, 0f);

            return (
                points.Max(point => point.X) - points.Min(point => point.X),
                points.Max(point => point.Y) - points.Min(point => point.Y));
        }

        /// <summary>
        /// Resizes a placed trigger's local polygon around its existing centre. Arbitrary polygons
        /// retain their shape; missing or degenerate geometry is repaired to a centred rectangle.
        /// </summary>
        public static void SetTriggerGeometrySize(JsonGffStruct instance, float width, float height)
        {
            ArgumentNullException.ThrowIfNull(instance);
            if (!float.IsFinite(width) || width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width), "Trigger width must be positive.");
            if (!float.IsFinite(height) || height <= 0f)
                throw new ArgumentOutOfRangeException(nameof(height), "Trigger height must be positive.");

            var points = TriggerPoints(instance).ToList();
            if (points.Count < 3 || points.Count != TriggerGeometryElementCount(instance))
            {
                ReplaceTriggerGeometry(instance, CreateTriggerRectangle(width, height));
                return;
            }

            var minX = points.Min(point => point.X);
            var maxX = points.Max(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxY = points.Max(point => point.Y);
            var oldWidth = maxX - minX;
            var oldHeight = maxY - minY;
            if (oldWidth <= float.Epsilon || oldHeight <= float.Epsilon)
            {
                ReplaceTriggerGeometry(instance, CreateTriggerRectangle(width, height));
                return;
            }

            var centerX = (minX + maxX) / 2f;
            var centerY = (minY + maxY) / 2f;
            var scaleX = width / oldWidth;
            var scaleY = height / oldHeight;
            foreach (var point in points)
            {
                point.Struct.SetSingle("PointX", centerX + (point.X - centerX) * scaleX);
                point.Struct.SetSingle("PointY", centerY + (point.Y - centerY) * scaleY);
            }
        }

        private static int TriggerGeometryElementCount(JsonGffStruct instance)
        {
            var geometry = instance.GetOrNull("Geometry");
            return geometry?.Type == GffFieldType.List ? geometry.Elements?.Count ?? 0 : 0;
        }

        private static IEnumerable<(JsonGffStruct Struct, float X, float Y)> TriggerPoints(JsonGffStruct instance)
        {
            var geometry = instance.GetOrNull("Geometry");
            if (geometry?.Type != GffFieldType.List || geometry.Elements == null)
                yield break;

            foreach (var point in geometry.Elements)
            {
                if (!point.TryGet("PointX", out var xField) ||
                    !point.TryGet("PointY", out var yField) ||
                    xField.Type != GffFieldType.Float ||
                    yField.Type != GffFieldType.Float)
                    continue;

                var x = xField.GetSingle();
                var y = yField.GetSingle();
                if (float.IsFinite(x) && float.IsFinite(y))
                    yield return (point, x, y);
            }
        }

        private static void ReplaceTriggerGeometry(JsonGffStruct instance, JsonGffField geometry)
        {
            instance.Remove("Geometry");
            instance.Add("Geometry", geometry);
        }

        private static JsonGffField CreateTriggerRectangle(float width, float height)
        {
            var geometry = JsonGffField.CreateList();
            var halfWidth = width / 2f;
            var halfHeight = height / 2f;
            foreach (var (x, y) in new[]
                     {
                         (-halfWidth, -halfHeight), (halfWidth, -halfHeight),
                         (halfWidth, halfHeight), (-halfWidth, halfHeight)
                     })
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
        public static JsonGffField CloneField(JsonGffField source)
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
