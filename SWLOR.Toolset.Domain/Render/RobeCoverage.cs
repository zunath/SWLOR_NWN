using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Domain.Render
{
    /// <summary>
    /// Decides from a robe model's actual geometry whether it replaces the body parts it covers.
    ///
    /// Classic NWN full robes are near-total bodies (own torso/arm/leg geometry); rendering the
    /// individual body parts alongside one duplicates geometry and z-fights, so those parts must
    /// be suppressed. But SWLOR's wardrobe is full of partial "robes" — loincloths, skirts,
    /// tabards (e.g. sw_pt_robe\pfh0_robe033.mdl, whose only renderable meshes span Z 0.38–1.24)
    /// — where suppressing would amputate the torso and limbs the robe doesn't actually cover.
    /// A fixed suppression list is therefore wrong in one direction or the other; this classifies
    /// per robe: only a robe whose renderable geometry spans essentially the whole body (ankles to
    /// shoulders) is treated as full-body.
    /// </summary>
    public static class RobeCoverage
    {
        /// <summary>Renderable robe geometry must reach below this Z (shin level) …</summary>
        public const float FullBodyMinZ = 0.5f;

        /// <summary>… and above this Z (shoulder level) to count as a full-body robe.</summary>
        public const float FullBodyMaxZ = 1.35f;

        /// <summary>
        /// True when the robe's renderable meshes span the full body vertically, meaning the body
        /// parts it covers should not be rendered alongside it. Expects a part-space model (the
        /// same orientation the part attaches with); node transforms are honored, so this works on
        /// both flattened and unflattened models. False for empty/unrenderable models.
        /// </summary>
        public static bool IsFullBodyRobe(MdlModel robeModel)
        {
            ArgumentNullException.ThrowIfNull(robeModel);

            if (robeModel.GeometryRoot == null)
                return false;

            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            var hasGeometry = false;

            Walk(robeModel.GeometryRoot, System.Numerics.Matrix4x4.Identity, ref minZ, ref maxZ, ref hasGeometry);

            return hasGeometry && minZ < FullBodyMinZ && maxZ > FullBodyMaxZ;
        }

        /// <summary>
        /// Roughly where each body part sits on a standing human, measured off a composed pmh0 in the
        /// idle pose (head 1.72m, shoulders 1.54m, chest 1.19m, feet 0.14m).
        /// </summary>
        private static readonly (string PartType, float Height)[] PartHeights =
        {
            ("chest", 1.19f),
            ("belt", 1.05f),
            ("pelvis", 1.00f),
            ("legl", 0.90f), ("legr", 0.90f),
            ("shinl", 0.50f), ("shinr", 0.50f),
            ("footl", 0.15f), ("footr", 0.15f),
            ("bicepl", 1.45f), ("bicepr", 1.45f),
            ("forel", 1.25f), ("forer", 1.25f),
            ("handl", 0.96f), ("handr", 0.96f),
            ("shol", 1.54f), ("shor", 1.54f),
        };

        /// <summary>
        /// The body parts this robe's own geometry actually reaches, and so replaces. Empty when the
        /// robe has no renderable geometry.
        /// </summary>
        /// <remarks>
        /// Coverage is measured rather than decided by a full-body/partial flag. That flag could only
        /// be wrong in one of two ways: a gown whose geometry stopped just short of the threshold left
        /// the armor's own torso showing through it, and treating every robe as full-length instead
        /// amputated the torso off short ones. Comparing the robe's vertical span against where each
        /// part sits answers both - a skirt covers hips and thighs, a gown covers those and the chest,
        /// and neither has to be classified in advance.
        /// </remarks>
        public static IReadOnlySet<string> CoveredParts(MdlModel robeModel)
        {
            ArgumentNullException.ThrowIfNull(robeModel);

            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (robeModel.GeometryRoot == null)
                return covered;

            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            var hasGeometry = false;
            Walk(robeModel.GeometryRoot, System.Numerics.Matrix4x4.Identity, ref minZ, ref maxZ, ref hasGeometry);
            if (!hasGeometry)
                return covered;

            foreach (var (partType, height) in PartHeights)
            {
                if (height >= minZ && height <= maxZ)
                    covered.Add(partType);
            }

            // Sleeves only exist on a robe that reaches the shoulders; a skirt spanning the arms'
            // height range does not cover them.
            if (maxZ <= FullBodyMaxZ)
            {
                foreach (var sleeve in new[] { "bicepl", "bicepr", "forel", "forer", "handl", "handr", "shol", "shor" })
                    covered.Remove(sleeve);
            }

            return covered;
        }

        private static void Walk(
            MdlNode node,
            System.Numerics.Matrix4x4 parentTransform,
            ref float minZ,
            ref float maxZ,
            ref bool hasGeometry)
        {
            var local = System.Numerics.Matrix4x4.CreateScale(node.Scale) *
                        System.Numerics.Matrix4x4.CreateFromQuaternion(node.Orientation) *
                        System.Numerics.Matrix4x4.CreateTranslation(node.Position);
            var world = local * parentTransform;

            if (node is MdlTrimeshNode mesh && mesh.Render && mesh.Vertices.Length > 0)
            {
                hasGeometry = true;
                foreach (var vertex in mesh.Vertices)
                {
                    var transformed = System.Numerics.Vector3.Transform(vertex, world);
                    if (transformed.Z < minZ) minZ = transformed.Z;
                    if (transformed.Z > maxZ) maxZ = transformed.Z;
                }
            }

            foreach (var child in node.Children)
                Walk(child, world, ref minZ, ref maxZ, ref hasGeometry);
        }
    }
}
