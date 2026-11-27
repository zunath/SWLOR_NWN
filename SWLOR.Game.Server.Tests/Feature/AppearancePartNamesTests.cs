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
    public void AnAstromechPartHasItsOwnNameNotTheClothingNameOfItsNumber()
    {
        // Astromech torsos and thighs are barrels and legs, not the shared armor shapes of those numbers.
        AppearancePartNames.Get("pmh0_chest008", "?").Should().Be(AppearancePartNames.All["chest008"]);
        foreach (var gender in new[] { "m", "f" })
        {
            for (var design = 1; design <= 8; design++)
            {
                var dome = AppearancePartNames.Get($"p{gender}r0_head{design:000}", "?");
                var body = AppearancePartNames.Get($"p{gender}r0_chest{design:000}", "?");
                var left = AppearancePartNames.Get($"p{gender}r0_legl{design:000}", "?");
                var right = AppearancePartNames.Get($"p{gender}r0_legr{design:000}", "?");

                dome.Should().EndWith(" Dome");
                body.Should().EndWith(" Body");
                left.Should().EndWith(" Leg");
                right.Should().Be(left);
                body.Should().NotBe(AppearancePartNames.All[$"chest{design:000}"]);
            }
        }
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
            // Letter count alone lets wide names ("Compact Matte") run into the next tile's name.
            AppearancePartNames.Width(name).Should().BeLessThanOrEqualTo(AppearancePartNames.MaxWidth, $"{key} is \"{name}\"");
            // Letters only: a number would read as the part number drawn on the tile.
            Regex.IsMatch(name, "^[A-Za-z][A-Za-z '\\-]*$").Should().BeTrue($"{key} is \"{name}\"");
            name.Should().Be(name.Trim(), key);
            name.Should().NotContain("  ", key);
        }
    }

    [TestCase("Long Mustache", 102)]
    [TestCase("Goatee", 50)]
    [TestCase("Jawa's Hood-Cap", 109)]
    public void WidthMatchesTheGameLabelFont(string name, int expected)
    {
        AppearancePartNames.Width(name).Should().Be(expected);
    }

    [TestCase("Bare Fists (Invisible)", "Bare Fists")]
    [TestCase("Power Glove", "Power Glove")]
    [TestCase("Pad (Thin)", "Pad (Thin)")]
    public void TileNameDropsABracketedNoteOnlyWhenTheNameIsTooWide(string name, string expected)
    {
        AppearancePartNames.TileName(name).Should().Be(expected);
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
