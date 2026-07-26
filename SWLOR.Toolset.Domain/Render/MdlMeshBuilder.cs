// SPDX-License-Identifier: GPL-3.0-or-later
//
// The world-transform composition below mirrors Radoub.UI's ModelViewController.GetWorldTransform
// (https://github.com/LordOfMyatar/Radoub), which is GPL-3.0. That makes this file a derivative
// work: it is GPL-3.0 even though the rest of the SWLOR Toolset's own source is MIT. Dropping the
// Radoub reference would not change that - the transform order would have to be clean-roomed from
// the MDL format spec instead. See SWLOR.Toolset/LICENSE-NOTICE.md.
using System.Numerics;
using Radoub.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// A single renderable trimesh extracted from an <see cref="MdlModel"/>: flat vertex/index
    /// buffers in the mesh node's own local space, plus the accumulated node-to-model transform
    /// a consumer applies to place it (GL preview, area renderer, etc.). Vertex data is left
    /// untransformed so callers can bake it into a GPU instance matrix or a CPU-side transform
    /// as their scene graph requires.
    /// </summary>
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
        /// Per-state transforms for the placeable preview. Geometry remains shared; changing a state
        /// only changes which matrix is supplied for this mesh.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Vertex count, derived from <see cref="Positions"/>.</summary>
        public int VertexCount => Positions.Length / 3;

        /// <summary>Triangle count, derived from <see cref="Indices"/>.</summary>
        public int TriangleCount => Indices.Length / 3;
    }

    /// <summary>A model-declared placeable state exposed by the preview.</summary>
    public sealed record RenderAnimation(
        string Name,
        float Length,
        bool HasPosedNodes,
        bool ShowsEmitters)
    {
        /// <summary>Whether continuing to render this state can visibly change the preview.</summary>
        public bool IsPlayable => (Length > 0f && HasPosedNodes) || ShowsEmitters;
    }

    /// <summary>
    /// The subset of an MDL particle emitter needed for a lightweight editor preview. The game has a
    /// much richer particle simulation; this keeps the authored texture, sprite grid and node
    /// transform so effects such as portals and fires visibly move without changing area rendering.
    /// </summary>
    public sealed class RenderEmitter
    {
        public required string NodeName { get; init; }
        public required string TextureName { get; init; }
        public required Matrix4x4 Transform { get; init; }
        public required int XGrid { get; init; }
        public required int YGrid { get; init; }
        public required string Blend { get; init; }

        public IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> AnimationFrames { get; init; } =
            new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Render-ready mesh data for an entire <see cref="MdlModel"/>: one <see cref="RenderMesh"/>
    /// per visible trimesh node in the model's node hierarchy.
    /// </summary>
    public sealed class RenderModel
    {
        /// <summary>Source MDL model name.</summary>
        public required string Name { get; init; }

        /// <summary>Visible trimesh nodes, in the order they were encountered during traversal.</summary>
        public required IReadOnlyList<RenderMesh> Meshes { get; init; }

        /// <summary>Placeable animation states carried as transform-only tracks.</summary>
        public IReadOnlyList<RenderAnimation> Animations { get; init; } = Array.Empty<RenderAnimation>();

        /// <summary>The explicit state selected by the placeable preview rule.</summary>
        public string? DefaultAnimationName { get; init; }

        /// <summary>Particle emitters used only by the opt-in single-model preview.</summary>
        public IReadOnlyList<RenderEmitter> Emitters { get; init; } = Array.Empty<RenderEmitter>();
    }

    /// <summary>
    /// Builds <see cref="RenderModel"/> render data from a parsed <see cref="MdlModel"/> (as
    /// produced by <see cref="MdlReader"/>). Headless/Domain-level pipeline stage - no OpenGL or
    /// UI dependency - consumed later by both the GL model preview and the area renderer.
    /// </summary>
    public static class MdlMeshBuilder
    {
        /// <summary>
        /// Walk the model's node hierarchy and extract render-ready geometry for every trimesh
        /// node (including its Skin/Dangly/Anim/Aabb specializations, matching
        /// <see cref="MdlModel.GetMeshNodes"/>). Nodes with <c>Render == false</c>, or with no
        /// vertex/face data, are skipped for geometry but their transform still composes into any
        /// descendant's <see cref="RenderMesh.Transform"/> (transform composition walks the plain
        /// node/parent chain regardless of node type).
        /// </summary>
        /// <summary>
        /// Node names BioWare uses for geometry that is not artwork, and which must not be drawn.
        /// </summary>
        /// <remarks>
        /// These are placeholders that carry real, sizeable geometry and are flagged render=1, so nothing
        /// else here filters them. Drawing them puts a large untextured slab over the model: every base
        /// door model carries a <c>sam</c> node, and in TTU_udoor_06 it is 42 of the model's 196
        /// triangles - which is exactly the blank white panel that appeared across the palette's doors.
        /// <para>
        /// Matched by name rather than by "has no texture", because untextured is not the same as
        /// placeholder. Measured over 4,000 models in the resource stack, only 121 have any untextured
        /// rendered mesh, and those include real artwork - a gargoyle's wing parts among them - so
        /// dropping every untextured mesh would delete geometry that belongs on screen.
        /// </para>
        /// </remarks>
        private static readonly HashSet<string> PlaceholderNodeNames =
            new(StringComparer.OrdinalIgnoreCase) { "sam", "rootdummy" };

        private static bool IsPlaceholderNode(MdlTrimeshNode trimesh) =>
            PlaceholderNodeNames.Contains(trimesh.Name ?? string.Empty);

        private static readonly HashSet<string> EmitterOffStates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "off", "close", "closed", "deactivate", "deactivated", "dead", "die", "destroyed"
            };

        /// <summary>
        /// The lightweight preview can represent a persistent fountain loop, but not a one-shot
        /// explosion's birth-rate/lifespan controllers. Treating every emitter node as ambient made
        /// damage debris on the replacement model look like portal VFX that survived the scene swap.
        /// </summary>
        private static bool IsContinuousPreviewEmitter(MdlEmitterNode emitter) =>
            emitter.Loop &&
            emitter.Update.Equals("Fountain", StringComparison.OrdinalIgnoreCase);

        public static RenderModel Build(MdlModel model) => Build(model, pose: null);

        /// <summary>
        /// As <see cref="Build(MdlModel)"/>, but standing the model in <paramref name="pose"/> - the
        /// per-node local transforms sampled from an animation by <see cref="MdlAnimationPose"/>.
        /// </summary>
        /// <remarks>
        /// The pose replaces a node's own local transform wherever it names one; everything else keeps
        /// what the geometry declares. That is what lets one skeleton's idle pose carry a whole
        /// composed body: the parts hang off bones by name, so posing the bones moves the parts with
        /// them without the parts needing keyframes of their own.
        /// </remarks>
        public static RenderModel Build(MdlModel model, IReadOnlyDictionary<string, PosedNode>? pose) =>
            Build(model, pose == null ? Array.Empty<IReadOnlyDictionary<string, PosedNode>>() : new[] { pose });

        /// <summary>
        /// As <see cref="Build(MdlModel)"/>, carrying a transform per frame of an idle animation.
        /// </summary>
        /// <remarks>
        /// The mesh is built once and posed many times: NWN's bodies are rigid parts attached to bones,
        /// so a frame changes each mesh's transform and never a vertex. The geometry uploads once and
        /// playback swaps which matrix is bound. The last frame becomes <see cref="RenderMesh.Transform"/>,
        /// because that is where the animation stops and stays.
        /// </remarks>
        public static RenderModel Build(
            MdlModel model, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> poseFrames)
            => Build(
                model,
                poseFrames,
                new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>>(
                    StringComparer.OrdinalIgnoreCase),
                Array.Empty<RenderAnimation>(),
                defaultAnimationName: null,
                includeEmitters: false);

        /// <summary>
        /// Builds a placeable for the interactive preview: every declared state is sampled once into
        /// mesh transforms, and particle emitters are retained for the preview's lightweight effect
        /// pass. The ordinary area renderer never opts into these continuous tracks.
        /// </summary>
        public static RenderModel BuildPlaceablePreview(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var animationFrames =
                new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>>(
                    StringComparer.OrdinalIgnoreCase);
            var animations = new List<RenderAnimation>();
            var hasEmitters = model
                .EnumerateAllNodes()
                .OfType<MdlEmitterNode>()
                .Any(IsContinuousPreviewEmitter);

            foreach (var animation in MdlAnimationPose.PlaceableAnimations(model))
            {
                var frames = MdlAnimationPose.SampleFrames(animation);
                var poses = frames.Select(frame => frame.Pose).ToList();
                var hasPosedNodes = poses.Any(pose => pose.Count > 0);

                if (hasPosedNodes)
                    animationFrames[animation.Name] = poses;

                animations.Add(new RenderAnimation(
                    animation.Name,
                    MathF.Max(0f, animation.Length),
                    hasPosedNodes,
                    hasEmitters && !EmitterOffStates.Contains(animation.Name)));
            }

            // The SWLOR portal is emitter-driven and declares no MdlAnimation at all. A synthetic
            // default state gives that real authored loop the same play/pause surface as machinery.
            if (hasEmitters && animations.Count == 0)
                animations.Add(new RenderAnimation("default", 0f, false, true));

            var defaultAnimationName = MdlAnimationPose.FindPlaceableDefault(model)?.Name ??
                                       animations.FirstOrDefault()?.Name;

            return Build(
                model,
                Array.Empty<IReadOnlyDictionary<string, PosedNode>>(),
                animationFrames,
                animations,
                defaultAnimationName,
                includeEmitters: true);
        }

        private static RenderModel Build(
            MdlModel model,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> poseFrames,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationFrames,
            IReadOnlyList<RenderAnimation> animations,
            string? defaultAnimationName,
            bool includeEmitters)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(poseFrames);
            ArgumentNullException.ThrowIfNull(animationFrames);

            var pose = poseFrames.Count > 0 ? poseFrames[^1] : null;
            var meshes = new List<RenderMesh>();
            var emitters = new List<RenderEmitter>();

            if (model.GeometryRoot != null)
            {
                foreach (var node in model.EnumerateAllNodes())
                {
                    if (includeEmitters &&
                        node is MdlEmitterNode emitter &&
                        IsContinuousPreviewEmitter(emitter))
                    {
                        emitters.Add(BuildEmitter(emitter, animationFrames));
                        continue;
                    }

                    if (node is not MdlTrimeshNode trimesh)
                        continue; // Non-trimesh nodes (dummy, light, reference, ...) contribute no geometry.

                    if (!trimesh.Render)
                        continue;

                    if (IsPlaceholderNode(trimesh))
                        continue;

                    if (trimesh.Vertices.Length == 0 || trimesh.Faces.Length == 0)
                        continue;

                    meshes.Add(BuildMesh(trimesh, pose, poseFrames, animationFrames));
                }
            }

            return new RenderModel
            {
                Name = model.Name,
                Meshes = meshes,
                Animations = animations,
                DefaultAnimationName = defaultAnimationName,
                Emitters = emitters
            };
        }

        private static RenderMesh BuildMesh(
            MdlTrimeshNode trimesh,
            IReadOnlyDictionary<string, PosedNode>? pose,
            IReadOnlyList<IReadOnlyDictionary<string, PosedNode>> poseFrames,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationFrames)
        {
            var vertexCount = trimesh.Vertices.Length;

            var positions = new float[vertexCount * 3];
            for (var i = 0; i < vertexCount; i++)
            {
                var v = trimesh.Vertices[i];
                positions[i * 3] = v.X;
                positions[i * 3 + 1] = v.Y;
                positions[i * 3 + 2] = v.Z;
            }

            var normals = Array.Empty<float>();
            if (trimesh.Normals.Length == vertexCount)
            {
                normals = new float[vertexCount * 3];
                for (var i = 0; i < vertexCount; i++)
                {
                    var n = trimesh.Normals[i];
                    normals[i * 3] = n.X;
                    normals[i * 3 + 1] = n.Y;
                    normals[i * 3 + 2] = n.Z;
                }
            }

            var texCoords = Array.Empty<float>();
            if (trimesh.TextureCoords.Length > 0 && trimesh.TextureCoords[0].Length == vertexCount)
            {
                var uv0 = trimesh.TextureCoords[0];
                texCoords = new float[vertexCount * 2];
                for (var i = 0; i < vertexCount; i++)
                {
                    texCoords[i * 2] = uv0[i].X;
                    texCoords[i * 2 + 1] = uv0[i].Y;
                }
            }

            var indices = new int[trimesh.Faces.Length * 3];
            for (var f = 0; f < trimesh.Faces.Length; f++)
            {
                var face = trimesh.Faces[f];
                indices[f * 3] = face.VertexIndex0;
                indices[f * 3 + 1] = face.VertexIndex1;
                indices[f * 3 + 2] = face.VertexIndex2;
            }

            var bitmap = trimesh.Bitmap;
            var textureName = string.IsNullOrEmpty(bitmap) || bitmap.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : bitmap.ToLowerInvariant();

            return new RenderMesh
            {
                NodeName = trimesh.Name,
                TextureName = textureName,
                Positions = positions,
                Normals = normals,
                TexCoords = texCoords,
                Indices = indices,
                TileFade = trimesh.Tilefade,
                Transform = ComposeNodeTransform(trimesh, pose),
                PoseFrames = poseFrames.Count == 0
                    ? Array.Empty<Matrix4x4>()
                    : poseFrames.Select(frame => ComposeNodeTransform(trimesh, frame)).ToArray(),
                AnimationFrames = BuildAnimationTransforms(trimesh, animationFrames)
            };
        }

        private static RenderEmitter BuildEmitter(
            MdlEmitterNode emitter,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationFrames) =>
            new()
            {
                NodeName = emitter.Name,
                TextureName = emitter.Texture.Equals("null", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : emitter.Texture.ToLowerInvariant(),
                Transform = ComposeNodeTransform(emitter),
                XGrid = Math.Max(1, emitter.XGrid),
                YGrid = Math.Max(1, emitter.YGrid),
                Blend = emitter.Blend,
                AnimationFrames = BuildAnimationTransforms(emitter, animationFrames)
            };

        private static IReadOnlyDictionary<string, IReadOnlyList<Matrix4x4>> BuildAnimationTransforms(
            MdlNode node,
            IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, PosedNode>>> animationFrames)
        {
            var transforms = new Dictionary<string, IReadOnlyList<Matrix4x4>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, frames) in animationFrames)
                transforms[name] = frames.Select(frame => ComposeNodeTransform(node, frame)).ToArray();

            return transforms;
        }

        /// <summary>
        /// Compose a node-to-model transform by walking from <paramref name="node"/> up through
        /// its parent chain, accumulating each ancestor's local Scale * Rotation * Translation
        /// (SRT, row-vector convention matching <see cref="System.Numerics.Matrix4x4"/>).
        /// Mirrors Radoub.UI's <c>ModelViewController.GetWorldTransform</c> (the App-layer GL
        /// renderer reused by the model preview) so both consumers place nodes identically.
        /// </summary>
        public static Matrix4x4 ComposeNodeTransform(MdlNode? node) => ComposeNodeTransform(node, pose: null);

        /// <summary>
        /// As <see cref="ComposeNodeTransform(MdlNode)"/>, taking each ancestor's local transform from
        /// <paramref name="pose"/> where it names one. Posing has to happen here rather than on the
        /// finished mesh, because a bone's animation moves everything below it in the hierarchy.
        /// </summary>
        public static Matrix4x4 ComposeNodeTransform(MdlNode? node, IReadOnlyDictionary<string, PosedNode>? pose)
        {
            var transform = Matrix4x4.Identity;
            var current = node;

            while (current != null)
            {
                var local = pose != null && current.Name.Length > 0 && pose.TryGetValue(current.Name, out var posed)
                    ? posed
                    : new PosedNode(current.Position, current.Orientation, current.Scale);

                var scale = Matrix4x4.CreateScale(local.Scale);
                var rotation = Matrix4x4.CreateFromQuaternion(local.Orientation);
                var translation = Matrix4x4.CreateTranslation(local.Position);

                transform *= scale * rotation * translation;

                current = current.Parent;
            }

            return transform;
        }
    }
}
