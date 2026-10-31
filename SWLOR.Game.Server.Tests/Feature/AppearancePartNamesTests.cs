using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class AppearancePartNamesTests
{
    [TestCase("pmh0_head257", "pmh0_head257")]
    [TestCase("pfe0_head012", "pfe0_head012")]
    [TestCase("pmh0_chest012", "chest012")]
    [TestCase("pfz0_chest012", "chest012")]
    [TestCase("pmh0_bicepl012", "bicep012")]
    [TestCase("pfh0_bicepr012", "bicep012")]
    [TestCase("pmh0_legl004", "leg004")]
    [TestCase("pmh0_shor031", "sho031")]
    [TestCase("pmh0_pelvis101", "pelvis101")]
    [TestCase("pmh0_belt003", "belt003")]
    [TestCase("pmh0_clk012", "clk012")]
    [TestCase("helm_012", "helm_012")]
    [TestCase("wswls_t_011", "wswls_t_011")]
    [TestCase("ASHLW_004", "ashlw_004")]
    public void NameKeySharesArmorNamesAcrossBodiesAndSides(string pictureKey, string expected)
    {
        AppearancePartNames.NameKey(pictureKey).Should().Be(expected);
    }

    [Test]
    public void MissingNamesFallBack()
    {
        AppearancePartNames.Get(null, "7").Should().Be("7");
        AppearancePartNames.Get(string.Empty, "7").Should().Be("7");
        AppearancePartNames.Get("pmh0_chest999", "999").Should().Be("999");
    }

    [Test]
    public void LeftAndRightPartsShareOneName()
    {
        var key = AppearancePartNames.All.Keys.First(k => k.StartsWith("bicep"));
        var id = key.Substring("bicep".Length);
        AppearancePartNames.Get($"pmh0_bicepl{id}", "?").Should().Be(AppearancePartNames.All[key]);
        AppearancePartNames.Get($"pfh0_bicepr{id}", "?").Should().Be(AppearancePartNames.All[key]);
    }

    [Test]
    public void NamesFitUnderAPictureInPlainText()
    {
        AppearancePartNames.All.Should().NotBeEmpty();
        foreach (var (key, name) in AppearancePartNames.All)
        {
            name.Length.Should().BeInRange(1, AppearancePartNames.MaxLength, key);
            Regex.IsMatch(name, "^[A-Za-z0-9][A-Za-z0-9 '\\-]*$").Should().BeTrue($"{key} is \"{name}\"");
            name.Should().Be(name.Trim(), key);
            name.Should().NotContain("  ", key);
        }
    }

    [Test]
    public void NamesAreUniqueWithinOneBrowserList()
    {
        // A browser list is one slot (or head body model, or weapon part class).
        var lists = AppearancePartNames.All.GroupBy(entry => ListOf(entry.Key));
        foreach (var list in lists)
        {
            var duplicates = list.GroupBy(entry => entry.Value).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            duplicates.Should().BeEmpty(list.Key);
        }
    }

    private static string ListOf(string nameKey) => Regex.Replace(nameKey, "\\d+$", string.Empty);
}
