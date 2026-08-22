using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A cloak grafts at the composite root, not onto the skeleton's cloak bone.
    /// </summary>
    /// <remarks>
    /// A cloak part model carries its own copy of the chain it hangs from -
    /// <c>rootdummy &gt; torso_g &gt; Cloak_g</c> and the CL/CM/CR fan below it - so its geometry is
    /// already in absolute body space. Parenting it under the skeleton's own <c>Cloak_g</c> applies
    /// that chain twice and lifts the cape about a metre and a half clear of the wearer. Robes are
    /// authored the same way and already grafted at the root; cloaks now do too.
    /// </remarks>
    [TestFixture]
    [NonParallelizable]
    public class MdlPartComposerCloakTests
    {
        [Test]
        public void ACloakHangsOnTheBodyRatherThanAboveIt()
        {
            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
            {
                Assert.Ignore("the licensed base-game parts are required for a cloak");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(CorpusLocator.RepositoryRoot, "Build", "hakbuilder.json"),
                Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installRoot, "data")));

            MdlModel? Load(string resRef, bool isSkeleton)
            {
                if (!index.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out var handle))
                    return null;

                var model = new MdlReader().Parse(handle.GetBytes());
                if (!isSkeleton)
                    MdlGeometryFlattener.FlattenNodeTransforms(model);
                return model;
            }

            var parts = new List<(string, string)>
            {
                ("cloak", "pmh0_cloak_001"),
                ("head", "pmh0_head001"),
                ("chest", "pmh0_chest001"),
                ("pelvis", "pmh0_pelvis001"),
                ("footl", "pmh0_footl001"),
                ("footr", "pmh0_footr001"),
            };

            var model = new MdlPartComposer(Load).Compose("pmh0", parts, adjustSeams: true);
            model.Should().NotBeNull();

            var cloak = model!.GetMeshNodes()
                .Where(mesh => mesh.Bitmap?.Contains("cloak", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            cloak.Should().NotBeEmpty("the cloak part must contribute geometry");

            // Grafted at the root: the cloak's own copy of the body chain must not sit beneath the
            // skeleton's, which is what doubled its transform.
            foreach (var mesh in cloak)
            {
                var ancestors = new List<string>();
                for (MdlNode? node = mesh.Parent; node != null; node = node.Parent)
                    ancestors.Add(node.Name);

                ancestors.Should().NotContain(
                    name => name.Equals("Cloak_g", StringComparison.OrdinalIgnoreCase) &&
                            ancestors.Count(other => other.Equals("Cloak_g", StringComparison.OrdinalIgnoreCase)) > 1,
                    "the skeleton's cloak bone must not appear twice in a mesh's chain");
            }

            var pose = MdlAnimationPose.SampleIdleFrames(model, superModel => Load(superModel, true));
            var frame = pose.Count > 0 ? pose[^1].Pose : null;
            var heights = cloak
                .Select(mesh => MdlMeshBuilder.ComposeNodeTransform(mesh, frame).M43)
                .Where(height => height != 0f)
                .ToList();

            heights.Should().NotBeEmpty();
            heights.Max().Should().BeLessThan(1.9f,
                "a cape hangs from the shoulders, not above the head - double-transforming put it near 3m");
            heights.Min().Should().BeGreaterThan(0f, "the hem stays at or above the ground");
        }
    }
}
