// SPDX-License-Identifier: MIT

using System.Numerics;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Bakes an MDL geometry tree's local transforms into its mesh data.
    /// </summary>
    /// <remarks>
    /// Aurora MDL nodes use row-vector scale, rotation, then translation. A child's world matrix is
    /// therefore its local matrix multiplied by its parent's world matrix. Flattening is used for
    /// independently-authored body parts before they are attached to a creature skeleton.
    /// </remarks>
    public static class MdlGeometryFlattener
    {
        private const int MaximumNodes = 1_000_000;

        /// <summary>
        /// Transforms every mesh position and normal through its complete parent chain, then resets
        /// every visited node to an identity local transform.
        /// </summary>
        public static void FlattenNodeTransforms(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);
            if (model.GeometryRoot == null)
                return;

            var visited = new HashSet<MdlNode>(ReferenceEqualityComparer.Instance);
            var pending = new Stack<(MdlNode Node, Matrix4x4 ParentTransform)>();
            var minimum = new Vector3(float.MaxValue);
            var maximum = new Vector3(float.MinValue);
            var radiusSquared = 0f;
            var foundVertex = false;
            pending.Push((model.GeometryRoot, Matrix4x4.Identity));

            while (pending.Count > 0)
            {
                var (node, parentTransform) = pending.Pop();
                if (!visited.Add(node))
                    continue;
                if (visited.Count > MaximumNodes)
                    throw new InvalidDataException($"MDL geometry exceeds the {MaximumNodes:N0}-node flattening limit.");

                var worldTransform = LocalTransform(node) * parentTransform;
                if (node is MdlTrimeshNode mesh)
                {
                    BakeMesh(
                        mesh,
                        worldTransform,
                        ref minimum,
                        ref maximum,
                        ref radiusSquared,
                        ref foundVertex);
                }

                for (var index = node.Children.Count - 1; index >= 0; index--)
                {
                    var child = node.Children[index];
                    if (child != null)
                        pending.Push((child, worldTransform));
                }

                node.Position = Vector3.Zero;
                node.Orientation = Quaternion.Identity;
                node.Scale = 1f;
            }

            if (foundVertex)
            {
                model.BoundsMinimum = minimum;
                model.BoundsMaximum = maximum;
                model.Radius = MathF.Sqrt(radiusSquared);
            }
        }

        private static Matrix4x4 LocalTransform(MdlNode node)
        {
            var scale = float.IsFinite(node.Scale) ? node.Scale : 1f;
            var orientation = IsFinite(node.Orientation) && node.Orientation.LengthSquared() > 0f
                ? Quaternion.Normalize(node.Orientation)
                : Quaternion.Identity;
            var position = IsFinite(node.Position) ? node.Position : Vector3.Zero;

            return Matrix4x4.CreateScale(scale) *
                   Matrix4x4.CreateFromQuaternion(orientation) *
                   Matrix4x4.CreateTranslation(position);
        }

        private static void BakeMesh(
            MdlTrimeshNode mesh,
            Matrix4x4 transform,
            ref Vector3 minimum,
            ref Vector3 maximum,
            ref float radiusSquared,
            ref bool foundVertex)
        {
            for (var index = 0; index < mesh.Vertices.Length; index++)
            {
                var transformed = Vector3.Transform(mesh.Vertices[index], transform);
                mesh.Vertices[index] = transformed;
                if (!IsFinite(transformed))
                    continue;

                minimum = Vector3.Min(minimum, transformed);
                maximum = Vector3.Max(maximum, transformed);
                radiusSquared = MathF.Max(radiusSquared, transformed.LengthSquared());
                foundVertex = true;
            }

            for (var index = 0; index < mesh.Normals.Length; index++)
                mesh.Normals[index] = TransformNormal(mesh.Normals[index], transform);

            foreach (var face in mesh.Faces)
                face.Normal = TransformNormal(face.Normal, transform);
        }

        private static Vector3 TransformNormal(Vector3 normal, Matrix4x4 transform)
        {
            if (!IsFinite(normal))
                return Vector3.Zero;

            var transformed = Vector3.TransformNormal(normal, transform);
            return IsFinite(transformed) && transformed.LengthSquared() > 0f
                ? Vector3.Normalize(transformed)
                : Vector3.Zero;
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

        private static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) &&
            float.IsFinite(value.Z) && float.IsFinite(value.W);
    }
}
