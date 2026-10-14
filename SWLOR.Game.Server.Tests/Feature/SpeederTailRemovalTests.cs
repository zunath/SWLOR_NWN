using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpeederTailRemovalTests
{
    [TestCase(TailType.SwoopBike, 4)]
    [TestCase(TailType.SpeederBike, 5)]
    [TestCase(TailType.LandSpeeder, 6)]
    public void RetiredTailSlotsRemainEmptyWithoutRenumbering(TailType tail, int row)
    {
        var table = ReadTable("tailmodel");

        ((int)tail).Should().Be(row);
        table.RowLabels[row].Should().Be(row.ToString());
        table.GetValue(row, "LABEL").Should().BeNull();
        table.GetValue(row, "MODEL").Should().BeNull();
        table.GetValue(row, "ENVMAP").Should().BeNull();
        table.GetValue(3, "MODEL").Should().Be("c_taildevil");
        table.GetValue(7, "MODEL").Should().Be("c_tail_sil");
    }

    [Test]
    public void RetiredRidingPhenotypeIsUnavailableAndTailModelsAreAbsent()
    {
        var table = ReadTable("phenotype");
        var row = (int)PhenoType.SpeederBike;

        row.Should().Be(22);
        table.RowLabels[row].Should().Be("22");
        table.GetValue(row, "Label").Should().BeNull();
        table.GetValue(row, "Name").Should().BeNull();
        table.GetValue(23, "Label").Should().Be("car");

        var robeMappings = ReadTable("roberender");
        for (var index = 0; index < robeMappings.RowCount; index++)
        {
            robeMappings.GetValue(index, "BASEPHENOTYPE").Should().NotBe("22");
            robeMappings.GetValue(index, "PHENOTYPE").Should().NotBe("22");
        }

        foreach (var model in new[] { "tail_swoopbike", "tail_speederbike", "tail_sandspeeder" })
            File.Exists(Path.Combine(FindRoot(), "SWLOR_Haks", "sw_cr_creature", model + ".mdl"))
                .Should().BeFalse();

    }

    [TestCase("pfh22_robe007")]
    [TestCase("pmh22_robe187")]
    [TestCase("pmh22_robe254")]
    public void LargeBodyRobeModelsRemainAvailable(string model)
    {
        var root = Path.Combine(FindRoot(), "SWLOR_Haks");
        File.Exists(Path.Combine(root, "sw_pt_robe", model + ".mdl")).Should().BeTrue(
            "the phenotype-2 robe lookup uses the double-2 prefix, independently of riding phenotype 22");

        Directory.EnumerateFiles(Path.Combine(root, "sw_pt_robe"), "*.mdl")
            .Count(path => Regex.IsMatch(Path.GetFileName(path), @"^p[fm][a-z]22_robe\d{3}\.mdl$",
                RegexOptions.IgnoreCase))
            .Should().BeGreaterThanOrEqualTo(1241, "the existing large-body robe catalog must remain intact");
    }

    [TestCase("pfh22_robe007")]
    [TestCase("pmh22_robe187")]
    public void LargeBodyRobesKeepTheirTintMappings(string model)
    {
        var table = ReadTable("tintmap");
        Enumerable.Range(0, table.RowCount)
            .Select(row => table.GetValue(row, "MODEL"))
            .Should().Contain(model);
    }

    [TestCase(1058, "sw_plc_mdrn", "plc_swpbike1", "sw_plc", "plc_swoop.dds")]
    [TestCase(20667, "sw_plc_mdrn", "jzc_sdrbk01", "sw_cr_creature", "c_speederbike.tga")]
    [TestCase(20668, "sw_plc_mdrn", "jzc_sdrbk02", "sw_cr_creature", "c_speederbike.tga")]
    [TestCase(31450, "sw_plc", "plc_airspd_04", "sw_plc", "Nv_sandspeeder.dds")]
    public void PlaceableSpeedersKeepTheirAppearanceModelsAndSharedTextures(
        int appearance, string modelFolder, string model, string textureFolder, string texture)
    {
        ReadTable("placeables").GetValue(appearance, "ModelName").Should().Be(model);
        File.Exists(Path.Combine(FindRoot(), "SWLOR_Haks", modelFolder, model + ".mdl"))
            .Should().BeTrue();
        File.Exists(Path.Combine(FindRoot(), "SWLOR_Haks", textureFolder, texture))
            .Should().BeTrue("placeables still use this texture after tail models are removed");
    }

    private static TwoDAFile ReadTable(string table) =>
        TwoDAReader.Read(Path.Combine(FindRoot(), "SWLOR_Haks", "sw_2da", table + ".2da"));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the repository root.");
    }
}
