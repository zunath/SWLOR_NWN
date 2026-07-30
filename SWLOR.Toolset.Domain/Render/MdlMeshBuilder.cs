// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>Renderable triangle data and transform metadata for one MDL mesh node.</summary>
    public sealed class RenderMesh
    {
        /// <summary>Source MDL node name (for diagnostics/debugging, not guaranteed unique).</summary>
        public required string NodeName { get; init; }

        /// <summary>
        /// Lowercased primary texture (bitmap) resref for this mesh, or empty when the mesh has
        /// no bitmap assigned (e.g. a "NULL" bitmap in the source MDL).
        /// </summary>
        public required string TextureName { get; init; }

        /// <summary>Vertex positions in node-local space, 3 floats (x, y, z) per vertex.</summary>
        public required float[] Positions { get; init; }

        /// <summary>
        /// Vertex normals, 3 floats (x, y, z) per vertex, parallel to <see cref="Positions"/>.
        /// Empty when the source mesh had no normal array or a mismatched count.
        /// </summary>
        public required float[] Normals { get; init; }

        /// <summary>
        /// Primary UV set, 2 floats (u, v) per vertex, parallel to <see cref="Positions"/>.
        /// Empty when the source mesh had no UV set or a mismatched count.
        /// </summary>
        public required float[] TexCoords { get; init; }

        /// <summary>Triangle face indices into the vertex arrays above, 3 ints per face.</summary>
        public required int[] Indices { get; init; }

        /// <summary>
        /// The node's MDL diffuse colour, which multiplies the texture rather than replacing it.
        /// White for the great majority of meshes, and for any that do not state one.
        /// </summary>
        /// <remarks>
        /// Carried because for some models it is the only colour there is. Every waypoint marker in
        /// the haks - the cyan flag, the orange one, the treasure chest - is drawn on
        /// <c>tcn01_white</c> and coloured entirely by this, so a pipeline that samples the texture
        /// alone renders the whole set as identical white shapes.
        /// </remarks>
        public Vector3 DiffuseColor { get; init; } = Vector3.One;

        /// <summary>
        /// The source node's MDL <c>tilefade</c> flag: 0 for geometry that is always drawn, non-zero
        /// for geometry the engine fades out when the camera would otherwise be looking through it.
        /// </summary>
        /// <remarks>
        /// This is how a tileset marks what is overhead. Every <c>ceilling*</c> node of the zsf01
        /// interior tiles carries tilefade 1, as does the high <c>treefol_01</c> canopy shell of the
        /// ttw01 forest tiles - and nothing at floor or wall height does. Aurora's area view drops all
        /// of it, which is why a builder can see into rooms from above and see the forest floor at all;
        /// see <c>GlAreaControl.ShowCeilings</c>.
        /// </remarks>
        public int TileFade { get; init; }

        /// <summary>
        /// Accumulated node-to-model transform: this node's own SRT composed with every ancestor
        /// up to (but not including) a transform for the model root itself. See
        /// <see cref="MdlMeshBuilder.ComposeNodeTransform"/>.
        /// </summary>
        public required Matrix4x4 Transform { get; init; }

        /// <summary>
        /// This mesh's node-to-model transform at each frame of the idle, or empty when the model has
        /// no idle to play. <see cref="Transform"/> is the last of them - where the animation comes to
        /// rest - so anything that wants the settled model rather than the playback uses that.
        /// </summary>
        public IReadOnlyList<Matrix4x4> PoseFrames { get; init; } = Array.Empty<Matrix4x4>();

        /// <summary>
        /// Model-space vertex positions for each idle frame of a skinned mesh. Empty for rigid
        /// meshes. Each entry is parallel to <see cref="Positions"/>, whose values remain the final
        /// resting frame used by still thumbnails, bounds, and non-animated draws.
        /// </summary>
        public IReadOnlyList<float[]> PosePositions { get; init; } = Array.Empty<float[]>();

        /// <summary>
        /// Model-space vertex normals parallel to <see cref="PosePositions"/>. A frame may be empty
        /// when the source skinmesh has no complete normal array.
        /// </summary>
        public IReadOnlyList<float[]> PoseNormals { get; init; } = Array.Empty<float[]>();

        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);

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

        /// <summary>
        /// The palette index each PLT layer is dyed with (skin, hair, metal, cloth, leather, tattoo),
        /// or empty for a model with no dyed textures.
        /// </summary>
        /// <remarks>
        /// Carried on the model rather than passed alongside it because a PLT is not a picture until
        /// its layers are coloured - a renderer that loads one without these gets the palette's
        /// default row, which is why the armor preview ignored every dye channel in the viewport
        /// while the 2D icon beside it honoured them.
        /// </remarks>
        public IReadOnlyDictionary<int, int> LayerColorIndices { get; init; } =
            new Dictionary<int, int>();

        /// <summary>
        /// The model's world-space bounding box at rest, or null when it has no drawable vertices.
        /// Uses each mesh's settled <see cref="RenderMesh.Transform"/> (the last idle frame), which
        /// is what the viewport draws when nothing is animating.
        /// </summary>
        public (Vector3 Minimum, Vector3 Maximum)? ComputeBounds()
        {
            var minimum = new Vector3(float.MaxValue);
            var maximum = new Vector3(float.MinValue);
            var found = false;

            foreach (var mesh in Meshes)
            {
                for (var vertex = 0; vertex < mesh.VertexCount; vertex++)
                {
                    var local = new Vector3(
                        mesh.Positions[vertex * 3],
                        mesh.Positions[vertex * 3 + 1],
                        mesh.Positions[vertex * 3 + 2]);
                    var world = Vector3.Transform(local, mesh.Transform);
                    if (!float.IsFinite(world.X) || !float.IsFinite(world.Y) || !float.IsFinite(world.Z))
                        continue;

                    minimum = Vector3.Min(minimum, world);
                    maximum = Vector3.Max(maximum, world);
                    found = true;
                }
            }

            return found ? (minimum, maximum) : null;
        }
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
        /// Whether a model contains an ASCII skinmesh with one influence row per vertex.
        /// </summary>
        /// <remarks>
        /// Such a model must retain its authored bone transforms through composition because they
        /// are the bind pose used by <see cref="Build(MdlModel, IReadOnlyList{IReadOnlyDictionary{string, PosedNode}}?)"/>.
        /// </remarks>
        public static bool ContainsNamedSkinWeights(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return EnumerateNodes(model.GeometryRoot)
                .OfType<MdlSkinmeshNode>()
                .Any(skin =>
                    skin.VertexInfluences.Length == skin.Vertices.Length &&
                    skin.VertexInfluences.Length > 0);
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
                // A collision node is the walkable surface, not artwork. It has to be excluded by its
                // own flag rather than by Render: ASCII MDL writes no "render" line for one, so it
                // arrives at the default of true, and it carries no bitmap - which drew it as a flat
                // grey slab across the ground of every tile that had one. The area view gets its
                // walkmesh from the tile's .wok (see TileWalkmeshCache), never from here.
                if (mesh.IsWalkmesh || !mesh.Render || PlaceholderNames.Contains(mesh.Name))
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

            var skinnedPositions = Array.Empty<Vector3>();
            var skinnedNormals = Array.Empty<Vector3>();
            IReadOnlyList<float[]> skinnedPosePositions = Array.Empty<float[]>();
            IReadOnlyList<float[]> skinnedPoseNormals = Array.Empty<float[]>();
            var skinned = mesh is MdlSkinmeshNode skin &&
                          TrySkinPoses(
                              skin,
                              poseFrames,
                              out skinnedPositions,
                              out skinnedNormals,
                              out skinnedPosePositions,
                              out skinnedPoseNormals);
            var positions = skinned
                ? Flatten(skinnedPositions)
                : Flatten(mesh.Vertices.Select(FiniteOrZero));
            var normals = skinned
                ? Flatten(skinnedNormals)
                : mesh.Normals.Length == vertexCount
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

            // Skin vertices have already been transformed into composed-model space at the idle's
            // resting frame. A single matrix cannot express their per-bone deformation, so their
            // transform stays identity.
            var staticTransform = skinned ? Matrix4x4.Identity : ComposeNodeTransform(mesh);
            var renderedPoseFrames = poseFrames == null
                ? Array.Empty<Matrix4x4>()
                : skinned
                    ? Enumerable.Repeat(Matrix4x4.Identity, poseFrames.Count).ToArray()
                    : poseFrames.Select(pose => ComposeNodeTransform(mesh, pose)).ToArray();
            var animationFrames = new Dictionary<string, IReadOnlyList<Matrix4x4>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, samples) in animationSamples)
            {
                animationFrames[name] = skinned
                    ? Enumerable.Repeat(Matrix4x4.Identity, samples.Count).ToArray()
                    : samples.Select(pose => ComposeNodeTransform(mesh, pose)).ToArray();
            }

            return new RenderMesh
            {
                NodeName = mesh.Name,
                TextureName = NormalizeTextureName(mesh.Bitmap),
                DiffuseColor = ReadDiffuse(mesh),
                Positions = positions,
                Normals = normals,
                TexCoords = texCoords,
                Indices = indices.ToArray(),
                Transform = renderedPoseFrames.Length > 0 ? renderedPoseFrames[^1] : staticTransform,
                PoseFrames = renderedPoseFrames,
                PosePositions = skinnedPosePositions,
                PoseNormals = skinnedPoseNormals,
                AnimationFrames = animationFrames,
                TileFade = mesh.TileFade
            };
        }

        /// <summary>
        /// Deforms an ASCII skinmesh into every requested pose, retaining the final one as rest.
        /// </summary>
        /// <remarks>
        /// Aurora stores a skin vertex in the skin node's bind space and names the bones that
        /// influence it. Rendering it as an ordinary trimesh leaves sleeves and coat panels in the
        /// arms-out bind pose while the segmented mannequin moves into its idle. The usual
        /// inverse-bind × posed-bone blend places each vertex in the same composed-model space as
        /// the rigid body parts.
        /// <para>
        /// Binary skinmeshes expose index/mapping arrays instead of bone names. Those continue
        /// through the rigid fallback until their index ordering can be resolved without guessing.
        /// SWLOR's robe and cloak parts are ASCII and carry the named influence rows used here.
        /// </para>
        /// </remarks>
        private static bool TrySkinPoses(
            MdlSkinmeshNode skin,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames,
            out Vector3[] positions,
            out Vector3[] normals,
            out IReadOnlyList<float[]> renderedPositions,
            out IReadOnlyList<float[]> renderedNormals)
        {
            positions = Array.Empty<Vector3>();
            normals = Array.Empty<Vector3>();
            renderedPositions = Array.Empty<float[]>();
            renderedNormals = Array.Empty<float[]>();

            if (skin.VertexInfluences.Length != skin.Vertices.Length ||
                skin.VertexInfluences.Length == 0)
            {
                return false;
            }

            var bones = FindSkinBones(skin);
            if (bones.Bind.Count == 0)
                return false;

            var meshBind = ComposeNodeTransform(skin);
            if (poseFrames is not { Count: > 0 })
                return TrySkinPose(skin, bones, meshBind, pose: null, out positions, out normals);

            var positionFrames = new List<float[]>(poseFrames.Count);
            var normalFrames = new List<float[]>(poseFrames.Count);
            foreach (var pose in poseFrames)
            {
                if (!TrySkinPose(skin, bones, meshBind, pose, out positions, out normals))
                    return false;

                positionFrames.Add(Flatten(positions));
                normalFrames.Add(normals.Length == 0 ? Array.Empty<float>() : Flatten(normals));
            }

            renderedPositions = positionFrames;
            renderedNormals = normalFrames;
            return true;
        }

        private static bool TrySkinPose(
            MdlSkinmeshNode skin,
            SkinBones bones,
            Matrix4x4 meshBind,
            IReadOnlyDictionary<string, PosedNode>? pose,
            out Vector3[] positions,
            out Vector3[] normals)
        {
            positions = Array.Empty<Vector3>();
            normals = Array.Empty<Vector3>();

            var transforms = new Dictionary<string, Matrix4x4>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, bindBone) in bones.Bind)
            {
                var bind = ComposeNodeTransform(bindBone);
                if (!Matrix4x4.Invert(bind, out var inverseBind))
                    continue;

                // The robe's private bone is inverse-bind data; the mannequin's corresponding
                // skeleton bone is the deformation target. They are not interchangeable. In
                // particular, the player skeleton carries shoulder nodes that several robe bind
                // skeletons omit. Re-applying the mannequin's local bicep/forearm pose through the
                // robe's shorter hierarchy drops those shoulder transforms and collapses sleeves
                // inward at the elbow.
                var posed = bones.Target.TryGetValue(name, out var targetBone)
                    ? ComposeNodeTransform(targetBone, pose)
                    : pose == null
                        ? bind
                        : ComposeNodeTransform(bindBone, pose);
                transforms[name] = inverseBind * posed;
            }

            if (transforms.Count == 0)
                return false;

            positions = new Vector3[skin.Vertices.Length];
            var hasNormals = skin.Normals.Length == skin.Vertices.Length;
            normals = hasNormals ? new Vector3[skin.Normals.Length] : Array.Empty<Vector3>();

            for (var index = 0; index < skin.Vertices.Length; index++)
            {
                var bindPosition = Vector3.Transform(FiniteOrZero(skin.Vertices[index]), meshBind);
                var bindNormal = hasNormals
                    ? TransformDirection(FiniteOrZero(skin.Normals[index]), meshBind)
                    : Vector3.Zero;
                var position = Vector3.Zero;
                var normal = Vector3.Zero;
                var totalWeight = 0f;

                foreach (var influence in skin.VertexInfluences[index])
                {
                    if (!float.IsFinite(influence.Weight) ||
                        influence.Weight <= 0f ||
                        string.IsNullOrWhiteSpace(influence.BoneName) ||
                        !transforms.TryGetValue(influence.BoneName, out var boneTransform))
                    {
                        continue;
                    }

                    position += Vector3.Transform(bindPosition, boneTransform) * influence.Weight;
                    if (hasNormals)
                        normal += TransformDirection(bindNormal, boneTransform) * influence.Weight;
                    totalWeight += influence.Weight;
                }

                if (totalWeight > 0f)
                {
                    position /= totalWeight;
                    if (hasNormals)
                        normal /= totalWeight;
                }
                else
                {
                    position = bindPosition;
                    normal = bindNormal;
                }

                positions[index] = FiniteOrZero(position);
                if (hasNormals)
                    normals[index] = NormalizeOrZero(normal);
            }

            return true;
        }

        /// <summary>
        /// Finds the robe's nearest complete bind skeleton and the matching bones on the composed
        /// mannequin outside that subtree.
        /// </summary>
        /// <remarks>
        /// A robe model carries a private copy of the bones needed to interpret its weighted
        /// vertices. Those bones define the inverse bind matrices only. Once the robe is grafted
        /// into a segmented body, Aurora deforms it toward the outer mannequin skeleton, whose
        /// hierarchy can include joints (notably shoulders) absent from the robe copy.
        /// </remarks>
        private static SkinBones FindSkinBones(MdlSkinmeshNode skin)
        {
            var required = skin.VertexInfluences
                .SelectMany(row => row)
                .Where(influence =>
                    influence.Weight > 0f &&
                    !string.IsNullOrWhiteSpace(influence.BoneName))
                .Select(influence => influence.BoneName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (required.Count == 0)
                return SkinBones.Empty;

            for (var scope = skin.Parent; scope != null; scope = scope.Parent)
            {
                var indexed = IndexNodes(scope);
                if (required.All(indexed.ContainsKey))
                {
                    var modelRoot = scope;
                    while (modelRoot.Parent != null)
                        modelRoot = modelRoot.Parent;

                    var target = ReferenceEquals(modelRoot, scope)
                        ? new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase)
                        : IndexNodes(modelRoot, scope);
                    return new SkinBones(indexed, target);
                }
            }

            return SkinBones.Empty;
        }

        private static IReadOnlyDictionary<string, MdlNode> IndexNodes(
            MdlNode root,
            MdlNode? excludedSubtree = null)
        {
            var result = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<MdlNode>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (ReferenceEquals(node, excludedSubtree))
                    continue;

                if (!string.IsNullOrWhiteSpace(node.Name))
                    result.TryAdd(node.Name, node);

                // Push in reverse so traversal remains source-order and a skeleton parent wins
                // over any identically named mesh nested in a subsequently attached body part.
                for (var index = node.Children.Count - 1; index >= 0; index--)
                    pending.Push(node.Children[index]);
            }

            return result;
        }

        private readonly record struct SkinBones(
            IReadOnlyDictionary<string, MdlNode> Bind,
            IReadOnlyDictionary<string, MdlNode> Target)
        {
            public static SkinBones Empty { get; } = new(
                new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase));
        }

        private static Vector3 TransformDirection(Vector3 value, Matrix4x4 transform) =>
            FiniteOrZero(Vector3.TransformNormal(value, transform));

        private static Vector3 NormalizeOrZero(Vector3 value) =>
            IsFinite(value) && value.LengthSquared() > 0f
                ? Vector3.Normalize(value)
                : Vector3.Zero;

        /// <summary>
        /// The node's diffuse colour, preserved verbatim - including explicit black.
        /// </summary>
        /// <remarks>
        /// The corpus authors black on purpose: <c>jr_lab_sink.mdl</c> carries rendered
        /// <c>diffuse 0 0 0</c> geometry, and armor parts state black over their textures.
        /// An unstated diffuse is already white (<see cref="MdlTrimeshNode.Diffuse"/> defaults to
        /// <see cref="Vector3.One"/>), and non-rendered helper planes never reach this method
        /// because <see cref="BuildMesh"/> skips <c>render 0</c> nodes, so no substitution for
        /// zero is needed or correct.
        /// </remarks>
        private static Vector3 ReadDiffuse(MdlTrimeshNode trimesh) => trimesh.Diffuse;

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

        private static readonly string[] EmitterOffStates =
        {
            // A placeable in any of these states is inert: a closed door of a portal, a
            // deactivated device, a dying fire. Persistent particles must not keep playing.
            "off", "close", "closed", "deactivate", "deactivated", "die", "dead", "destroyed"
        };

        private static bool StateShowsEmitters(string name) =>
            !EmitterOffStates.Any(state => string.Equals(name, state, StringComparison.OrdinalIgnoreCase)) &&
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
