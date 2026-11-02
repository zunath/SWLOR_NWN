using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Who may use a city's Bank and Starport (Player City Revamp, "Who may use the Bank and Starport"): the order of the five rules, the
/// 7-day departure pass, what happens to a ship docked at a Starport its owner can no longer use, and the Embargo and Alliance on a
/// station's Jump Beacon and Shipyard.
/// </summary>
public class EdictFacilityAccessTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    #region Access order

    [Test]
    public void AnEmbargoRefusesFirst_EvenForACitizenOrUnderOpenGates()
    {
        // Embargoed citizens are citizens of another city, but nothing later in the order can let them in.
        FacilityAccessRules.Decide(true, false, true, 0, true).Should().Be(FacilityAccessResult.Embargoed);
        FacilityAccessRules.Decide(true, false, false, 0, true).Should().Be(FacilityAccessResult.Embargoed);
    }

    [Test]
    public void ACitizenWithNoTaxesOwedIsAllowed()
    {
        FacilityAccessRules.Decide(false, true, false, 0, false).Should().Be(FacilityAccessResult.Allowed);
    }

    [Test]
    public void ACitizenWhoOwesTaxesIsRefused_EvenUnderOpenGates()
    {
        FacilityAccessRules.Decide(false, true, false, 1, true).Should().Be(FacilityAccessResult.OwesTaxes);
        FacilityAccessRules.Decide(false, true, false, 500, false).Should().Be(FacilityAccessResult.OwesTaxes);
    }

    [Test]
    public void AnAlliedCitizenWithNoTaxesOwedAtHomeIsAllowed_WithoutOpenGates()
    {
        FacilityAccessRules.Decide(false, false, true, 0, false).Should().Be(FacilityAccessResult.Allowed);
    }

    [Test]
    public void AnAlliedCitizenWhoOwesTaxesAtHomeIsRefused_UnlessOpenGatesAdmitsThemAsAnyoneElse()
    {
        FacilityAccessRules.Decide(false, false, true, 300, false).Should().Be(FacilityAccessResult.OwesTaxes);
        FacilityAccessRules.Decide(false, false, true, 300, true).Should().Be(FacilityAccessResult.Allowed);
    }

    [TestCase(0)]
    [TestCase(900)]
    public void AnyoneElseIsAllowedUnderOpenGates(int owedTaxes)
    {
        FacilityAccessRules.Decide(false, false, false, owedTaxes, true).Should().Be(FacilityAccessResult.Allowed);
    }

    [Test]
    public void EveryoneElseIsRefused()
    {
        FacilityAccessRules.Decide(false, false, false, 0, false).Should().Be(FacilityAccessResult.NotWelcome);
    }

    [Test]
    public void TheRefusalsAreWordedForTheFacility()
    {
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.Allowed, 0, true, "x").Should().BeEmpty();
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.NotWelcome, 0, false, FacilityAccessRules.BankNotWelcomeMessage)
            .Should().Be("Only citizens may use this terminal.");
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.NotWelcome, 0, false, FacilityAccessRules.StarportNotWelcomeMessage)
            .Should().Be("Only citizens may use starport facilities.");
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.Embargoed, 0, false, "x")
            .Should().Be(FacilityAccessRules.EmbargoedMessage);
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.OwesTaxes, 75, true, "x")
            .Should().Be("You owe 75 credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these.");
        FacilityAccessRules.DescribeRefusal(FacilityAccessResult.OwesTaxes, 75, false, "x")
            .Should().Contain("your city").And.Contain("75");
    }

    #endregion

    #region Departure pass

    [Test]
    public void ADeparturePassLastsSevenDaysFromItsStart()
    {
        FacilityAccessRules.GetDeparturePassEndUtc(Now).Should().Be(Now.AddDays(7));
        FacilityAccessRules.IsDeparturePassValid(Now, Now).Should().BeTrue();
        FacilityAccessRules.IsDeparturePassValid(Now, Now.AddDays(7).AddSeconds(-1)).Should().BeTrue();
        FacilityAccessRules.IsDeparturePassValid(Now, Now.AddDays(7)).Should().BeFalse();
        FacilityAccessRules.IsDeparturePassValid(null, Now).Should().BeFalse();
    }

    [Test]
    public void AShipWhoseOwnerMayUseTheStarportStartsTheOwnersPassOver()
    {
        FacilityAccessRules.GetDockedShipAction(true, null, Now).Should().Be(DockedShipAction.RefreshPass);
        FacilityAccessRules.GetDockedShipAction(true, Now.AddDays(-30), Now).Should().Be(DockedShipAction.RefreshPass);
    }

    [Test]
    public void AShipStaysWhileTheOwnersPassIsValid()
    {
        FacilityAccessRules.GetDockedShipAction(false, Now.AddDays(-6), Now).Should().Be(DockedShipAction.Leave);
    }

    [Test]
    public void AShipGoesToItsSafeDockOnceTheOwnerLostAccessAndThePassRanOut()
    {
        FacilityAccessRules.GetDockedShipAction(false, Now.AddDays(-7), Now).Should().Be(DockedShipAction.ReturnToSafeDock);
        FacilityAccessRules.GetDockedShipAction(false, Now.AddDays(-40), Now).Should().Be(DockedShipAction.ReturnToSafeDock);
    }

    [Test]
    public void AShipOfAnOwnerWhoNeverHeldAPassIsLeftAlone_DockingStaysOpenToEveryone()
    {
        FacilityAccessRules.GetDockedShipAction(false, null, Now).Should().Be(DockedShipAction.Leave);
    }

    [Test]
    public void ThePassesAreKeptPerCity_AndAnOldRecordLoadsWithNone()
    {
        var player = new Player("p");
        player.StarportDeparturePasses.Should().BeEmpty();

        var loaded = Newtonsoft.Json.JsonConvert.DeserializeObject<Player>("{\"Id\":\"p\"}");
        loaded!.StarportDeparturePasses.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Jump Beacon and Shipyard

    [TestCase(JumpBeaconAccessType.Anyone)]
    [TestCase(JumpBeaconAccessType.Allies)]
    [TestCase(JumpBeaconAccessType.Citizens)]
    public void AnEmbargoRefusesAtTheBeaconWhateverTheSetting(JumpBeaconAccessType access)
    {
        StationBuildingRules.CanUseBeacon(access, false, true, true).Should().BeFalse();
        StationBuildingRules.CanUseBeacon(access, true, true, true).Should().BeFalse();
    }

    [Test]
    public void WithoutAnEmbargoTheBeaconKeepsItsOwnAccessSetting()
    {
        StationBuildingRules.CanUseBeacon(JumpBeaconAccessType.Anyone, false, false).Should().BeTrue();
        StationBuildingRules.CanUseBeacon(JumpBeaconAccessType.Allies, false, true).Should().BeTrue("an Alliance counts the allied station as an ally");
        StationBuildingRules.CanUseBeacon(JumpBeaconAccessType.Allies, false, false).Should().BeFalse();
        StationBuildingRules.CanUseBeacon(JumpBeaconAccessType.Citizens, true, false).Should().BeTrue();
        StationBuildingRules.CanUseBeacon(JumpBeaconAccessType.Citizens, false, true).Should().BeFalse("allies only matter when the setting lists them");
    }

    [TestCase(ShipyardAccessType.Anyone)]
    [TestCase(ShipyardAccessType.Citizens)]
    public void AnEmbargoRefusesAtTheShipyardWhateverTheSetting(ShipyardAccessType access)
    {
        var refusal = StationBuildingRules.GetShipyardRefusal(true, access, false, 0, true);

        refusal.Should().Be(ShipyardRefusal.Embargoed);
        StationBuildingRules.DescribeShipyardRefusal(refusal).Should().Contain("closed its Shipyard");
    }

    [Test]
    public void TheShipyardStillChecksTheShipyardFirst_AndWithoutAnEmbargoIsUnchanged()
    {
        StationBuildingRules.GetShipyardRefusal(false, ShipyardAccessType.Anyone, false, 0, true).Should().Be(ShipyardRefusal.NotAtShipyard);
        StationBuildingRules.GetShipyardRefusal(true, ShipyardAccessType.Anyone, false, 500).Should().Be(ShipyardRefusal.None);
        StationBuildingRules.GetShipyardRefusal(true, ShipyardAccessType.Citizens, false, 0).Should().Be(ShipyardRefusal.CitizensOnly);
        StationBuildingRules.GetShipyardRefusal(true, ShipyardAccessType.Citizens, true, 500).Should().Be(ShipyardRefusal.OwesTaxes);
    }

    #endregion
}
