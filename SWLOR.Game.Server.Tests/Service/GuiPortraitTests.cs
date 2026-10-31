using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class GuiPortraitTests
{
    [TestCase(Gender.Male, "po_hu_m_99_l")]
    [TestCase(Gender.Female, "po_hu_f_99_l")]
    [TestCase(Gender.None, "po_hu_m_99_l")]
    public void MissingCustomPortrait_UsesUnknownPortrait(Gender gender, string expected)
    {
        GuiPortrait.ResolveLarge("custom_", gender, _ => false).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void EmptyPortrait_UsesUnknownPortraitWithoutLookingUpResource(string portrait)
    {
        GuiPortrait.ResolveLarge(portrait, Gender.Female,
            _ => throw new AssertionException("An empty portrait should not be looked up."))
            .Should().Be("po_hu_f_99_l");
    }

    [TestCase(Gender.Male)]
    [TestCase(Gender.Female)]
    public void AvailablePortrait_IsPreserved(Gender gender)
    {
        GuiPortrait.ResolveLarge("custom_", gender, resref => resref == "custom_l")
            .Should().Be("custom_l");
    }
}
