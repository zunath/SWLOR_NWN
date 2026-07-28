using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="RobeCoverage"/> — the geometry-driven decision of whether a robe
    /// replaces the body parts it covers. Motivating corpus case: sw_pt_robe\pfh0_robe033.mdl is a
    /// loincloth (renderable meshes span only Z 0.38–1.24); treating it as a full-body robe
    /// amputated the wearer's torso and limbs in the preview (visual gate, 2026-07-21).
    /// </summary>
    public class RobeCoverageTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                        return current.FullName;
                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        [Test]
        public void LoinclothRobe_FromCorpus_IsNotFullBody()
        {
            var path = Path.Combine(RepoRoot, "SWLOR_Haks", "sw_pt_robe", "pfh0_robe033.mdl");
            var model = new MdlReader().Parse(File.ReadAllBytes(path));

            RobeCoverage.IsFullBodyRobe(model).Should().BeFalse(
                "its only renderable meshes (loincloth front/back, belt) span Z 0.38–1.24 — nowhere near ankles-to-shoulders");
        }

        [Test]
        public void SyntheticFullBodyRobe_IsFullBody()
        {
            // One renderable mesh spanning ankles (0.2) to shoulders (1.6).
            var model = ModelWithMesh(render: true, new Vector3(0, 0, 0.2f), new Vector3(0.3f, 0.1f, 1.6f));

            RobeCoverage.IsFullBodyRobe(model).Should().BeTrue();
        }

        [Test]
        public void RenderDisabledGeometry_DoesNotCount()
        {
            // Full-body span but render=false (rigging/reference meshes) -> not a full-body robe.
            var model = ModelWithMesh(render: false, new Vector3(0, 0, 0.2f), new Vector3(0.3f, 0.1f, 1.6f));

            RobeCoverage.IsFullBodyRobe(model).Should().BeFalse();
        }

        [Test]
        public void NodeTransforms_AreHonored()
        {
            // Mesh authored at the origin but lifted to full-body span by its node position —
            // matches how unflattened part files carry placement in node transforms.
            var model = ModelWithMesh(render: true, new Vector3(0, 0, -0.7f), new Vector3(0.3f, 0.1f, 0.7f));
            var mesh = (MdlTrimeshNode)model.GeometryRoot!.Children[0];
            mesh.Position = new Vector3(0, 0, 0.95f); // spans 0.25..1.65 in part space

            RobeCoverage.IsFullBodyRobe(model).Should().BeTrue();
        }

        private static MdlModel ModelWithMesh(bool render, Vector3 min, Vector3 max)
        {
            var mesh = new MdlTrimeshNode
            {
                Name = "m",
                Render = render,
                Position = Vector3.Zero,
                Orientation = Quaternion.Identity,
                Scale = 1f,
                Vertices = new[] { min, max },
            };
            var root = new MdlNode { Name = "root", Position = Vector3.Zero, Orientation = Quaternion.Identity, Scale = 1f };
            root.Children.Add(mesh);
            mesh.Parent = root;

            return new MdlModel { Name = "synthetic_robe", GeometryRoot = root };
        }
    }
}
