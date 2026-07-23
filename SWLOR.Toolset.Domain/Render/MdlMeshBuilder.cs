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
        /// Accumulated node-to-model transform: this node's own SRT composed with every ancestor
        /// up to (but not including) a transform for the model root itself. See
        /// <see cref="MdlMeshBuilder.ComposeNodeTransform"/>.
        /// </summary>
        public required Matrix4x4 Transform { get; init; }

        /// <summary>Vertex count, derived from <see cref="Positions"/>.</summary>
        public int VertexCount => Positions.Length / 3;

        /// <summary>Triangle count, derived from <see cref="Indices"/>.</summary>
        public int TriangleCount => Indices.Length / 3;
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
        public static RenderModel Build(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            var meshes = new List<RenderMesh>();

            if (model.GeometryRoot != null)
            {
                foreach (var node in model.EnumerateAllNodes())
                {
                    if (node is not MdlTrimeshNode trimesh)
                        continue; // Non-trimesh nodes (dummy, light, emitter, reference, ...) contribute no geometry.

                    if (!trimesh.Render)
                        continue;

                    if (trimesh.Vertices.Length == 0 || trimesh.Faces.Length == 0)
                        continue;

                    meshes.Add(BuildMesh(trimesh));
                }
            }

            return new RenderModel { Name = model.Name, Meshes = meshes };
        }

        private static RenderMesh BuildMesh(MdlTrimeshNode trimesh)
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
                Transform = ComposeNodeTransform(trimesh)
            };
        }

        /// <summary>
        /// Compose a node-to-model transform by walking from <paramref name="node"/> up through
        /// its parent chain, accumulating each ancestor's local Scale * Rotation * Translation
        /// (SRT, row-vector convention matching <see cref="System.Numerics.Matrix4x4"/>).
        /// Mirrors Radoub.UI's <c>ModelViewController.GetWorldTransform</c> (the App-layer GL
        /// renderer reused by the model preview) so both consumers place nodes identically.
        /// </summary>
        public static Matrix4x4 ComposeNodeTransform(MdlNode? node)
        {
            var transform = Matrix4x4.Identity;
            var current = node;

            while (current != null)
            {
                var scale = Matrix4x4.CreateScale(current.Scale);
                var rotation = Matrix4x4.CreateFromQuaternion(current.Orientation);
                var translation = Matrix4x4.CreateTranslation(current.Position);

                var local = scale * rotation * translation;
                transform *= local;

                current = current.Parent;
            }

            return transform;
        }
    }
}
