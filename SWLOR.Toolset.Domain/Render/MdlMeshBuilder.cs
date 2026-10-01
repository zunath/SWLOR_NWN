// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Mdl;
using Nwn.Preview.Scene;

namespace SWLOR.Toolset.Domain.Render
{
    public static class MdlMeshBuilder
    {
        private const int MaximumNodes = 1_000_000;
        private const int MaximumParentDepth = 4_096;
        private const int MaximumEmitterGrid = 256;
        private static readonly HashSet<string> PlaceholderNames =
            new(StringComparer.OrdinalIgnoreCase) { "sam", "rootdummy" };

        /// <summary>
        /// Builds ordinary render geometry. Optional poses become a bounded sequence of per-mesh
        /// matrices; the final pose is the mesh's resting transform. A positive
        /// <paramref name="skinSurfaceClearance"/> expands deformed skinmeshes along their rendered
        /// normals, for garments authored as a shell over separately rendered body parts. Weights
        /// assigned to <paramref name="skinSurfaceClearanceExcludedBones"/> proportionally suppress
        /// that expansion where the garment replaces a concealed body segment.
        /// </summary>
        public static RenderModel Build(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames = null,
            float skinSurfaceClearance = 0f,
            IReadOnlySet<string>? skinSurfaceClearanceExcludedBones = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (!float.IsFinite(skinSurfaceClearance) || skinSurfaceClearance < 0f)
                throw new ArgumentOutOfRangeException(nameof(skinSurfaceClearance));
            var excludedBones = skinSurfaceClearanceExcludedBones?.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

            return BuildInternal(
                model,
                poseFrames,
                includePlaceableMetadata: false,
                includeDoorTransitionGeometry: false,
                skinSurfaceClearance: skinSurfaceClearance,
                skinSurfaceClearanceExcludedBones: excludedBones,
                sampledAnimations: null);
        }

        /// <summary>Builds creature geometry with a bounded set of sampled supermodel animations.</summary>
        public static RenderModel BuildAnimatedPreview(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames,
            IReadOnlyList<MdlAnimationPose.SampledAnimation> animations,
            float skinSurfaceClearance = 0f,
            IReadOnlySet<string>? skinSurfaceClearanceExcludedBones = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(animations);
            if (!float.IsFinite(skinSurfaceClearance) || skinSurfaceClearance < 0f)
                throw new ArgumentOutOfRangeException(nameof(skinSurfaceClearance));

            // Named clips own playback. Retain only the settled idle pose for static rendering and
            // bounds instead of uploading the same skinned idle frames a second time.
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? restingPoseFrames = poseFrames;
            if (poseFrames is { Count: > 1 })
                restingPoseFrames = [poseFrames[^1]];

            return BuildInternal(
                model,
                restingPoseFrames,
                includePlaceableMetadata: false,
                includeDoorTransitionGeometry: false,
                skinSurfaceClearance,
                skinSurfaceClearanceExcludedBones?.ToHashSet(StringComparer.OrdinalIgnoreCase),
                animations);
        }

        /// <summary>
        /// Builds geometry plus transform-only animation and persistent emitter metadata for the
        /// single-placeable preview.
        /// </summary>
        public static RenderModel BuildPlaceablePreview(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            return BuildInternal(
                model,
                poseFrames: null,
                includePlaceableMetadata: true,
                includeDoorTransitionGeometry: false,
                skinSurfaceClearance: 0f,
                skinSurfaceClearanceExcludedBones: null,
                sampledAnimations: null,
                includeInvisiblePlaceableGeometry: HasOnlyHiddenPlaceableGeometry(model));
        }

        /// <summary>Builds a placed prop, retaining an invisible object's authored selection shape.</summary>
        public static RenderModel BuildPlaceableEditor(
            MdlModel model, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            return BuildInternal(
                model, poseFrames, includePlaceableMetadata: false,
                includeDoorTransitionGeometry: false, skinSurfaceClearance: 0f,
                skinSurfaceClearanceExcludedBones: null, sampledAnimations: null,
                includeInvisiblePlaceableGeometry: HasOnlyHiddenPlaceableGeometry(model));
        }

        private static bool HasOnlyHiddenPlaceableGeometry(MdlModel model)
        {
            var nodes = EnumerateNodes(model.GeometryRoot).ToArray();
            // Effect models can contain hidden helper surfaces; they are not invisible objects.
            return !nodes.OfType<MdlEmitterNode>().Any() &&
                   !nodes.OfType<MdlTrimeshNode>().Any(IsRenderableMesh) &&
                   nodes.OfType<MdlTrimeshNode>().Any(mesh => !mesh.Render && IsEditorSelectionMesh(mesh));
        }

        /// <summary>
        /// Builds the authored editor geometry for a door row whose <c>VisibleModel</c> is zero.
        /// Those transition planes commonly put their selectable surface on <c>render 0</c> meshes:
        /// correct for the game, but not for an area editor where the otherwise invisible object has
        /// to remain visible and selectable.
        /// </summary>
        public static RenderModel BuildDoorTransition(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames = null)
        {
            ArgumentNullException.ThrowIfNull(model);
            return BuildInternal(
                model,
                poseFrames,
                includePlaceableMetadata: false,
                includeDoorTransitionGeometry: true,
                skinSurfaceClearance: 0f,
                skinSurfaceClearanceExcludedBones: null,
                sampledAnimations: null);
        }

        /// <summary>
        /// Whether a model contains an ASCII skinmesh with one influence row per vertex.
        /// </summary>
        /// <remarks>
        /// Such a model must retain its authored bone transforms through composition because they
        /// are the bind pose used by <see cref="Build"/>.
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

                var local = current.ReceivesNamedAnimationPose &&
                            pose != null &&
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
            bool includePlaceableMetadata,
            bool includeDoorTransitionGeometry,
            float skinSurfaceClearance,
            IReadOnlySet<string>? skinSurfaceClearanceExcludedBones,
            IReadOnlyList<MdlAnimationPose.SampledAnimation>? sampledAnimations,
            bool includeInvisiblePlaceableGeometry = false)
        {
            var sourceAnimations = includePlaceableMetadata
                ? MdlAnimationPose.PlaceableAnimations(model)
                : Array.Empty<MdlAnimation>();
            var animationSamples = sampledAnimations == null
                ? SampleAnimations(sourceAnimations)
                : sampledAnimations.ToDictionary(
                    animation => animation.Name,
                    animation => animation.Frames,
                    StringComparer.OrdinalIgnoreCase);
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
                if (!(includeDoorTransitionGeometry || includeInvisiblePlaceableGeometry
                        ? IsEditorSelectionMesh(mesh) : IsRenderableMesh(mesh)))
                    continue;

                var built = BuildMesh(
                    mesh,
                    poseFrames,
                    animationSamples,
                    skinSurfaceClearance,
                    skinSurfaceClearanceExcludedBones);
                if (built != null)
                    renderMeshes.Add(built);
            }

            var renderEmitters = includePlaceableMetadata
                ? emitterNodes.Select(node => BuildEmitter(node, animationSamples)).ToList()
                : new List<RenderEmitter>();
            var descriptors = sampledAnimations != null
                ? sampledAnimations.Select(animation =>
                    new AnimationDescriptor(animation.Name, animation.Length, false)).ToList()
                : sourceAnimations.Select(animation =>
                    new AnimationDescriptor(animation.Name, animation.Length, StateShowsEmitters(animation.Name))).ToList();
            var renderAnimations = BuildAnimations(descriptors, renderMeshes, renderEmitters);

            string? defaultAnimationName = null;
            if (includePlaceableMetadata)
            {
                defaultAnimationName = MdlAnimationPose.FindPlaceableDefault(model)?.Name;

                if (sourceAnimations.Count == 0 && renderEmitters.Count > 0)
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
            else if (sampledAnimations is { Count: > 0 })
            {
                defaultAnimationName = sampledAnimations[0].Name;
            }

            return new RenderModel
            {
                Name = model.Name,
                Meshes = renderMeshes,
                Animations = renderAnimations,
                Emitters = renderEmitters,
                DefaultAnimationName = defaultAnimationName,
                IsDoorTransitionGeometry = includeDoorTransitionGeometry,
                IsInvisiblePlaceableGeometry = includeInvisiblePlaceableGeometry && renderMeshes.Count > 0
            };
        }

        /// <summary>
        /// Whether a source trimesh produces one <see cref="RenderMesh"/> in <see cref="Build"/>.
        /// Consumers that retain metadata parallel to the built mesh list must use this predicate
        /// rather than approximating the builder's filtering by node name or geometry counts.
        /// </summary>
        public static bool IsRenderableMesh(MdlTrimeshNode mesh)
        {
            ArgumentNullException.ThrowIfNull(mesh);
            if (mesh.IsWalkmesh || !mesh.Render || PlaceholderNames.Contains(mesh.Name) ||
                mesh.Vertices.Length == 0 || mesh.Faces.Length == 0)
            {
                return false;
            }

            var vertexCount = mesh.Vertices.Length;
            return mesh.Faces.Any(face =>
                face.VertexIndex0 < vertexCount &&
                face.VertexIndex1 < vertexCount &&
                face.VertexIndex2 < vertexCount);
        }

        /// <summary>
        /// A transition door may expose its toolset selection surface with <c>render 0</c>. Keep
        /// those meshes while retaining the ordinary exclusions for collision and placeholder
        /// nodes; both rendered and non-rendered authored surfaces contribute to the editor shape.
        /// </summary>
        private static bool IsEditorSelectionMesh(MdlTrimeshNode mesh)
        {
            ArgumentNullException.ThrowIfNull(mesh);
            if (mesh.IsWalkmesh || PlaceholderNames.Contains(mesh.Name) ||
                mesh.Vertices.Length == 0 || mesh.Faces.Length == 0)
            {
                return false;
            }

            var vertexCount = mesh.Vertices.Length;
            return mesh.Faces.Any(face =>
                face.VertexIndex0 < vertexCount &&
                face.VertexIndex1 < vertexCount &&
                face.VertexIndex2 < vertexCount);
        }

        private static RenderMesh? BuildMesh(
            MdlTrimeshNode mesh,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>? poseFrames,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationSamples,
            float skinSurfaceClearance,
            IReadOnlySet<string>? skinSurfaceClearanceExcludedBones)
        {
            var vertexCount = mesh.Vertices.Length;
            if (vertexCount == 0 || mesh.Faces.Length == 0)
                return null;

            var skinnedPositions = Array.Empty<Vector3>();
            var skinnedNormals = Array.Empty<Vector3>();
            IReadOnlyList<float[]> skinnedPosePositions = Array.Empty<float[]>();
            IReadOnlyList<float[]> skinnedPoseNormals = Array.Empty<float[]>();
            var skin = mesh as MdlSkinmeshNode;
            var skinned = skin != null &&
                          TrySkinPoses(
                              skin,
                              poseFrames,
                              skinSurfaceClearance,
                              skinSurfaceClearanceExcludedBones,
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
            var animationPositions = new Dictionary<string, IReadOnlyList<float[]>>(
                StringComparer.OrdinalIgnoreCase);
            var animationNormals = new Dictionary<string, IReadOnlyList<float[]>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var (name, samples) in animationSamples)
            {
                animationFrames[name] = skinned
                    ? Enumerable.Repeat(Matrix4x4.Identity, samples.Count).ToArray()
                    : samples.Select(pose => ComposeNodeTransform(mesh, pose)).ToArray();
                if (skinned && TrySkinPoses(
                        skin!,
                        samples,
                        skinSurfaceClearance,
                        skinSurfaceClearanceExcludedBones,
                        out _,
                        out _,
                        out var positionFrames,
                        out var normalFrames))
                {
                    animationPositions[name] = positionFrames;
                    animationNormals[name] = normalFrames;
                }
            }

            return new RenderMesh
            {
                NodeName = mesh.Name,
                TextureName = NormalizeTextureName(mesh.Bitmap),
                MaterialName = NormalizeTextureName(mesh.MaterialName),
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
                AnimationPositions = animationPositions,
                AnimationNormals = animationNormals,
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
            float surfaceClearance,
            IReadOnlySet<string>? surfaceClearanceExcludedBones,
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
            {
                return TrySkinPose(
                    skin,
                    bones,
                    meshBind,
                    pose: null,
                    surfaceClearance,
                    surfaceClearanceExcludedBones,
                    out positions,
                    out normals);
            }

            var positionFrames = new List<float[]>(poseFrames.Count);
            var normalFrames = new List<float[]>(poseFrames.Count);
            foreach (var pose in poseFrames)
            {
                if (!TrySkinPose(
                        skin,
                        bones,
                        meshBind,
                        pose,
                        surfaceClearance,
                        surfaceClearanceExcludedBones,
                        out positions,
                        out normals))
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
            float surfaceClearance,
            IReadOnlySet<string>? surfaceClearanceExcludedBones,
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

                // The skin's authored hierarchy supplies both inverse bind and animated target.
                // Aurora renders weighted garments as their own visual, so substituting an outer
                // mannequin bone with the same name mixes two different parent chains and clips
                // the garment into the rigid body.
                var posed = pose == null
                    ? bind
                    : ComposeNodeTransform(bindBone, pose);
                transforms[name] = inverseBind * posed;
            }

            if (transforms.Count == 0)
                return false;

            positions = new Vector3[skin.Vertices.Length];
            var hasAuthoredNormals = skin.Normals.Length == skin.Vertices.Length;
            normals = hasAuthoredNormals ? new Vector3[skin.Normals.Length] : Array.Empty<Vector3>();
            var clearanceScales =
                surfaceClearance > 0f && surfaceClearanceExcludedBones is { Count: > 0 }
                    ? new float[skin.Vertices.Length]
                    : null;

            for (var index = 0; index < skin.Vertices.Length; index++)
            {
                var bindPosition = Vector3.Transform(FiniteOrZero(skin.Vertices[index]), meshBind);
                var bindNormal = hasAuthoredNormals
                    ? TransformDirection(FiniteOrZero(skin.Normals[index]), meshBind)
                    : Vector3.Zero;
                var position = Vector3.Zero;
                var normal = Vector3.Zero;
                var totalWeight = 0f;
                var excludedWeight = 0f;

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
                    if (hasAuthoredNormals)
                        normal += TransformDirection(bindNormal, boneTransform) * influence.Weight;
                    totalWeight += influence.Weight;
                    if (surfaceClearanceExcludedBones?.Contains(influence.BoneName) == true)
                        excludedWeight += influence.Weight;
                }

                if (totalWeight > 0f)
                {
                    position /= totalWeight;
                    if (hasAuthoredNormals)
                        normal /= totalWeight;
                }
                else
                {
                    position = bindPosition;
                    normal = bindNormal;
                }

                positions[index] = FiniteOrZero(position);
                if (hasAuthoredNormals)
                    normals[index] = NormalizeOrZero(normal);
                if (clearanceScales != null)
                {
                    clearanceScales[index] = totalWeight > 0f
                        ? 1f - Math.Clamp(excludedWeight / totalWeight, 0f, 1f)
                        : 1f;
                }
            }

            if (!hasAuthoredNormals)
                normals = GenerateVertexNormals(skin, positions);

            if (surfaceClearance > 0f && normals.Length == positions.Length)
            {
                for (var index = 0; index < positions.Length; index++)
                {
                    var scale = clearanceScales?[index] ?? 1f;
                    positions[index] = FiniteOrZero(
                        positions[index] + normals[index] * surfaceClearance * scale);
                }
            }

            return true;
        }

        /// <summary>
        /// Generates a safe fallback normal array for programmatically constructed skinmeshes.
        /// Parsed ASCII models already receive Aurora-compatible smoothing-group normals before
        /// their vertices are expanded, so production garments follow the authored face winding.
        /// </summary>
        private static Vector3[] GenerateVertexNormals(MdlTrimeshNode mesh, IReadOnlyList<Vector3> positions)
        {
            var normals = new Vector3[positions.Count];
            foreach (var face in mesh.Faces)
            {
                if (face.VertexIndex0 >= positions.Count ||
                    face.VertexIndex1 >= positions.Count ||
                    face.VertexIndex2 >= positions.Count)
                {
                    continue;
                }

                var a = positions[face.VertexIndex0];
                var b = positions[face.VertexIndex1];
                var c = positions[face.VertexIndex2];
                var faceNormal = Vector3.Cross(b - a, c - a);
                if (!IsFinite(faceNormal) || faceNormal.LengthSquared() <= 0f)
                    continue;

                normals[face.VertexIndex0] += faceNormal;
                normals[face.VertexIndex1] += faceNormal;
                normals[face.VertexIndex2] += faceNormal;
            }

            for (var index = 0; index < normals.Length; index++)
                normals[index] = NormalizeOrZero(normals[index]);
            return normals;
        }

        /// <summary>
        /// Finds the skinmesh's nearest complete authored bone hierarchy.
        /// </summary>
        /// <remarks>
        /// The nearest ancestor containing every weighted bone is authoritative. Its supermodel
        /// pose is applied by name to that same hierarchy; identically named nodes elsewhere in a
        /// composite model must not replace it.
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
                    return new SkinBones(indexed);
            }

            return SkinBones.Empty;
        }

        private static IReadOnlyDictionary<string, MdlNode> IndexNodes(MdlNode root)
        {
            var result = new Dictionary<string, MdlNode>(StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<MdlNode>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                if (!string.IsNullOrWhiteSpace(node.Name))
                    result.TryAdd(node.Name, node);

                // Push in reverse so traversal remains source-order and a skeleton parent wins
                // over any identically named mesh nested in a subsequently attached body part.
                for (var index = node.Children.Count - 1; index >= 0; index--)
                    pending.Push(node.Children[index]);
            }

            return result;
        }

        private readonly record struct SkinBones(IReadOnlyDictionary<string, MdlNode> Bind)
        {
            public static SkinBones Empty { get; } = new(
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

        private readonly record struct AnimationDescriptor(string Name, float Length, bool ShowsEmitters);

        private static List<RenderAnimation> BuildAnimations(
            IReadOnlyList<AnimationDescriptor> source,
            IReadOnlyList<RenderMesh> meshes,
            IReadOnlyList<RenderEmitter> emitters)
        {
            var result = new List<RenderAnimation>(source.Count);
            foreach (var animation in source)
            {
                var showsEmitters = emitters.Count > 0 && animation.ShowsEmitters;
                var movesGeometry = meshes.Any(mesh =>
                    mesh.AnimationFrames.TryGetValue(animation.Name, out var frames) &&
                    MatricesDiffer(frames) ||
                    mesh.AnimationPositions.TryGetValue(animation.Name, out var positions) &&
                    ArraysDiffer(positions));
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

        private static bool ArraysDiffer(IReadOnlyList<float[]> frames)
        {
            if (frames.Count < 2)
                return false;
            var first = frames[0];
            return frames.Skip(1).Any(frame => !frame.AsSpan().SequenceEqual(first));
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
