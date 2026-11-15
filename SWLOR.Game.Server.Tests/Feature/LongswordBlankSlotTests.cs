using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Vibroblades and electroblades share the wswls part models. Top 1.6 and Middle 1.6 are blank parts, and
/// Middle 5.5 and 9.6 are full blades meant to pair with the blank top. A part with no model renders as
/// the base item's bag.
/// </summary>
public class LongswordBlankSlotTests
{
    private const string ItemClass = "wswls";

    /// <summary>Base game wswls parts: models 1 to 9 in colors 1 to 4, for every role.</summary>
    private static bool IsStockPart(int model, int color) => model is >= 1 and <= 9 && color is >= 1 and <= 4;

    private static readonly (string File, string Sha256)[] LegacyWholeBladeMiddles =
    {
        ("wswls_m_055.mdl", "96a1928bf73cfc5c8326909c151cdae2e46457a527b3b350255649c28f659cb9"),
        ("wswls_m_096.mdl", "bcd63e506c383a561b1fe2fb43703ef37b1796a5b41bd1ace1d6d0dc4089755c"),
    };

    private static IEnumerable<TestCaseData> Definitions()
    {
        // The vibroblade (longsword) definition is internal to the server assembly.
        var longsword = typeof(WeaponAppearanceBaseDefinition).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.LongswordAppearanceDefinition", throwOnError: true)!;
        yield return new TestCaseData((WeaponAppearanceBaseDefinition)Activator.CreateInstance(longsword, nonPublic: true)!)
            .SetArgDisplayNames("Vibroblade");
        yield return new TestCaseData(new ElectrobladeAppearanceDefinition()).SetArgDisplayNames("Electroblade");
    }

    [TestCaseSource(nameof(Definitions))]
    public void TheBlankAndLegacyWholeBladeChoicesStayInTheCatalog(WeaponAppearanceBaseDefinition appearance)
    {
        // The editor refuses a weapon whose current part is missing from its catalog, so a blade built
        // as blank top + full-blade middle can only be edited while these stay listed (color * 100 + model).
        appearance.TopParts.Should().Contain(601, "Top 1.6 is the blank top");
        appearance.MiddleParts.Should().Contain(new[] { 601, 505, 609, 105 },
            "Middle 1.6 is blank, 5.5 and 9.6 are full blades and 5.1 is a guard players pair with the blank top");
    }

    [TestCaseSource(nameof(Definitions))]
    public void EveryCatalogPartHasAModelAndInventoryIcon(WeaponAppearanceBaseDefinition appearance)
    {
        var weapon = WeaponRoot();
        var missing = new List<string>();
        foreach (var (letter, parts) in new[] { ('t', appearance.TopParts), ('m', appearance.MiddleParts), ('b', appearance.BottomParts) })
        {
            foreach (var part in parts)
            {
                var model = part % 100;
                var color = part / 100;
                if (IsStockPart(model, color))
                    continue;
                var resref = $"{ItemClass}_{letter}_{model * 10 + color:D3}";
                if (!File.Exists(Path.Combine(weapon, resref + ".mdl")))
                    missing.Add($"{resref}.mdl (catalog {part})");
                if (!File.Exists(Path.Combine(weapon, $"i{resref}.tga")) && !File.Exists(Path.Combine(weapon, $"i{resref}.dds")))
                    missing.Add($"i{resref}.tga (catalog {part})");
            }
        }

        missing.Should().BeEmpty("a catalog part without a model renders as the base item's bag:\n" + string.Join("\n", missing));
    }

    [TestCase("wswls_t_016")]
    [TestCase("wswls_m_016")]
    [TestCase("wswls_b_091")] // the blank bottom under the full-sword middle 5.2
    public void BlankPartsAreEmptyModels(string resref)
    {
        var path = Path.Combine(WeaponRoot(), resref + ".mdl");
        File.Exists(path).Should().BeTrue($"{resref} must ship, or the client draws a bag in its place");
        var data = File.ReadAllBytes(path);
        data.Take(4).Should().Equal(new byte[4], "the blank part is a compiled model");
        // A compiled model holding only its root dummy node is 356 bytes; any mesh makes it larger.
        data.Length.Should().Be(356, $"{resref} must stay a dummy-only model with no geometry");
        AppearancePartNames.Get(resref, string.Empty).Should().Be("Blank");
    }

    [Test]
    public void LegacyWholeBladeMiddlesKeepTheirOriginalModels()
    {
        foreach (var (file, sha256) in LegacyWholeBladeMiddles)
        {
            var path = Path.Combine(WeaponRoot(), file);
            File.Exists(path).Should().BeTrue($"{file} is a full blade players pair with the blank top");
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant().Should().Be(sha256,
                $"{file} is a look players already use with the blank top; give a new design a free part ID instead");
        }
    }

    private static string WeaponRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");

        var weapon = Path.Combine(directory!.FullName, "SWLOR_Haks", "sw_weapon");
        Directory.Exists(weapon).Should().BeTrue("the SWLOR_Haks sw_weapon asset directory must be checked out");
        return weapon;
    }
}
