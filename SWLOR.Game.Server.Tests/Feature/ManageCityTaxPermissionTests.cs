using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ManageCityTaxPermissionTests
{
    [TestCase("900", "5")]
    [TestCase("500", "11")]
    [TestCase("500", "10")]
    [TestCase("abc", "99")]
    public void WithoutEditTaxes_StoredTaxesAreKeptWhateverWasEntered(string citizenshipText, string transportationText)
    {
        ManageCityViewModel.ResolveTaxes(
            false, citizenshipText, transportationText, 500, 10, out var citizenship, out var transportation);

        citizenship.Should().Be(500);
        transportation.Should().Be(10);
    }

    [Test]
    public void WithEditTaxes_NewTaxesAreAcceptedAndClamped()
    {
        ManageCityViewModel.ResolveTaxes(true, "900", "5", 500, 10, out var citizenship, out var transportation);
        citizenship.Should().Be(900);
        transportation.Should().Be(5);

        ManageCityViewModel.ResolveTaxes(true, "999999", "-4", 500, 10, out citizenship, out transportation);
        citizenship.Should().Be(50000);
        transportation.Should().Be(0);

        ManageCityViewModel.ResolveTaxes(true, "abc", "99", 500, 10, out citizenship, out transportation);
        citizenship.Should().Be(0);
        transportation.Should().Be(25);
    }
}
