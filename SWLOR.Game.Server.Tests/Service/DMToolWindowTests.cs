using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Service.DMToolService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Service;

public class DMToolWindowTests
{
    [Test]
    public void SearchTerms_SplitOnWhitespaceAndDropBlanks()
    {
        DMSearchText.GetTerms("  rancor   den\tkeeper ").Should().Equal("rancor", "den", "keeper");
        DMSearchText.GetTerms(null).Should().BeEmpty();
        DMSearchText.GetTerms("   ").Should().BeEmpty();
    }

    [Test]
    public void SearchMatch_RequiresEveryTermInSomeField()
    {
        var terms = DMSearchText.GetTerms("rancor DEN");

        DMSearchText.Matches(terms, "Rancor", "pw_rancor_den").Should().BeTrue();
        DMSearchText.Matches(terms, "Rancor", "rancor_pit").Should().BeFalse();
    }

    [Test]
    public void SearchMatch_IsCaseInsensitiveSubstringAndIgnoresMissingFields()
    {
        var terms = DMSearchText.GetTerms("qrm");

        DMSearchText.Matches(terms, null, string.Empty, "AQRMX6GM").Should().BeTrue();
        DMSearchText.Matches(terms, null, string.Empty).Should().BeFalse();
    }

    [Test]
    public void SearchMatch_WithNoTermsMatchesEverything()
    {
        DMSearchText.Matches(DMSearchText.GetTerms(string.Empty), "anything").Should().BeTrue();
        DMSearchText.Matches(DMSearchText.GetTerms(string.Empty)).Should().BeTrue();
    }

    [Test]
    public void DMWindows_StayOpenAcrossAreaChanges()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        new DMToolbarDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeTrue();
        new DMChooserDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeTrue();
        new DMCreatorDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeTrue();
        new DMPlayerSearchDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeTrue();
    }

    [Test]
    public void OtherWindows_StillCloseOnAreaChange()
    {
        using var validationBuild = GuiLayoutValidator.BeginValidationOnlyBuild();

        new KeyItemsDefinition().BuildWindow().KeepsOpenOnAreaChange.Should().BeFalse();
    }
}
