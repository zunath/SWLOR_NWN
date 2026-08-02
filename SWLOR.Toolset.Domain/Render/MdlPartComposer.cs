// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Composes Aurora segmented-creature body parts onto a player skeleton.
    /// </summary>
    /// <remarks>
    /// Models are loaded through the caller so resource precedence and supermodel policy stay outside
    /// this class. Source models are never mutated: every composition receives a deep geometry clone,
    /// attaches each part beneath its category's skeleton bone, and stamps the part resref onto its
    /// meshes so the normal Aurora part-texture convention can resolve it.
    /// </remarks>
    public sealed class MdlPartComposer
    {
        private const int MaximumParts = 256;
        private const int MaximumNodes = 1_000_000;
        private const int MaximumDepth = 4_096;
        private const float MinimumSeamOverlap = 0.10f;
        private const float ReferenceBodyHeight = 1.9f;

        // Lower joint first: moving the neck toward the chest establishes the target the head then
        // moves toward, so the second correction does not reopen the first seam.
        private static readonly (string Upper, string Lower)[] SeamPairs =
        [
            ("neck", "chest"),
            ("head", "neck")
        ];

        private readonly Func<string, bool, MdlModel?> _loadModel;
        private readonly Dictionary<(string ResRef, bool WithSupermodelAnimations), MdlModel?> _cache =
            new(ModelKeyComparer.Instance);

        public MdlPartComposer(Func<string, bool, MdlModel?> loadModel)
        {
            _loadModel = loadModel ?? throw new ArgumentNullException(nameof(loadModel));
        }

        /// <summary>Forgets source models after the mounted resource stack changes.</summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Loads and clones <paramref name="skeletonResRef"/>, then attaches every resolvable,
        /// supported part. Missing skeletons return null; missing, unknown, and malformed individual
        /// part entries are ignored so one absent cosmetic part does not suppress the whole preview.
        /// </summary>
        public MdlModel? Compose(
            string skeletonResRef,
            IEnumerable<(string PartType, string ModelResRef)> parts,
            bool adjustSeams = true)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(skeletonResRef);
            ArgumentNullException.ThrowIfNull(parts);

            var skeletonSource = Load(skeletonResRef, withSupermodelAnimations: true);
            if (skeletonSource?.GeometryRoot == null)
                return null;

            var composed = CloneModel(skeletonSource);
            if (composed.GeometryRoot == null)
                return null;

            var bones = IndexNodes(composed.GeometryRoot);
            var attachedParts = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
            var partCount = 0;
            foreach (var (partType, modelResRef) in parts)
            {
                if (++partCount > MaximumParts)
                    throw new InvalidDataException($"A composed MDL may contain at most {MaximumParts} parts.");
                if (string.IsNullOrWhiteSpace(partType) || string.IsNullOrWhiteSpace(modelResRef))
                    continue;

                var partSource = Load(modelResRef, withSupermodelAnimations: false);
                if (partSource?.GeometryRoot == null)
                    continue;

                // Robe and cloak geometry is authored in absolute body space, so both graft at the
                // composite root. Attach() applies no transform compensation, and parenting one under
                // a bone would double-transform it by that bone's chain: a cloak model carries its own
                // copy of rootdummy>torso_g>Cloak_g, so hanging it off the skeleton's Cloak_g lifted it
                // about a metre and a half clear of the body. Coverage-based body-part suppression
                // (hiding skin beneath a partial robe) is the caller's concern, not this decision.
                var bone = partType.Equals("robe", StringComparison.OrdinalIgnoreCase) ||
                           partType.Equals("cloak", StringComparison.OrdinalIgnoreCase)
                    ? composed.GeometryRoot
                    : FindBone(bones, partType);
                if (bone == null)
                    continue;

                var partRoot = CloneNodeTree(partSource.GeometryRoot);
                if (partRoot == null)
                    continue;

                StampPartTexture(partRoot, modelResRef);
                Attach(bone, partRoot);
                attachedParts.TryAdd(partType.Trim(), partRoot);
            }

            // A bare skeleton clone is not a usable composition: when every requested part is
            // missing, malformed, or unmapped, return null so the caller takes its no-model
            // fallback (the type symbol) instead of drawing an invisible ghost.
            if (attachedParts.Count == 0)
                return null;

            if (adjustSeams)
                AdjustSeamOverlaps(composed, attachedParts);

            RecalculateBounds(composed);
            return composed;
        }

        /// <summary>
        /// Merges independently positioned part models - a composite item's bottom/middle/top - under
        /// one synthetic root, with no skeleton, no seam correction, and no texture stamping: composite
        /// item parts are authored in a shared item space with their own bitmaps, so each part keeps
        /// its placement and textures as-is. Returns null when no part resolves to real geometry.
        /// </summary>
        public MdlModel? ComposeFlat(IEnumerable<string> partResRefs, string name)
        {
            ArgumentNullException.ThrowIfNull(partResRefs);

            var root = new MdlNode { Name = name };
            var composed = new MdlModel { Name = name, GeometryRoot = root };
            var attached = 0;
            var partCount = 0;

            foreach (var resRef in partResRefs)
            {
                if (++partCount > MaximumParts)
                    throw new InvalidDataException($"A composed MDL may contain at most {MaximumParts} parts.");
                if (string.IsNullOrWhiteSpace(resRef))
                    continue;

                var partSource = Load(resRef, withSupermodelAnimations: false);
                if (partSource?.GeometryRoot == null)
                    continue;

                var partRoot = CloneNodeTree(partSource.GeometryRoot);
                if (partRoot == null)
                    continue;

                Attach(root, partRoot);
                attached++;
            }

            if (attached == 0)
                return null;

            RecalculateBounds(composed);
            return composed;
        }

        private MdlModel? Load(string resRef, bool withSupermodelAnimations)
        {
            var key = (resRef.Trim(), withSupermodelAnimations);
            if (withSupermodelAnimations && _cache.TryGetValue(key, out var cached))
                return cached;

            MdlModel? loaded;
            try
            {
                loaded = _loadModel(key.Item1, withSupermodelAnimations);
            }
            catch (Exception)
            {
                loaded = null;
            }

            // Skeletons are shared across many creatures. Parts intentionally pass through the
            // caller on every compose run: BlueprintPreviewRenderer records each freshly loaded
            // part's authored bitmap so it can restore valid custom textures after our resref stamp.
            if (withSupermodelAnimations)
                _cache[key] = loaded;
            return loaded;
        }

        private static MdlNode? FindBone(
            IReadOnlyDictionary<string, MdlNode> bones,
            string partType)
        {
            foreach (var name in MdlPartBoneMap.GetBoneCandidates(partType))
            {
                if (bones.TryGetValue(name, out var bone))
                    return bone;
            }

            return null;
        }

        private static IReadOnlyDictionary<string, MdlNode> IndexNodes(MdlNode root)
        {
            var result = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<MdlNode>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL skeleton exceeds the {MaximumNodes:N0}-node limit.");

                if (!string.IsNullOrWhiteSpace(node.Name))
                    result.TryAdd(node.Name, node);

                for (var index = node.Children.Count - 1; index >= 0; index--)
                {
                    var child = node.Children[index];
                    if (child != null)
                        pending.Push(child);
                }
            }

            return result;
        }

        private static void Attach(MdlNode bone, MdlNode partRoot)
        {
            partRoot.Parent = bone;
            bone.Children.Add(partRoot);

            // Parts are independently authored trees. Reasserting all child Parent links keeps the
            // transform chain coherent even when an ASCII source omitted or disagreed with them; the
            // Children collection is the topology authority used by the standalone reader.
            RepairParentLinks(partRoot);
        }

        private static void AdjustSeamOverlaps(
            MdlModel model,
            IReadOnlyDictionary<string, MdlNode> attachedParts)
        {
            if (model.GeometryRoot == null ||
                !TryWorldBounds(model.GeometryRoot, out var modelMinimum, out var modelMaximum))
            {
                return;
            }

            var modelHeight = modelMaximum.Z - modelMinimum.Z;
            var requiredOverlap = float.IsFinite(modelHeight) && modelHeight > 0f
                ? modelHeight * (MinimumSeamOverlap / ReferenceBodyHeight)
                : MinimumSeamOverlap;

            foreach (var (upperType, lowerType) in SeamPairs)
            {
                if (!attachedParts.TryGetValue(upperType, out var upper) ||
                    !attachedParts.TryGetValue(lowerType, out var lower) ||
                    !TryWorldBounds(upper, out var upperMinimum, out var upperMaximum) ||
                    !TryWorldBounds(lower, out var lowerMinimum, out var lowerMaximum))
                {
                    continue;
                }

                var overlap = MathF.Min(upperMaximum.Z, lowerMaximum.Z) -
                              MathF.Max(upperMinimum.Z, lowerMinimum.Z);
                if (!float.IsFinite(overlap) || overlap >= requiredOverlap)
                    continue;

                MoveInWorld(upper, new Vector3(0f, 0f, -(requiredOverlap - overlap)));
            }
        }

        private static bool TryWorldBounds(
            MdlNode root,
            out Vector3 minimum,
            out Vector3 maximum)
        {
            minimum = new Vector3(float.MaxValue);
            maximum = new Vector3(float.MinValue);
            var found = false;
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<MdlNode>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL part exceeds the {MaximumNodes:N0}-node limit.");

                if (node is MdlTrimeshNode mesh && mesh.Render)
                {
                    var world = MdlMeshBuilder.ComposeNodeTransform(mesh);
                    foreach (var vertex in mesh.Vertices)
                    {
                        var transformed = Vector3.Transform(vertex, world);
                        if (!IsFinite(transformed))
                            continue;

                        minimum = Vector3.Min(minimum, transformed);
                        maximum = Vector3.Max(maximum, transformed);
                        found = true;
                    }
                }

                foreach (var child in node.Children)
                {
                    if (child != null)
                        pending.Push(child);
                }
            }

            return found;
        }

        private static void MoveInWorld(MdlNode root, Vector3 worldOffset)
        {
            var parentWorld = root.Parent == null
                ? Matrix4x4.Identity
                : MdlMeshBuilder.ComposeNodeTransform(root.Parent);
            if (!Matrix4x4.Invert(parentWorld, out var inverseParent))
                return;

            var localOffset = Vector3.TransformNormal(worldOffset, inverseParent);
            if (IsFinite(localOffset))
                root.Position += localOffset;
        }

        private static void RepairParentLinks(MdlNode root)
        {
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<MdlNode>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL part exceeds the {MaximumNodes:N0}-node limit.");

                foreach (var child in node.Children)
                {
                    if (child == null || visited.Contains(child))
                        continue;
                    child.Parent = node;
                    pending.Push(child);
                }
            }
        }

        private static void StampPartTexture(MdlNode root, string partResRef)
        {
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<MdlNode>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL part exceeds the {MaximumNodes:N0}-node limit.");

                if (node is MdlTrimeshNode mesh)
                    mesh.Bitmap = partResRef;

                foreach (var child in node.Children)
                {
                    if (child != null)
                        pending.Push(child);
                }
            }
        }

        private static MdlModel CloneModel(MdlModel source)
        {
            var clone = new MdlModel
            {
                Name = source.Name,
                SuperModel = source.SuperModel,
                ModelType = source.ModelType,
                BoundsMinimum = source.BoundsMinimum,
                BoundsMaximum = source.BoundsMaximum,
                Radius = source.Radius,
                Scale = source.Scale,
                GeometryRoot = source.GeometryRoot == null ? null : CloneNodeTree(source.GeometryRoot)
            };

            foreach (var animation in source.Animations)
            {
                clone.Animations.Add(new MdlAnimation
                {
                    Name = animation.Name,
                    Length = animation.Length,
                    TransitionTime = animation.TransitionTime,
                    GeometryRoot = animation.GeometryRoot == null ? null : CloneNodeTree(animation.GeometryRoot)
                });
            }

            return clone;
        }

        private static MdlNode? CloneNodeTree(MdlNode root) =>
            CloneNode(root, parent: null, new HashSet<MdlNode>(ReferenceEqualityComparer.Instance), 0, new NodeCounter());

        private static MdlNode? CloneNode(
            MdlNode source,
            MdlNode? parent,
            HashSet<MdlNode> visited,
            int depth,
            NodeCounter counter)
        {
            if (depth > MaximumDepth)
                throw new InvalidDataException($"MDL node depth exceeds the {MaximumDepth:N0}-level limit.");
            if (!visited.Add(source))
                return null;
            if (++counter.Value > MaximumNodes)
                throw new InvalidDataException($"MDL geometry exceeds the {MaximumNodes:N0}-node limit.");

            var clone = CreateNodeClone(source);
            clone.Parent = parent;
            CopyCommon(source, clone);

            foreach (var child in source.Children)
            {
                if (child == null)
                    continue;

                var childClone = CloneNode(child, clone, visited, depth + 1, counter);
                if (childClone != null)
                    clone.Children.Add(childClone);
            }

            return clone;
        }

        private static MdlNode CreateNodeClone(MdlNode source)
        {
            if (source is MdlSkinmeshNode skin)
            {
                var clone = new MdlSkinmeshNode
                {
                    VertexInfluences = skin.VertexInfluences
                        .Select(influences => influences?.ToArray() ?? Array.Empty<MdlSkinInfluence>())
                        .ToArray(),
                    BoneWeights = skin.BoneWeights.ToArray(),
                    BoneIndices = skin.BoneIndices.ToArray(),
                    BoneMapping = skin.BoneMapping.ToArray(),
                    BoneQuaternions = skin.BoneQuaternions.ToArray(),
                    BoneTranslations = skin.BoneTranslations.ToArray()
                };
                CopyMesh(skin, clone);
                return clone;
            }

            if (source is MdlTrimeshNode mesh)
            {
                var clone = new MdlTrimeshNode();
                CopyMesh(mesh, clone);
                return clone;
            }

            if (source is MdlEmitterNode emitter)
            {
                return new MdlEmitterNode
                {
                    DeadSpace = emitter.DeadSpace,
                    BlastRadius = emitter.BlastRadius,
                    BlastLength = emitter.BlastLength,
                    XGrid = emitter.XGrid,
                    YGrid = emitter.YGrid,
                    Update = emitter.Update,
                    RenderMode = emitter.RenderMode,
                    Blend = emitter.Blend,
                    Texture = emitter.Texture,
                    Chunk = emitter.Chunk,
                    TextureIsTwoSided = emitter.TextureIsTwoSided,
                    Loop = emitter.Loop,
                    RenderOrder = emitter.RenderOrder
                };
            }

            return new MdlNode();
        }

        private static void CopyCommon(MdlNode source, MdlNode target)
        {
            target.Name = source.Name;
            target.Position = source.Position;
            target.Orientation = source.Orientation;
            target.Scale = source.Scale;
            target.PositionTimes = source.PositionTimes.ToArray();
            target.PositionValues = source.PositionValues.ToArray();
            target.OrientationTimes = source.OrientationTimes.ToArray();
            target.OrientationValues = source.OrientationValues.ToArray();
            target.ScaleTimes = source.ScaleTimes.ToArray();
            target.ScaleValues = source.ScaleValues.ToArray();
        }

        private static void CopyMesh(MdlTrimeshNode source, MdlTrimeshNode target)
        {
            target.Render = source.Render;
            target.TileFade = source.TileFade;
            target.Bitmap = source.Bitmap;
            target.Lightmap = source.Lightmap;
            target.Diffuse = source.Diffuse;
            target.Vertices = source.Vertices.ToArray();
            target.Normals = source.Normals.ToArray();
            target.TextureCoordinates = source.TextureCoordinates.ToArray();
            target.Faces = source.Faces.Select(face => new MdlFace
            {
                Normal = face.Normal,
                Distance = face.Distance,
                SurfaceId = face.SurfaceId,
                VertexIndex0 = face.VertexIndex0,
                VertexIndex1 = face.VertexIndex1,
                VertexIndex2 = face.VertexIndex2
            }).ToArray();
        }

        private static void RecalculateBounds(MdlModel model)
        {
            if (model.GeometryRoot == null)
                return;

            var minimum = new Vector3(float.MaxValue);
            var maximum = new Vector3(float.MinValue);
            var radiusSquared = 0f;
            var found = false;
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<(MdlNode Node, Matrix4x4 Parent)>();
            pending.Push((model.GeometryRoot, Matrix4x4.Identity));

            while (pending.Count > 0)
            {
                var (node, parent) = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL composition exceeds the {MaximumNodes:N0}-node limit.");

                var local = Matrix4x4.CreateScale(FiniteOr(node.Scale, 1f)) *
                            Matrix4x4.CreateFromQuaternion(NormalizedOrIdentity(node.Orientation)) *
                            Matrix4x4.CreateTranslation(FiniteOrZero(node.Position));
                var world = local * parent;

                if (node is MdlTrimeshNode mesh && mesh.Render)
                {
                    foreach (var vertex in mesh.Vertices)
                    {
                        var transformed = Vector3.Transform(vertex, world);
                        if (!IsFinite(transformed))
                            continue;
                        minimum = Vector3.Min(minimum, transformed);
                        maximum = Vector3.Max(maximum, transformed);
                        radiusSquared = MathF.Max(radiusSquared, transformed.LengthSquared());
                        found = true;
                    }
                }

                foreach (var child in node.Children)
                {
                    if (child != null)
                        pending.Push((child, world));
                }
            }

            if (!found)
                return;

            model.BoundsMinimum = minimum;
            model.BoundsMaximum = maximum;
            model.Radius = MathF.Sqrt(radiusSquared);
        }

        private static float FiniteOr(float value, float fallback) => float.IsFinite(value) ? value : fallback;

        private static Vector3 FiniteOrZero(Vector3 value) => IsFinite(value) ? value : Vector3.Zero;

        private static Quaternion NormalizedOrIdentity(Quaternion value) =>
            IsFinite(value) && value.LengthSquared() > 0f ? Quaternion.Normalize(value) : Quaternion.Identity;

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        private static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) &&
            float.IsFinite(value.Z) && float.IsFinite(value.W);

        private sealed class NodeCounter
        {
            public int Value;
        }

        private sealed class ModelKeyComparer : IEqualityComparer<(string ResRef, bool WithSupermodelAnimations)>
        {
            public static readonly ModelKeyComparer Instance = new();

            public bool Equals(
                (string ResRef, bool WithSupermodelAnimations) x,
                (string ResRef, bool WithSupermodelAnimations) y) =>
                x.WithSupermodelAnimations == y.WithSupermodelAnimations &&
                string.Equals(x.ResRef, y.ResRef, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode((string ResRef, bool WithSupermodelAnimations) key) =>
                HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(key.ResRef),
                    key.WithSupermodelAnimations);
        }
    }
}
