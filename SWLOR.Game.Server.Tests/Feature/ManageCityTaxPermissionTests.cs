using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class ManageCityTaxPermissionTests
{
    [Test]
    public void WithoutEditTaxes_ChangingEitherTaxIsRejectedAndStoredValuesAreKept()
    {
        var allowed = ManageCityViewModel.TryResolveTaxes(
            false, "900", "5", 500, 10, out var citizenship, out var transportation);

        allowed.Should().BeFalse();
        citizenship.Should().Be(500);
        transportation.Should().Be(10);
    }

    [Test]
    public void WithoutEditTaxes_ChangingOnlyTransportationIsRejected()
    {
        ManageCityViewModel.TryResolveTaxes(false, "500", "11", 500, 10, out _, out _).Should().BeFalse();
    }

    [Test]
    public void WithoutEditTaxes_LeavingTaxesUnchangedStillAllowsSavingTheRest()
    {
        var allowed = ManageCityViewModel.TryResolveTaxes(
            false, "500", "10", 500, 10, out var citizenship, out var transportation);

        allowed.Should().BeTrue();
        citizenship.Should().Be(500);
        transportation.Should().Be(10);
    }

    [Test]
    public void WithEditTaxes_NewTaxesAreAcceptedAndClamped()
    {
        ManageCityViewModel.TryResolveTaxes(true, "900", "5", 500, 10, out var citizenship, out var transportation)
            .Should().BeTrue();
        citizenship.Should().Be(900);
        transportation.Should().Be(5);

        ManageCityViewModel.TryResolveTaxes(true, "999999", "-4", 500, 10, out citizenship, out transportation)
            .Should().BeTrue();
        citizenship.Should().Be(50000);
        transportation.Should().Be(0);

        ManageCityViewModel.TryResolveTaxes(true, "abc", "99", 500, 10, out citizenship, out transportation)
            .Should().BeTrue();
        citizenship.Should().Be(0);
        transportation.Should().Be(25);
    }
}
