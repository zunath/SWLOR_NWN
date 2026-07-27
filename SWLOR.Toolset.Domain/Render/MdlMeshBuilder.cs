// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Renderable triangle data and transform metadata for one MDL mesh node.</summary>
    public sealed class RenderMesh
    {
        public string NodeName { get; init; } = string.Empty;
        public string TextureName { get; init; } = string.Empty;
        public float[] Positions { get; init; } = Array.Empty<float>();
        public float[] Normals { get; init; } = Array.Empty<float>();
        public float[] TexCoords { get; init; } = Array.Empty<float>();
        public int[] Indices { get; init; } = Array.Empty<int>();
        public Matrix4x4 Transform { get; init; } = Matrix4x4.Identity;
        public IReadOnlyList<Matrix4x4> PoseFrames { get; init; } = Array.Empty<Matrix4x4>();
        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The source mesh's signed Aurora <c>tilefade</c> value.</summary>
        public int TileFade { get; init; }

        public int VertexCount => Positions.Length / 3;
        public int TriangleCount => Indices.Length / 3;
    }

    /// <summary>A placeable preview animation exposed to the state picker and viewport.</summary>
    public sealed class RenderAnimation
    {
        public string Name { get; init; } = string.Empty;
        public float Length { get; init; }
        public bool ShowsEmitters { get; init; }
        public bool IsPlayable { get; init; }
    }

    /// <summary>Bounded emitter metadata used by the placeable preview's particle cue.</summary>
    public sealed class RenderEmitter
    {
        public string NodeName { get; init; } = string.Empty;
        public string TextureName { get; init; } = string.Empty;
        public Matrix4x4 Transform { get; init; } = Matrix4x4.Identity;
        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);
        public int XGrid { get; init; } = 1;
        public int YGrid { get; init; } = 1;
        public string Update { get; init; } = string.Empty;
        public string RenderMode { get; init; } = string.Empty;
        public string Blend { get; init; } = string.Empty;
        public string Chunk { get; init; } = string.Empty;
        public bool TextureIsTwoSided { get; init; }
        public bool Loop { get; init; }
        public ushort RenderOrder { get; init; }
        public float DeadSpace { get; init; }
        public float BlastRadius { get; init; }
        public float BlastLength { get; init; }
    }

    /// <summary>All renderable geometry and optional placeable-preview metadata for one MDL.</summary>
    public sealed class RenderModel
    {
        public string Name { get; init; } = string.Empty;
        public IReadOnlyList<RenderMesh> Meshes { get; init; } = Array.Empty<RenderMesh>();
        public IReadOnlyList<RenderAnimation> Animations { get; init; } = Array.Empty<RenderAnimation>();
        public IReadOnlyList<RenderEmitter> Emitters { get; init; } = Array.Empty<RenderEmitter>();
        public string? DefaultAnimationName { get; init; }
    }

    /// <summary>
    /// Converts parsed Aurora MDL nodes into compact arrays and transform metadata used by the
    /// software thumbnail renderer and OpenGL viewport.
    /// </summary>
    public static class MdlMeshBuilder
    {
        private const int MaximumNodes = 1_000_000;
        private const int MaximumParentDepth = 4_096;
        private const int MaximumEmitterGrid = 256;

        private static readonly HashSet<string> PlaceholderNames =
            new(StringComparer.OrdinalIgnoreCase) { "sam", "rootdummy" };

        /// <summary>
        /// Builds ordinary render geometry. Optional poses become a bounded sequence of per-mesh
        /// matrices; the final pose is the mesh's resting transform.
        /// </summary>
        public static RenderModel Build(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            return BuildInternal(model, poseFrames, includePlaceableMetadata: false);
        }

        /// <summary>
        /// Builds geometry plus transform-only animation and persistent emitter metadata for the
        /// single-placeable preview.
        /// </summary>
        public static RenderModel BuildPlaceablePreview(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return BuildInternal(model, poseFrames: null, includePlaceableMetadata: true);
        }

        /// <summary>
        /// Composes one node's local transform through its parent chain. A supplied pose replaces
        /// the corresponding node's authored local transform by name.
        /// </summary>
        public static Matrix4x4 ComposeNodeTransform(
            MdlNode node,
            IReadOnlyDictionary<string, PosedNode>? pose = null)
        {
            ArgumentNullException.ThrowIfNull(node);

            var result = Matrix4x4.Identity;
            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            MdlNode? current = node;
            var depth = 0;

            while (current != null)
            {
                if (!visited.Add(current))
                    throw new InvalidDataException("MDL parent chain contains a cycle.");
                if (++depth > MaximumParentDepth)
                    throw new InvalidDataException(
                        $"MDL parent chain exceeds the {MaximumParentDepth:N0}-level limit.");

                var local = pose != null &&
                            !string.IsNullOrEmpty(current.Name) &&
                            pose.TryGetValue(current.Name, out var posed)
                    ? LocalTransform(posed.Position, posed.Orientation, posed.Scale)
                    : LocalTransform(current.Position, current.Orientation, current.Scale);
                result *= local;
                current = current.Parent;
            }

            return result;
        }

        private static RenderModel BuildInternal(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames,
            bool includePlaceableMetadata)
        {
            var animations = includePlaceableMetadata
                ? MdlAnimationPose.PlaceableAnimations(model)
                : Array.Empty<MdlAnimation>();
            var animationSamples = SampleAnimations(animations);
            var emitterNodes = includePlaceableMetadata
                ? EnumerateNodes(model.GeometryRoot).OfType<MdlEmitterNode>().Where(IsPersistentEmitter).ToList()
                : new List<MdlEmitterNode>();

            var renderMeshes = new List<RenderMesh>();
            foreach (var mesh in EnumerateNodes(model.GeometryRoot).OfType<MdlTrimeshNode>())
            {
                if (!mesh.Render || PlaceholderNames.Contains(mesh.Name))
                    continue;

                var built = BuildMesh(mesh, poseFrames, animationSamples);
                if (built != null)
                    renderMeshes.Add(built);
            }

            var renderEmitters = includePlaceableMetadata
                ? emitterNodes.Select(node => BuildEmitter(node, animationSamples)).ToList()
                : new List<RenderEmitter>();
            var renderAnimations = includePlaceableMetadata
                ? BuildAnimations(animations, renderMeshes, renderEmitters)
                : new List<RenderAnimation>();

            string? defaultAnimationName = null;
            if (includePlaceableMetadata)
            {
                defaultAnimationName = MdlAnimationPose.FindPlaceableDefault(model)?.Name;

                if (animations.Count == 0 && renderEmitters.Count > 0)
                {
                    const string syntheticDefault = "default";
                    renderAnimations.Add(new RenderAnimation
                    {
                        Name = syntheticDefault,
                        Length = 1f,
                        ShowsEmitters = true,
                        IsPlayable = true
                    });
                    defaultAnimationName = syntheticDefault;
                }
            }

            return new RenderModel
            {
                Name = model.Name,
                Meshes = renderMeshes,
                Animations = renderAnimations,
                Emitters = renderEmitters,
                DefaultAnimationName = defaultAnimationName
            };
        }

        private static RenderMesh? BuildMesh(
            MdlTrimeshNode mesh,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationSamples)
        {
            var vertexCount = mesh.Vertices.Length;
            if (vertexCount == 0 || mesh.Faces.Length == 0)
                return null;

            var positions = new float[checked(vertexCount * 3)];
            for (var index = 0; index < vertexCount; index++)
            {
                var vertex = FiniteOrZero(mesh.Vertices[index]);
                positions[index * 3] = vertex.X;
                positions[index * 3 + 1] = vertex.Y;
                positions[index * 3 + 2] = vertex.Z;
            }

            var normals = mesh.Normals.Length == vertexCount
                ? Flatten(mesh.Normals.Select(FiniteOrZero))
                : Array.Empty<float>();
            var texCoords = mesh.TextureCoordinates.Length == vertexCount
                ? Flatten(mesh.TextureCoordinates.Select(FiniteOrZero))
                : Array.Empty<float>();

            var indices = new List<int>(checked(mesh.Faces.Length * 3));
            foreach (var face in mesh.Faces)
            {
                if (face.VertexIndex0 >= vertexCount ||
                    face.VertexIndex1 >= vertexCount ||
                    face.VertexIndex2 >= vertexCount)
                {
                    continue;
                }

                indices.Add(face.VertexIndex0);
                indices.Add(face.VertexIndex1);
                indices.Add(face.VertexIndex2);
            }

            if (indices.Count == 0)
                return null;

            var staticTransform = ComposeNodeTransform(mesh);
            var renderedPoseFrames = poseFrames == null
                ? Array.Empty<Matrix4x4>()
                : poseFrames.Select(pose => ComposeNodeTransform(mesh, pose)).ToArray();
            var animationFrames = new Dictionary<string, IReadOnlyList<Matrix4x4>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, samples) in animationSamples)
                animationFrames[name] = samples.Select(pose => ComposeNodeTransform(mesh, pose)).ToArray();

            return new RenderMesh
            {
                NodeName = mesh.Name,
                TextureName = NormalizeTextureName(mesh.Bitmap),
                Positions = positions,
                Normals = normals,
                TexCoords = texCoords,
                Indices = indices.ToArray(),
                Transform = renderedPoseFrames.Length > 0 ? renderedPoseFrames[^1] : staticTransform,
                PoseFrames = renderedPoseFrames,
                AnimationFrames = animationFrames,
                TileFade = mesh.TileFade
            };
        }

        private static RenderEmitter BuildEmitter(
            MdlEmitterNode emitter,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationSamples)
        {
            var animationFrames = new Dictionary<string, IReadOnlyList<Matrix4x4>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, samples) in animationSamples)
            {
                animationFrames[name] = samples
                    .Select(pose => ComposeNodeTransform(emitter, pose))
                    .ToArray();
            }

            return new RenderEmitter
            {
                NodeName = emitter.Name,
                TextureName = NormalizeTextureName(emitter.Texture),
                Transform = ComposeNodeTransform(emitter),
                AnimationFrames = animationFrames,
                XGrid = Math.Clamp(emitter.XGrid, 1, MaximumEmitterGrid),
                YGrid = Math.Clamp(emitter.YGrid, 1, MaximumEmitterGrid),
                Update = emitter.Update ?? string.Empty,
                RenderMode = emitter.RenderMode ?? string.Empty,
                Blend = emitter.Blend ?? string.Empty,
                Chunk = emitter.Chunk ?? string.Empty,
                TextureIsTwoSided = emitter.TextureIsTwoSided,
                Loop = emitter.Loop,
                RenderOrder = emitter.RenderOrder,
                DeadSpace = FiniteOr(emitter.DeadSpace, 0f),
                BlastRadius = FiniteOr(emitter.BlastRadius, 0f),
                BlastLength = FiniteOr(emitter.BlastLength, 0f)
            };
        }

        private static IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>>
            SampleAnimations(IReadOnlyList<MdlAnimation> animations)
        {
            var result =
                new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var animation in animations)
            {
                result[animation.Name] = MdlAnimationPose.SampleFrames(animation)
                    .Select(frame => frame.Pose)
                    .ToList();
            }

            return result;
        }

        private static List<RenderAnimation> BuildAnimations(
            IReadOnlyList<MdlAnimation> source,
            IReadOnlyList<RenderMesh> meshes,
            IReadOnlyList<RenderEmitter> emitters)
        {
            var result = new List<RenderAnimation>(source.Count);
            foreach (var animation in source)
            {
                var showsEmitters = emitters.Count > 0 && StateShowsEmitters(animation.Name);
                var movesGeometry = meshes.Any(mesh =>
                    mesh.AnimationFrames.TryGetValue(animation.Name, out var frames) &&
                    MatricesDiffer(frames));
                var movesEmitters = emitters.Any(emitter =>
                    emitter.AnimationFrames.TryGetValue(animation.Name, out var frames) &&
                    MatricesDiffer(frames));

                result.Add(new RenderAnimation
                {
                    Name = animation.Name,
                    Length = FiniteOr(animation.Length, 0f),
                    ShowsEmitters = showsEmitters,
                    IsPlayable = showsEmitters ||
                                 (animation.Length > 0f && (movesGeometry || movesEmitters))
                });
            }

            return result;
        }

        private static bool MatricesDiffer(IReadOnlyList<Matrix4x4> frames)
        {
            if (frames.Count < 2)
                return false;

            var first = frames[0];
            for (var index = 1; index < frames.Count; index++)
            {
                if (frames[index] != first)
                    return true;
            }

            return false;
        }

        private static bool StateShowsEmitters(string name) =>
            !string.Equals(name, "off", StringComparison.OrdinalIgnoreCase) &&
            !(name?.Contains("damage", StringComparison.OrdinalIgnoreCase) ?? false) &&
            !(name?.Contains("destroy", StringComparison.OrdinalIgnoreCase) ?? false) &&
            !(name?.Contains("dead", StringComparison.OrdinalIgnoreCase) ?? false);

        private static bool IsPersistentEmitter(MdlEmitterNode emitter) =>
            emitter.Loop &&
            !string.IsNullOrWhiteSpace(NormalizeTextureName(emitter.Texture)) &&
            !(emitter.Update?.Contains("explosion", StringComparison.OrdinalIgnoreCase) ?? false);

        /// <summary>
        /// Maps the Aurora "no texture" literal to an empty string and otherwise lowercases the
        /// name, matching the ASCII reader's own null/casing normalization so binary-authored
        /// <c>bitmap NULL</c>/<c>texture NULL</c> meshes are treated as untextured.
        /// </summary>
        private static string NormalizeTextureName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Equals("null", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return name.ToLowerInvariant();
        }

        private static IEnumerable<MdlNode> EnumerateNodes(MdlNode? root)
        {
            if (root == null)
                yield break;

            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<MdlNode>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL geometry exceeds the {MaximumNodes:N0}-node render limit.");

                yield return node;

                for (var index = node.Children.Count - 1; index >= 0; index--)
                {
                    var child = node.Children[index];
                    if (child != null)
                        pending.Push(child);
                }
            }
        }

        private static Matrix4x4 LocalTransform(Vector3 position, Quaternion orientation, float scale)
        {
            position = FiniteOrZero(position);
            orientation = IsFinite(orientation) && orientation.LengthSquared() > 0f
                ? Quaternion.Normalize(orientation)
                : Quaternion.Identity;
            scale = float.IsFinite(scale) ? scale : 1f;

            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateFromQuaternion(orientation) *
                   Matrix4x4.CreateTranslation(position);
        }

        private static float[] Flatten(IEnumerable<Vector3> values)
        {
            var result = new List<float>();
            foreach (var value in values)
            {
                result.Add(value.X);
                result.Add(value.Y);
                result.Add(value.Z);
            }

            return result.ToArray();
        }

        private static float[] Flatten(IEnumerable<Vector2> values)
        {
            var result = new List<float>();
            foreach (var value in values)
            {
                result.Add(value.X);
                result.Add(value.Y);
            }

            return result.ToArray();
        }

        private static Vector3 FiniteOrZero(Vector3 value) =>
            IsFinite(value) ? value : Vector3.Zero;

        private static Vector2 FiniteOrZero(Vector2 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) ? value : Vector2.Zero;

        private static float FiniteOr(float value, float fallback) => float.IsFinite(value) ? value : fallback;

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        private static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) &&
            float.IsFinite(value.Z) && float.IsFinite(value.W);
    }
}
