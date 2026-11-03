using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The decisions behind a City Project donation across a restart (Player City Revamp, "City Projects"): what a record, the donor's
/// character mark and the project's receipt say about where the materials are, and what a cancelled project owes.
/// </summary>
public class CityProjectDonationRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private const string Parts = CityProjectCatalog.ReinforcedConstructionParts;
    private const string Units = CityProjectCatalog.ReinforcedPowerSupplyUnit;

    private static CityProject Open(int level = 2)
    {
        var project = CityProjectRules.Create("city", CityProjectType.StarportUpgrade, level, "mayor", Now);
        project.State = CityProjectState.Open;
        return project;
    }

    private static CityProjectDonation Donation(CityProject project, string donor = "donor", int count = 2) =>
        CityProjectDonationRules.Plan(project, donor, "A Donor", Parts, count, Now);

    #region Planning

    [Test]
    public void APlannedDonation_IsOnlyWhatTheProjectStillNeeds()
    {
        var project = Open();
        var needed = project.RequiredMaterials[Parts];

        var donation = CityProjectDonationRules.Plan(project, "donor", "A Donor", Parts, needed + 50, Now);

        donation.Count.Should().Be(needed);
        donation.PlayerId.Should().Be("donor");
        donation.ProjectId.Should().Be(project.Id);
        donation.Resref.Should().Be(Parts);
        donation.DonorLabel.Should().Be("A Donor");
        project.DonatedMaterials.Should().BeEmpty("planning changes nothing: the project takes the donation when it is applied");
    }

    [Test]
    public void NothingIsPlanned_ForNothingOffered_ANonMaterial_AnUnknownDonor_OrAProjectNotTakingDonations()
    {
        var project = Open();

        CityProjectDonationRules.Plan(project, "donor", "A Donor", Parts, 0, Now).Should().BeNull();
        CityProjectDonationRules.Plan(project, "donor", "A Donor", "not_a_material", 5, Now).Should().BeNull();
        CityProjectDonationRules.Plan(project, " ", "A Donor", Parts, 5, Now).Should().BeNull();
        CityProjectDonationRules.Plan(null, "donor", "A Donor", Parts, 5, Now).Should().BeNull();

        project.State = CityProjectState.Cancelling;
        CityProjectDonationRules.Plan(project, "donor", "A Donor", Parts, 5, Now).Should().BeNull();
    }

    #endregion

    #region Deciding

    [TestCase(false, false, false, CityProjectDonationSettlement.Drop)]
    [TestCase(false, false, true, CityProjectDonationSettlement.Drop)]
    [TestCase(true, true, false, CityProjectDonationSettlement.Forget)]
    [TestCase(true, true, true, CityProjectDonationSettlement.Forget)]
    [TestCase(true, false, true, CityProjectDonationSettlement.Apply)]
    [TestCase(true, false, false, CityProjectDonationSettlement.Return)]
    public void ARecord_IsSettledByItsMarkAndTheProjectsReceipt(bool marked, bool applied, bool takes, CityProjectDonationSettlement expected)
    {
        CityProjectDonationRules.Settle(marked, applied, takes).Should().Be(expected);
    }

    [Test]
    public void TheProject_TakesAllOfADonationOrNone_AndNeverTheSameOneTwice()
    {
        var project = Open();
        var needed = project.RequiredMaterials[Parts];
        var donation = Donation(project, count: needed);
        var tooMuch = Donation(project, "late", needed);
        CityProjectDonationRules.CanApply(project, donation).Should().BeTrue();

        // Someone else leaves less than the donation needs.
        CityProjectRules.Accept(project, "other", "Other", Parts, 1, Now).Should().Be(1);
        CityProjectDonationRules.CanApply(project, tooMuch).Should().BeFalse("a donation is taken whole or not at all");
        CityProjectDonationRules.Apply(project, tooMuch, Now).Should().BeFalse();
        project.DonatedMaterials[Parts].Should().Be(1, "a refused donation changes nothing");
        project.AppliedDonationIds.Should().BeEmpty();

        var small = Donation(project, count: 2);
        CityProjectDonationRules.Apply(project, small, Now).Should().BeTrue();
        CityProjectDonationRules.Apply(project, small, Now).Should().BeFalse("its receipt is on the project");
        project.DonatedMaterials[Parts].Should().Be(3);
        project.DonorMaterials["donor"][Parts].Should().Be(2);
        project.AppliedDonationIds.Should().Equal(small.Id);
        CityProjectDonationRules.IsApplied(project, small.Id).Should().BeTrue();
        CityProjectDonationRules.IsApplied(project, donation.Id).Should().BeFalse();
    }

    [Test]
    public void AClosedProject_OrOneThatIsGone_TakesNoDonation()
    {
        var project = Open();
        var donation = Donation(project);

        CityProjectDonationRules.CanApply(null, donation).Should().BeFalse();
        foreach (var state in new[] { CityProjectState.Starting, CityProjectState.Completing, CityProjectState.Cancelling })
        {
            project.State = state;
            CityProjectDonationRules.CanApply(project, donation).Should().BeFalse($"a {state} project takes nothing");
        }
    }

    [Test]
    public void TheLastDonation_StartsTheBuild_AtTheTimeItIsApplied()
    {
        var project = Open();
        var later = Now.AddDays(30);
        foreach (var material in project.RequiredMaterials)
        {
            var donation = CityProjectDonationRules.Plan(project, "donor", "A Donor", material.Key, material.Value, Now);
            CityProjectDonationRules.Apply(project, donation, later).Should().BeTrue();
        }

        project.SuppliedUtc.Should().Be(later);
        project.CompletesUtc.Should().Be(CityProjectRules.GetCompletionUtc(later));
    }

    #endregion

    #region Cancelling

    [Test]
    public void ACancelledProject_RefundsOnlyTheDonationsItHolds()
    {
        var project = Open();
        var held = Donation(project, "a", 3);
        var heldToo = CityProjectDonationRules.Plan(project, "b", "B", Units, 1, Now);
        var inFlight = Donation(project, "c", 4);
        CityProjectDonationRules.Apply(project, held, Now).Should().BeTrue();
        CityProjectDonationRules.Apply(project, heldToo, Now).Should().BeTrue();

        var refunds = CityProjectRules.GetRefunds(project);

        refunds.Should().BeEquivalentTo(new[]
        {
            new CityProjectRefund("a", Parts, 3),
            new CityProjectRefund("b", Units, 1)
        });
        refunds.Any(refund => refund.DonorId == "c").Should().BeFalse("a donation the project does not hold yet is not the project's to refund");
        CityProjectDonationRules.IsApplied(project, inFlight.Id).Should().BeFalse();
    }

    [Test]
    public void TheRecordsOfHeldDonations_GoBeforeTheProjectDoes()
    {
        var project = Open();
        var held = Donation(project, "a", 3);
        var waiting = Donation(project, "c", 4);
        CityProjectDonationRules.Apply(project, held, Now);

        CityProjectDonationRules.GetAppliedRecordIds(project, new[] { held, waiting }).Should().Equal(held.Id);
    }

    #endregion

    #region Marks

    [Test]
    public void TheMark_NamesEachDonationOnce_AndEmptiesWhenTheLastIsCleared()
    {
        var mark = CityProjectDonationRules.AddMark(string.Empty, "one");
        mark = CityProjectDonationRules.AddMark(mark, "two");
        mark = CityProjectDonationRules.AddMark(mark, "one");

        CityProjectDonationRules.ParseMarks(mark).Should().Equal("one", "two");

        mark = CityProjectDonationRules.RemoveMark(mark, "one");
        CityProjectDonationRules.ParseMarks(mark).Should().Equal("two");
        CityProjectDonationRules.RemoveMark(mark, "absent").Should().Be("two");
        CityProjectDonationRules.RemoveMark(mark, "two").Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase(",,")]
    public void ABlankMark_NamesNothing(string mark)
    {
        CityProjectDonationRules.ParseMarks(mark).Should().BeEmpty();
        CityProjectDonationRules.AddMark(mark, " ").Should().BeEmpty();
    }

    [Test]
    public void AMark_ReadsBackWhatWasWritten_WithOrWithoutSpaces()
    {
        CityProjectDonationRules.ParseMarks("a, b ,a").Should().Equal("a", "b");
    }

    #endregion

    #region Returning

    [Test]
    public void TheOverflowRecordIds_NeverChange_SoAReturnRepeatedAfterARestartWritesTheSameRecords()
    {
        var first = CityProjectDonationRules.GetOverflowRecordId("donation", 0);

        CityProjectDonationRules.GetOverflowRecordId("donation", 0).Should().Be(first);
        CityProjectDonationRules.GetOverflowRecordId("donation", 1).Should().NotBe(first);
        CityProjectDonationRules.GetOverflowRecordId("other", 0).Should().NotBe(first);
        first.Should().NotStartWith("city-project:", "a donation's records never collide with a cancelled project's refunds");
    }

    [Test]
    public void TheMessages_AreAscii_AndNameTheItemAndTheProject()
    {
        var project = Open(3);
        var donation = Donation(project, count: 4);

        var applied = CityProjectDonationRules.BuildAppliedText(donation, CityProjectRules.GetName(project), "Reinforced Construction Parts");
        var returned = CityProjectDonationRules.BuildReturnedText(donation, CityProjectRules.GetName(project), "Reinforced Construction Parts");

        applied.Should().Contain("4 Reinforced Construction Parts").And.Contain("Starport level 3");
        returned.Should().Contain("4 Reinforced Construction Parts").And.Contain("/overflow");
        applied.Concat(returned).Where(c => c > 0x7E).Should().BeEmpty("player-facing punctuation is ASCII");
    }

    #endregion
}
