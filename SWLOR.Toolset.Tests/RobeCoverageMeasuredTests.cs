using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A robe replaces the body parts its geometry actually reaches, and no others.
    /// </summary>
    /// <remarks>
    /// The full-body/partial flag this replaces could only be wrong in one of two ways. A gown whose
    /// geometry stopped just short of the threshold suppressed nothing, so the armor's own torso
    /// showed through it - a corset visible inside the dress. Treating every robe as full-length
    /// instead amputated the torso off the short ones, which is what SWLOR's wardrobe is mostly made
    /// of: loincloths, skirts and tabards.
    /// </remarks>
    [TestFixture]
    [NonParallelizable]
    public class RobeCoverageMeasuredTests
    {
        private static ResourceIndex BuildIndex()
        {
            var installRoot = NwnInstallLocator.Locate();
            if (installRoot == null)
                Assert.Ignore("the licensed base-game robes are required");

            return ResourceIndex.FromHakBuilderConfig(
                Path.Combine(CorpusLocator.RepositoryRoot, "Build", "hakbuilder.json"),
                Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installRoot!, "data")));
        }

        private static MdlModel? Load(ResourceIndex index, string resRef) =>
            index.TryLookup(ResourceIdentity.FromFileName(resRef + ".mdl"), out var handle)
                ? new MdlReader().Parse(handle.GetBytes())
                : null;

        [Test]
        public void AShortRobeLeavesTheTorsoAlone()
        {
            var index = BuildIndex();

            // Scan the male robes for one that clearly does not reach the chest.
            for (var number = 1; number <= 20; number++)
            {
                var model = Load(index, $"pmh0_robe{number:D3}");
                if (model == null)
                    continue;

                var covered = RobeCoverage.CoveredParts(model);
                if (covered.Contains("chest"))
                    continue;

                // A robe that does not reach the chest must not claim the arms either.
                covered.Should().NotContain("bicepl");
                covered.Should().NotContain("handl");
                return;
            }

            Assert.Ignore("no short robe found among pmh0_robe001-020");
        }

        [Test]
        public void ARobeThatReachesTheChestReplacesIt()
        {
            var index = BuildIndex();

            for (var number = 1; number <= 20; number++)
            {
                var model = Load(index, $"pmh0_robe{number:D3}");
                if (model == null)
                    continue;

                var covered = RobeCoverage.CoveredParts(model);
                if (!covered.Contains("chest"))
                    continue;

                // Reaching the chest means it also covers what is below it.
                covered.Should().Contain("pelvis", "a robe over the chest also covers the hips");
                return;
            }

            Assert.Ignore("no full-length robe found among pmh0_robe001-020");
        }

        [Test]
        public void ARobeWithNoGeometryCoversNothing()
        {
            RobeCoverage.CoveredParts(new MdlModel()).Should().BeEmpty();
        }
    }
}
