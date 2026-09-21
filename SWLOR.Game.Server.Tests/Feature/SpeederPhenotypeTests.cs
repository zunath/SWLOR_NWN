using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpeederPhenotypeTests
{
    [TestCase(PhenoType.Normal, PhenoType.SpeederBike)]
    [TestCase(PhenoType.Big, PhenoType.SpeederBikeLarge)]
    public void Riding_ReturnsToTheBodyTypeItMountedWith(PhenoType body, PhenoType riding)
    {
        SpeederPhenotype.TryGetRiding(body, out var actual).Should().BeTrue();
        actual.Should().Be(riding);
        SpeederPhenotype.IsRiding(actual).Should().BeTrue();
        SpeederPhenotype.GetDismounted(actual).Should().Be(body);
    }

    [Test]
    public void BodyTypesWithoutRidingModels_CannotMount()
    {
        SpeederPhenotype.TryGetRiding(PhenoType.SpeederBike, out _).Should().BeFalse();
        SpeederPhenotype.TryGetRiding(PhenoType.Custom1, out _).Should().BeFalse();
        SpeederPhenotype.IsRiding(PhenoType.Normal).Should().BeFalse();
        SpeederPhenotype.IsRiding(PhenoType.Big).Should().BeFalse();
    }

    [TestCase(PhenoType.Normal)]
    [TestCase(PhenoType.Big)]
    public void RidingPhenotypeRow_FallsBackToItsBodyTypeForPartModels(PhenoType body)
    {
        SpeederPhenotype.TryGetRiding(body, out var riding).Should().BeTrue();
        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da", "phenotype.2da")));

        rows[(int)riding]["DefaultPhenoType"].Should().Be(((int)body).ToString());
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
