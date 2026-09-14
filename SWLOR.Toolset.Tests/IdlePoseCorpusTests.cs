using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Real creature models resolve a real idle pose.
    /// </summary>
    /// <remarks>
    /// The premise the whole feature rests on, and it is not obvious: creature models carry no
    /// animations at all - pmh0, pfh0 and c_anurog all report zero. The animations live in a shared
    /// supermodel, so a pose can only be found by following the chain. Asserting against the shipped
    /// models rather than a fixture, because it is the shape of the real data that decides this.
    /// </remarks>
    public class IdlePoseCorpusTests
    {
        private static string RepoRoot
        {
            get
            {
                var current = new DirectoryInfo(AppContext.BaseDirectory);
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(current.FullName, "SWLOR_Haks")))
                    {
                        return current.FullName;
                    }

                    current = current.Parent;
                }

                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private static ResourceIndex? BuildIndex()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
                return null;

            var dataDirectory = Path.Combine(installPath, "data");
            if (!File.Exists(Path.Combine(dataDirectory, "nwn_base.key")))
                return null;

            return ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(dataDirectory));
        }

        /// <summary>
        /// A humanoid skeleton and a beast both pose, and neither carries the animation itself.
        /// pfh0 is the case that needs two links: a_fa's pause1 is an empty declaration deferring to
        /// a_fa_int, so a walk that stopped at the first idle it found would pose nothing.
        /// </summary>
        [TestCase("pmh0", 40)]
        [TestCase("pfh0", 40)]
        [TestCase("c_anurog", 10)]
        public void ACreatureModelPosesThroughItsSupermodelChain(string resRef, int minimumPosedNodes)
        {
            var index = BuildIndex();
            if (index == null)
            {
                Assert.Ignore("No local NWN:EE installation was found; skipping.");
                return;
            }

            var reader = new MdlReader();

            MdlModel? Load(string name)
            {
                var identity = new ResourceIdentity(name, ResourceIdentity.TypeFromExtension("mdl"));
                return index.TryLookup(identity, out var handle) ? reader.Parse(handle.GetBytes()) : null;
            }

            var model = Load(resRef);
            if (model == null)
            {
                Assert.Ignore($"'{resRef}' did not resolve; skipping.");
                return;
            }

            model.Animations.Should().BeEmpty(
                "creature models carry no animations of their own - that is why the chain has to be walked");

            var posed = MdlAnimationPose.SampleIdle(model, Load);

            posed.Count.Should().BeGreaterThanOrEqualTo(minimumPosedNodes,
                "the idle has to come from the supermodel, and an empty pose leaves the bind pose on screen");
        }

        /// <summary>A cycle in the chain terminates instead of hanging.</summary>
        [Test]
        public void ASupermodelCycleTerminates()
        {
            var a = new MdlModel { Name = "a", SuperModel = "b" };
            var b = new MdlModel { Name = "b", SuperModel = "a" };

            Action act = () => MdlAnimationPose.SampleIdle(
                a, name => string.Equals(name, "a", StringComparison.OrdinalIgnoreCase) ? a : b);

            act.Should().NotThrow();
            MdlAnimationPose.SampleIdle(a, name => name == "a" ? a : b).Should().BeEmpty();
        }
    }
}
