using System.Numerics;
using Radoub.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Bakes every node's composed model-root transform into its mesh vertex data and resets all
    /// node transforms to identity, so each mesh's vertices are expressed directly in the model's
    /// root space.
    ///
    /// Why this exists: body-part composition attaches a part file's meshes onto skeleton bones
    /// and assumes part geometry is authored at the part origin (Radoub's MdlPartComposer sets the
    /// attached mesh's Position to zero). BioWare's parts satisfy that, but several SWLOR hak parts
    /// (e.g. sw_pt_lthigh\pfh0_legl001.mdl, sw_pt_lshin\pfh0_shinl001.mdl) author vertices offset
    /// and correct them with node Positions inside the part file — transforms the composer
    /// discards, leaving those limbs floating away from the body. Flattening a part model first
    /// makes the at-origin assumption true for every part.
    ///
    /// Apply ONLY to models whose node transforms should no longer matter (composer part inputs):
    /// never to a skeleton (its node transforms are the bone positions) and never to models the
    /// renderer draws directly (it applies node world transforms itself — baking would double-
    /// transform). Animation vertex data (AnimatedVertices) is left untouched; static previews do
    /// not consume it.
    /// </summary>
    public static class MdlGeometryFlattener
    {
        public static void FlattenNodeTransforms(MdlModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (model.GeometryRoot == null)
                return;

            Flatten(model.GeometryRoot, Matrix4x4.Identity);
        }

        private static void Flatten(MdlNode node, Matrix4x4 parentTransform)
        {
            // Same composition order as Radoub's ModelViewController.GetWorldTransform:
            // local = scale × rotation × translation, composed child-to-root.
            var local = Matrix4x4.CreateScale(node.Scale) *
                        Matrix4x4.CreateFromQuaternion(node.Orientation) *
                        Matrix4x4.CreateTranslation(node.Position);
            var world = local * parentTransform;

            if (node is MdlTrimeshNode mesh && !world.IsIdentity)
            {
                for (var i = 0; i < mesh.Vertices.Length; i++)
                    mesh.Vertices[i] = Vector3.Transform(mesh.Vertices[i], world);

                for (var i = 0; i < mesh.Normals.Length; i++)
                {
                    var normal = Vector3.TransformNormal(mesh.Normals[i], world);
                    var length = normal.Length();
                    if (length > 1e-6f)
                        mesh.Normals[i] = normal / length;
                }
            }

            node.Position = Vector3.Zero;
            node.Orientation = Quaternion.Identity;
            node.Scale = 1f;

            foreach (var child in node.Children)
                Flatten(child, world);
        }
    }
}
