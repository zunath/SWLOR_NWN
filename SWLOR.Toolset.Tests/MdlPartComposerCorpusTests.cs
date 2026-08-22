using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    [Category("LicensedCorpus")]
    public sealed class MdlPartComposerCorpusTests
    {
        private static string RepositoryRoot
        {
            get
            {
                for (var current = new DirectoryInfo(AppContext.BaseDirectory);
                     current != null;
                     current = current.Parent)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                        return current.FullName;
                }

                throw new DirectoryNotFoundException(
                    "Could not locate Build/hakbuilder.json and SWLOR_Haks from the test context.");
            }
        }

        [Test]
        public void FemaleHumanSkeleton_ComposesEverySupportedRealPartCategory()
        {
            var installRoot = NwnInstallLocator.Locate();
            installRoot.Should().NotBeNull("the explicit licensed-corpus suite requires NWN:EE");
            var baseLayer = KeyBifCatalog.Load(Path.Combine(installRoot!, "data"));
            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepositoryRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepositoryRoot, "SWLOR_Haks"),
                baseLayer);

            MdlModel? Load(string resRef, bool isSkeleton)
            {
                if (!index.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out var handle))
                    return null;

                var model = new MdlReader().Parse(handle.GetBytes());
                if (!isSkeleton)
                    MdlGeometryFlattener.FlattenNodeTransforms(model);
                return model;
            }

            var composer = new MdlPartComposer(Load);
            var parts = new[]
            {
                ("head", "pfh0_head001"),
                ("neck", "pfh0_neck001"),
                ("chest", "pfh0_chest001"),
                ("belt", "pfh0_belt006"),
                ("pelvis", "pfh0_pelvis006"),
                ("shol", "pfh0_shoL003"),
                ("shor", "pfh0_shoR003"),
                ("bicepl", "pfh0_bicepl001"),
                ("bicepr", "pfh0_bicepr001"),
                ("forel", "pfh0_forel001"),
                ("forer", "pfh0_forer001"),
                ("handl", "pfh0_handl001"),
                ("handr", "pfh0_handr001"),
                ("legl", "pfh0_legl001"),
                ("legr", "pfh0_legr001"),
                ("shinl", "pfh0_shinl001"),
                ("shinr", "pfh0_shinr001"),
                ("footl", "pfh0_footl001"),
                ("footr", "pfh0_footr001"),
                ("robe", "pfh0_robe007")
            };
            var model = composer.Compose(
                "pfh0",
                parts,
                adjustSeams: true);

            model.Should().NotBeNull();
            var partBitmaps = model!.GetMeshNodes()
                .Select(mesh => mesh.Bitmap)
                .Where(bitmap => !string.IsNullOrWhiteSpace(bitmap))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, resRef) in parts)
                partBitmaps.Should().Contain(resRef, $"the '{resRef}' part must attach to its mapped skeleton bone");

            AssertAttachedTo(model!, "pfh0_legl001g", "lthigh_g");
            AssertAttachedTo(model!, "pfh0_legr001g", "rthigh_g");
            AssertAttachedTo(model!, "Shin", "lshin_g");
            AssertAttachedTo(model!, "Shin", "rshin_g");
            model!.GetMeshNodes().Should().OnlyContain(mesh =>
                mesh.Vertices.All(IsFinite) && mesh.Normals.All(IsFinite));

            var render = MdlMeshBuilder.Build(model);
            render.Meshes.Should().NotBeEmpty();
            render.Meshes.Sum(mesh => mesh.Indices.Count()).Should().BeGreaterThan(0);

            AssertTheBodyStandsUp(model, superModel => Load(superModel, isSkeleton: true));
        }

        /// <summary>
        /// A composed body posed by its idle has to occupy a body's worth of space. Attaching every
        /// part to the right bone is not enough on its own: the idle poses those bones, and when the
        /// pose reported a blank position for each one the whole skeleton folded onto the origin and
        /// the model rendered as a single blob - parts correctly attached to bones that were all in
        /// the same place.
        /// </summary>
        private static void AssertTheBodyStandsUp(MdlModel model, Func<string, MdlModel?> loadSuperModel)
        {
            var frames = MdlAnimationPose.SampleIdleFrames(model, loadSuperModel);
            frames.Should().NotBeEmpty("the human skeleton's supermodel carries the idle");

            var pose = frames[^1].Pose;
            var heights = model.GetMeshNodes()
                .Select(mesh => MdlMeshBuilder.ComposeNodeTransform(mesh, pose).M43)
                .ToList();

            heights.Should().NotBeEmpty();
            (heights.Max() - heights.Min()).Should().BeGreaterThan(1.0f,
                "a posed humanoid spans well over a metre from foot to head, and collapsed to " +
                "roughly zero when the pose overwrote every bone position with a blank");
        }

        private static void AssertAttachedTo(MdlModel model, string meshName, string boneName)
        {
            var ancestorChains = model.GetMeshNodes()
                .Where(node => node.Name.Equals(meshName, StringComparison.OrdinalIgnoreCase))
                .Select(mesh =>
                {
                    var ancestors = new List<string>();
                    for (var current = mesh.Parent; current != null; current = current.Parent)
                        ancestors.Add(current.Name);
                    return ancestors;
                })
                .ToArray();

            ancestorChains.Should().NotBeEmpty($"the composed model should contain mesh '{meshName}'");
            ancestorChains.Should().Contain(
                ancestors => ancestors.Any(name =>
                    name.Equals(boneName, StringComparison.OrdinalIgnoreCase)),
                $"'{meshName}' must be attached below skeleton bone '{boneName}'");
        }

        private static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }
}
