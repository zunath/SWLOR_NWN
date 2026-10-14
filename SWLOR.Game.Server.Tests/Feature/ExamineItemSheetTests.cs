using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ExamineItemSheetTests
{
    [Test]
    public void Facts_ShowRemainingCharges()
    {
        var facts = ExamineItemText.PlainText(ExamineItemSheet.BuildFacts("Electronic Components 2", 1, 3, 5, true));

        facts.Should().Be("Type: Electronic Components 2\nCharges: 3\nWeight: 0.5 lbs");
    }

    [Test]
    public void Facts_ShowStackQuantityAndTotalWeight()
    {
        var facts = ExamineItemText.PlainText(ExamineItemSheet.BuildFacts("Miscellaneous Medium", 50, 0, 250, true));

        facts.Should().Be("Type: Miscellaneous Medium\nQuantity: 50\nWeight: 25.0 lbs");
    }

    [Test]
    public void Facts_OmitEmptyChargesAndFlagUnidentifiedItems()
    {
        var facts = ExamineItemText.PlainText(ExamineItemSheet.BuildFacts("Lightsaber", 1, 0, 20, false));

        facts.Should().Be("Type: Lightsaber\nWeight: 2.0 lbs\nUnidentified");
    }

    [Test]
    public void UnidentifiedTitle_UsesTheBaseItemName()
    {
        ExamineItemSheet.UnidentifiedTitle("Lightsaber").Should().Be("Lightsaber (Unidentified)");
        ExamineItemSheet.UnidentifiedTitle("").Should().Be("Unidentified Item");
    }

    [Test]
    public void SortProperties_MatchesNativeOrdinalOrderAndDropsBlankLines()
    {
        var properties = "Accuracy:  1\nAttack:  1\nDMG 10\nUnlimited Ammo: Bullet Ammo: Basic\nRequires Skill:  Pistol 10\nDelay 250\n";

        ExamineItemSheet.SortProperties(properties).Should().Be(
            "Accuracy:  1\nAttack:  1\nDMG 10\nDelay 250\nRequires Skill:  Pistol 10\nUnlimited Ammo: Bullet Ammo: Basic");
        ExamineItemSheet.SortProperties(null).Should().BeEmpty();
    }

    [Test]
    public void Body_HasHeadedSectionsAndSkipsEmptyOnes()
    {
        var body = ExamineItemText.PlainText(ExamineItemSheet.BuildBody("A saber.", "Keen", true, "Weapon DMG: 10"));

        body.Should().Be("Description\nA saber.\n\nProperties\nKeen\n\nWeapon Damage\nWeapon DMG: 10");
        ExamineItemText.PlainText(ExamineItemSheet.BuildBody("A saber.", "", true, "")).Should().Be("Description\nA saber.");
    }

    [Test]
    public void Body_HidesPropertiesOfUnidentifiedItems()
    {
        var body = ExamineItemText.PlainText(ExamineItemSheet.BuildBody("Strange hilt.", "Keen", false, "Weapon DMG: 10"));

        body.Should().Be("Description\nStrange hilt.");
    }

    [Test]
    public void Body_KeepsAuthoredDescriptionColorsAndHeadingColor()
    {
        var runs = ExamineItemText.Parse(ExamineItemSheet.BuildBody(ColorToken.Green("Licensed"), "", true, ""));

        runs.Should().Contain(run => run.Text == "Licensed" && run.Color.G == 255 && run.Color.R == 0);
        runs.Should().Contain(run => run.Text == "Description" && run.Color.R == 226);
    }
}
