using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// City Projects (Player City Revamp, "City Projects"): the credits held in escrow with each portion remembered, donations capped at
/// the need, the 2-day build across restarts, the Gazette's top donors and what a cancelled project owes its donors.
/// </summary>
public class CityProjectRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    private const string Parts = CityProjectCatalog.ReinforcedConstructionParts;
    private const string Units = CityProjectCatalog.ReinforcedPowerSupplyUnit;

    private static WorldProperty City(long treasury = 1_000_000, long restricted = 0)
    {
        return new WorldProperty
        {
            Id = "city",
            PropertyType = PropertyType.City,
            Treasury = treasury,
            RestrictedTreasury = restricted,
            Upgrades =
            {
                [PropertyUpgradeType.CityLevel] = 5,
                [PropertyUpgradeType.StarportLevel] = 1,
                [PropertyUpgradeType.MedicalCenterLevel] = 1,
                [PropertyUpgradeType.CantinaLevel] = 1
            }
        };
    }

    private static CityProject Starport(int level = 2) =>
        CityProjectRules.Create("city", CityProjectType.StarportUpgrade, level, "mayor", Now);

    private static CityProject OpenProject(CityProject project)
    {
        project.State = CityProjectState.Open;
        return project;
    }

    private static CityProjectRefusal Check(
        CityProjectType type = CityProjectType.StarportUpgrade,
        int currentLevel = 1,
        int cityLevel = 5,
        int mayorRank = 5,
        bool isStation = false,
        bool homeGround = false,
        bool open = false,
        long spendable = 1_000_000,
        bool canStart = true)
    {
        return CityProjectRules.CheckStart(type, currentLevel, cityLevel, mayorRank, isStation, homeGround, open, spendable, canStart);
    }

    #region What a project costs

    [Test]
    public void AFacilityUpgrade_CostsWhatItCostsToday_FiftyThousandTimesTheLevel()
    {
        CityProjectRules.GetCredits(CityProjectType.StarportUpgrade, 2).Should().Be(100_000);
        CityProjectRules.GetCredits(CityProjectType.CantinaUpgrade, 3).Should().Be(150_000);
        CityProjectRules.GetCredits(CityProjectType.MedicalCenterUpgrade, 5).Should().Be(250_000);
        CityProjectRules.GetCredits(CityProjectType.JumpBeaconUpgrade, 3).Should().Be(150_000);
    }

    [Test]
    public void HomeGround_HasOnePrice_WhateverLevelIsAsked()
    {
        CityProjectRules.GetCredits(CityProjectType.HomeGround, 0).Should().Be(CityProjectRules.HomeGroundCredits);
        CityProjectRules.GetCredits(CityProjectType.HomeGround, 4).Should().Be(CityProjectRules.HomeGroundCredits);
    }

    [Test]
    public void ANewProject_CarriesItsCatalogMaterialsAndStartsUnfunded()
    {
        var project = Starport(3);

        project.State.Should().Be(CityProjectState.Starting);
        project.TargetLevel.Should().Be(3);
        project.CreditCost.Should().Be(150_000);
        project.RequiredMaterials.Should().Equal(CityProjectCatalog.Get(CityProjectType.StarportUpgrade, 3).Materials);
        project.DonatedMaterials.Should().BeEmpty();
        project.SuppliedUtc.Should().BeNull();
    }

    [Test]
    public void AProjectTheCatalogDoesNotHave_IsNotCreated()
    {
        CityProjectRules.Create("city", CityProjectType.StarportUpgrade, 9, "mayor", Now).Should().BeNull();
        CityProjectRules.Create("city", CityProjectType.Invalid, 2, "mayor", Now).Should().BeNull();
    }

    #endregion

    #region Starting

    [Test]
    public void AnAffordableUpgrade_CanStart()
    {
        Check().Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void StartingNeedsThePower()
    {
        Check(canStart: false).Should().Be(CityProjectRefusal.NoPermission);
    }

    [Test]
    public void AFacilityAtItsTopLevel_CannotBeUpgraded()
    {
        Check(currentLevel: 5).Should().Be(CityProjectRefusal.MaxLevel);
        Check(CityProjectType.JumpBeaconUpgrade, currentLevel: 3, isStation: true).Should().Be(CityProjectRefusal.MaxLevel);
        Check(CityProjectType.JumpBeaconUpgrade, currentLevel: 2, isStation: true).Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void TheCityLevelHasToRiseFirst()
    {
        Check(currentLevel: 2, cityLevel: 2).Should().Be(CityProjectRefusal.CityLevelTooLow);
        Check(currentLevel: 2, cityLevel: 3).Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void TheMayorsCityManagementHasToReachTheNextLevel()
    {
        Check(currentLevel: 3, mayorRank: 2).Should().Be(CityProjectRefusal.MayorPerkTooLow);
        Check(currentLevel: 3, mayorRank: 3).Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void OnlyOneProjectOfAKindIsOpenAtATime()
    {
        Check(open: true).Should().Be(CityProjectRefusal.AlreadyOpen);
        Check(CityProjectType.HomeGround, open: true).Should().Be(CityProjectRefusal.AlreadyOpen);
    }

    [Test]
    public void HomeGround_IsBuiltOnce()
    {
        Check(CityProjectType.HomeGround, currentLevel: 0).Should().Be(CityProjectRefusal.None);
        Check(CityProjectType.HomeGround, homeGround: true).Should().Be(CityProjectRefusal.AlreadyBuilt);
    }

    [Test]
    public void HomeGround_ToleratesAnyCityLevel_AndAnyMayorRank()
    {
        Check(CityProjectType.HomeGround, cityLevel: 1, mayorRank: 0).Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void TheJumpBeacon_IsAStationProject()
    {
        Check(CityProjectType.JumpBeaconUpgrade, isStation: false).Should().Be(CityProjectRefusal.StationOnly);
    }

    [Test]
    public void TheCredits_CanComeFromBothBalances_SoTheCheckIsAgainstWhatCanBeSpent()
    {
        Check(spendable: 99_999).Should().Be(CityProjectRefusal.CannotAfford);
        Check(spendable: 100_000).Should().Be(CityProjectRefusal.None);

        // 60,000 restricted and 40,000 in the treasury is 100,000 spendable.
        var city = City(treasury: 40_000, restricted: 60_000);
        Check(spendable: CityTreasuryRules.GetSpendable(city)).Should().Be(CityProjectRefusal.None);
    }

    [Test]
    public void EveryRefusal_HasAMessage()
    {
        foreach (var refusal in Enum.GetValues<CityProjectRefusal>().Where(x => x != CityProjectRefusal.None))
            CityProjectRules.Describe(refusal, 100_000).Should().NotBeNullOrWhiteSpace(refusal.ToString());

        CityProjectRules.Describe(CityProjectRefusal.CannotAfford, 100_000).Should().Contain("100,000");
    }

    #endregion

    #region Escrow

    [Test]
    public void StartingAProject_HoldsRestrictedFundsFirst_AndRemembersEachPortion()
    {
        var city = City(treasury: 80_000, restricted: 60_000);
        var project = Starport(2);

        CityProjectRules.HoldCredits(city, project).Should().BeTrue();

        city.RestrictedTreasury.Should().Be(0);
        city.Treasury.Should().Be(40_000);
        city.Escrow[project.Id].Restricted.Should().Be(60_000);
        city.Escrow[project.Id].Unrestricted.Should().Be(40_000);
    }

    [Test]
    public void AProjectTheCityCannotPay_HoldsNothing()
    {
        var city = City(treasury: 10_000, restricted: 5_000);

        CityProjectRules.HoldCredits(city, Starport(2)).Should().BeFalse();

        city.Treasury.Should().Be(10_000);
        city.RestrictedTreasury.Should().Be(5_000);
        city.Escrow.Should().BeEmpty();
    }

    [Test]
    public void StartingTheSameProjectTwice_AfterARestart_CannotHoldTheMoneyTwice()
    {
        var city = City(treasury: 500_000);
        var project = Starport(2);
        CityProjectRules.HoldCredits(city, project).Should().BeTrue();

        CityProjectRules.HoldCredits(city, project).Should().BeFalse();

        city.Treasury.Should().Be(400_000);
    }

    [Test]
    public void CancellingAProject_ReturnsEachPortionToTheBalanceItCameFrom()
    {
        var city = City(treasury: 80_000, restricted: 60_000);
        var project = Starport(2);
        CityProjectRules.HoldCredits(city, project);

        var release = CityTreasuryRules.RefundEscrow(city, project.Id);

        release.Found.Should().BeTrue();
        city.RestrictedTreasury.Should().Be(60_000, "restricted income does not turn into credits the mayor can withdraw");
        city.Treasury.Should().Be(80_000);
        city.Escrow.Should().BeEmpty();
    }

    [Test]
    public void CancellingTwice_AfterARestart_ReturnsTheCreditsOnce()
    {
        var city = City(treasury: 80_000, restricted: 60_000);
        var project = Starport(2);
        CityProjectRules.HoldCredits(city, project);
        CityTreasuryRules.RefundEscrow(city, project.Id);

        CityTreasuryRules.RefundEscrow(city, project.Id).Found.Should().BeFalse();

        city.Treasury.Should().Be(80_000);
        city.RestrictedTreasury.Should().Be(60_000);
    }

    [Test]
    public void ACompletedProject_PaysItsCreditsOut_AndNeitherBalanceGrows()
    {
        var city = City(treasury: 80_000, restricted: 60_000);
        var project = Starport(2);
        CityProjectRules.HoldCredits(city, project);

        var release = CityTreasuryRules.PayOutEscrow(city, project.Id);

        release.Credits.Should().Be(100_000);
        city.Treasury.Should().Be(40_000);
        city.RestrictedTreasury.Should().Be(0);
    }

    [Test]
    public void AMayorChange_DoesNotTouchTheHeldCredits()
    {
        var city = City(treasury: 200_000);
        var project = Starport(2);
        CityProjectRules.HoldCredits(city, project);

        city.OwnerPlayerId = "new-mayor";

        city.Escrow[project.Id].Unrestricted.Should().Be(100_000);
    }

    #endregion

    #region Donations

    [Test]
    public void ADonation_IsTakenOnlyUpToWhatTheProjectStillNeeds()
    {
        var project = OpenProject(Starport(2));
        var needed = project.RequiredMaterials[Parts];

        var accepted = CityProjectRules.Accept(project, "donor", "A Human", Parts, needed + 10, Now);

        accepted.Should().Be(needed);
        project.DonatedMaterials[Parts].Should().Be(needed);
        CityProjectRules.GetRemaining(project, Parts).Should().Be(0);
        CityProjectRules.ClampDonation(project, Parts, 5).Should().Be(0);
    }

    [Test]
    public void AGiftOfWhatIsAlreadyFullyDonated_TakesNothing()
    {
        var project = OpenProject(Starport(2));
        CityProjectRules.Accept(project, "one", "One", Parts, 99, Now);

        CityProjectRules.Accept(project, "two", "Two", Parts, 3, Now).Should().Be(0);

        project.DonorMaterials.Should().NotContainKey("two");
    }

    [Test]
    public void SeveralDonors_AddUp_AndTheLastMaterialStartsTheBuild()
    {
        var project = OpenProject(Starport(2));
        var parts = project.RequiredMaterials[Parts];
        var units = project.RequiredMaterials[Units];

        CityProjectRules.Accept(project, "a", "Alpha", Parts, parts - 1, Now);
        CityProjectRules.Accept(project, "b", "Beta", Parts, 5, Now.AddMinutes(1)).Should().Be(1, "only one part was still missing");
        project.SuppliedUtc.Should().BeNull("the power supply units are still missing");

        CityProjectRules.Accept(project, "c", "Gamma", Units, units, Now.AddHours(1));

        CityProjectRules.IsSupplied(project).Should().BeTrue();
        project.SuppliedUtc.Should().Be(Now.AddHours(1));
        project.CompletesUtc.Should().Be(Now.AddHours(1).AddDays(2));
        project.DonorMaterials["a"][Parts].Should().Be(parts - 1);
        project.DonorMaterials["b"][Parts].Should().Be(1);
    }

    [Test]
    public void ASuppliedProject_TakesNoMoreDonations_AndItsBuildTimeDoesNotRestart()
    {
        var project = OpenProject(Starport(2));
        foreach (var (resref, count) in project.RequiredMaterials.ToList())
            CityProjectRules.Accept(project, "a", "Alpha", resref, count, Now);
        var completes = project.CompletesUtc;

        CityProjectRules.AcceptsDonations(project).Should().BeFalse();
        CityProjectRules.Accept(project, "b", "Beta", Parts, 3, Now.AddDays(1)).Should().Be(0);

        project.CompletesUtc.Should().Be(completes);
    }

    [Test]
    public void ACancelledOrStartingProject_TakesNoDonations()
    {
        var project = Starport(2);

        CityProjectRules.AcceptsDonations(project).Should().BeFalse("the credits are not held yet");

        project.State = CityProjectState.Cancelling;
        CityProjectRules.Accept(project, "a", "Alpha", Parts, 1, Now).Should().Be(0);
    }

    [Test]
    public void DonatingNothingOrANonMaterial_ChangesNothing()
    {
        var project = OpenProject(Starport(2));

        CityProjectRules.Accept(project, "a", "Alpha", Parts, 0, Now).Should().Be(0);
        CityProjectRules.Accept(project, "a", "Alpha", Parts, -4, Now).Should().Be(0);
        CityProjectRules.Accept(project, "a", "Alpha", "not_a_material", 4, Now).Should().Be(0);
        CityProjectRules.Accept(project, "", "Alpha", Parts, 4, Now).Should().Be(0);

        project.DonorMaterials.Should().BeEmpty();
        project.DonatedMaterials.Should().BeEmpty();
    }

    #endregion

    #region Building and restarts

    private static CityProject SuppliedAt(DateTime supplied)
    {
        var project = OpenProject(Starport(2));
        foreach (var (resref, count) in project.RequiredMaterials.ToList())
            CityProjectRules.Accept(project, "a", "Alpha", resref, count, supplied);
        return project;
    }

    [Test]
    public void AProject_IsNotDueUntilTwoDaysAfterTheLastMaterial()
    {
        var project = SuppliedAt(Now);

        CityProjectRules.IsDue(project, Now.AddDays(2).AddSeconds(-1)).Should().BeFalse();
        CityProjectRules.IsDue(project, Now.AddDays(2)).Should().BeTrue();
    }

    [Test]
    public void AProjectNotFullySupplied_IsNeverDue()
    {
        var project = OpenProject(Starport(2));
        CityProjectRules.Accept(project, "a", "Alpha", Parts, 2, Now);

        CityProjectRules.IsDue(project, Now.AddDays(30)).Should().BeFalse();
    }

    [Test]
    public void ARestartBeforeTheTimeIsUp_ChangesNothing_AndOneAfterAppliesItWithTheTimeItAlwaysHad()
    {
        var project = SuppliedAt(Now);
        var city = City();
        var reloaded = Reload(project);

        // A restart a day in: not due, nothing applied, the stored time unchanged.
        CityProjectRules.IsDue(reloaded, Now.AddDays(1)).Should().BeFalse();
        reloaded.CompletesUtc.Should().Be(Now.AddDays(2));

        // The server was down when it came due and starts 5 days in: it is due, with the completion time it always had.
        CityProjectRules.IsDue(reloaded, Now.AddDays(5)).Should().BeTrue();
        reloaded.CompletesUtc.Should().Be(Now.AddDays(2));

        CityProjectRules.ApplyCompletion(city, reloaded);
        city.Upgrades[PropertyUpgradeType.StarportLevel].Should().Be(2);
    }

    [Test]
    public void AProjectNotOpen_IsNotDue()
    {
        var project = SuppliedAt(Now);
        project.State = CityProjectState.Cancelling;

        CityProjectRules.IsDue(project, Now.AddDays(9)).Should().BeFalse();
    }

    private static CityProject Reload(CityProject project)
    {
        return System.Text.Json.JsonSerializer.Deserialize<CityProject>(System.Text.Json.JsonSerializer.Serialize(project));
    }

    [Test]
    public void CompletingAFacilityProject_RaisesThatFacilityToTheLevelItBuilt()
    {
        var city = City();
        var project = Starport(2);

        CityProjectRules.ApplyCompletion(city, project);

        city.Upgrades[PropertyUpgradeType.StarportLevel].Should().Be(2);
        city.Upgrades[PropertyUpgradeType.MedicalCenterLevel].Should().Be(1, "only its own facility changes");
    }

    [Test]
    public void CompletingTwice_AfterARestart_NeverLowersOrRaisesAgain()
    {
        var city = City();
        city.Upgrades[PropertyUpgradeType.StarportLevel] = 3;

        CityProjectRules.ApplyCompletion(city, Starport(2));
        city.Upgrades[PropertyUpgradeType.StarportLevel].Should().Be(3, "a level is never lowered");

        var third = Starport(3);
        CityProjectRules.ApplyCompletion(city, third);
        CityProjectRules.ApplyCompletion(city, third);
        city.Upgrades[PropertyUpgradeType.StarportLevel].Should().Be(3);
    }

    [Test]
    public void TheJumpBeaconProject_RaisesTheBeaconLevel()
    {
        var city = City();
        var project = CityProjectRules.Create("city", CityProjectType.JumpBeaconUpgrade, 2, "mayor", Now);

        CityProjectRules.ApplyCompletion(city, project);

        city.Upgrades[PropertyUpgradeType.JumpBeaconLevel].Should().Be(2);
    }

    [Test]
    public void CompletingHomeGround_RecordsWhenItCompleted_NotWhenItWasApplied()
    {
        var city = City();
        var project = CityProjectRules.Create("city", CityProjectType.HomeGround, 0, "mayor", Now);
        project.CompletesUtc = Now.AddDays(2);

        CityProjectRules.ApplyCompletion(city, project);
        CityProjectRules.ApplyCompletion(city, project);

        city.HomeGroundCompletedUtc.Should().Be(Now.AddDays(2));
        CityProjectRules.IsHomeGroundBuilt(city, Now.AddDays(1)).Should().BeFalse();
        CityProjectRules.IsHomeGroundBuilt(city, Now.AddDays(2)).Should().BeTrue();
    }

    #endregion

    #region Donors and cancelling

    [Test]
    public void TheTopDonors_AreRankedByItemsGiven_AtMostThree()
    {
        var project = OpenProject(Starport(5));
        CityProjectRules.Accept(project, "small", "Small", Parts, 1, Now);
        CityProjectRules.Accept(project, "big", "Big", Parts, 4, Now);
        CityProjectRules.Accept(project, "mid", "Mid", Parts, 3, Now);
        CityProjectRules.Accept(project, "mid", "Mid", Units, 2, Now);
        CityProjectRules.Accept(project, "tiny", "Tiny", Units, 1, Now);

        var donors = CityProjectRules.GetTopDonors(project);

        donors.Select(donor => donor.Label).Should().Equal("Mid", "Big", "Small");
        donors.Select(donor => donor.Items).Should().Equal(5, 4, 1);
    }

    [Test]
    public void TheGazette_NamesTheTopDonors_ByTheNameStrangersKnowThem()
    {
        var project = OpenProject(Starport(2));
        CityProjectRules.Accept(project, "id-1", "a tall Twi'lek", Parts, 3, Now);

        var text = CityProjectRules.BuildCompletedText(project);

        text.Should().Contain("Starport level 2").And.Contain("a tall Twi'lek (3 items)");
        text.Should().NotContain("id-1");
        text.All(c => c < 128).Should().BeTrue("player text is plain ASCII");
    }

    [Test]
    public void ACompletedProjectWithNoDonors_StillGetsAGazetteLine()
    {
        CityProjectRules.BuildCompletedText(Starport(2)).Should().Be("The city finished its project: Starport level 2.");
    }

    [Test]
    public void ACancelledProject_OwesEachDonorTheirOwnMaterials()
    {
        var project = OpenProject(Starport(5));
        CityProjectRules.Accept(project, "b", "Beta", Parts, 3, Now);
        CityProjectRules.Accept(project, "a", "Alpha", Parts, 2, Now);
        CityProjectRules.Accept(project, "a", "Alpha", Units, 1, Now);

        var refunds = CityProjectRules.GetRefunds(project);

        refunds.Should().Equal(
            new CityProjectRefund("a", Parts, 2),
            new CityProjectRefund("a", Units, 1),
            new CityProjectRefund("b", Parts, 3));
        refunds.Sum(refund => refund.Count).Should().Be(CityProjectRules.GetDonatedCount(project));
    }

    [Test]
    public void ACancelledProjectWithNoDonations_OwesNothing()
    {
        CityProjectRules.GetRefunds(Starport(2)).Should().BeEmpty();
    }

    [Test]
    public void TheOverflowRecordIds_NeverChange_SoARefundRepeatedAfterARestartWritesTheSameRecords()
    {
        var first = CityProjectRules.GetOverflowRecordId("project", "donor", Parts, 0);
        var again = CityProjectRules.GetOverflowRecordId("project", "donor", Parts, 0);

        again.Should().Be(first);
        CityProjectRules.GetOverflowRecordId("project", "donor", Parts, 1).Should().NotBe(first);
        CityProjectRules.GetOverflowRecordId("project", "other", Parts, 0).Should().NotBe(first);
        CityProjectRules.GetOverflowRecordId("project", "donor", Units, 0).Should().NotBe(first);
    }

    [Test]
    public void TheProjectsText_IsPlainAscii()
    {
        var project = Starport(3);
        var texts = new[]
        {
            CityProjectRules.BuildStartedText(project, resref => resref),
            CityProjectRules.BuildCancelledText(project),
            CityProjectRules.DescribeStatus(project, Now),
            CityProjectRules.GetName(project)
        };

        foreach (var text in texts)
            text.All(c => c < 128).Should().BeTrue(text);
    }

    [Test]
    public void TheStatus_SaysWhatIsNeeded_OrWhenItCompletes()
    {
        var project = OpenProject(Starport(2));
        CityProjectRules.DescribeStatus(project, Now).Should().Contain("more materials");

        foreach (var (resref, count) in project.RequiredMaterials.ToList())
            CityProjectRules.Accept(project, "a", "Alpha", resref, count, Now);

        CityProjectRules.DescribeStatus(project, Now.AddHours(1)).Should().Contain("completes in 1d 23h");
        CityProjectRules.DescribeStatus(project, Now.AddDays(3)).Should().Contain("next server start");
    }

    #endregion
}
