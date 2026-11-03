using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A City Project donation across a restart (Player City Revamp, "City Projects"). A fake store stops the donation after any number of
/// its writes, as a restart would, drops what the character had not saved, lets the project change before the donor's next login
/// (another donor fills it, or it is cancelled), and then settles the donor's login, which can itself be stopped after any write.
/// However it goes, every item ends in exactly one place: the donor's inventory, the project, or the donor's overflow store.
/// </summary>
public class CityProjectDonationProcessTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private const string Parts = CityProjectCatalog.ReinforcedConstructionParts;
    private const string Donor = "donor";
    private const int Carried = 3;

    /// <summary>Thrown by the fake store at the write a restart stops the donation at.</summary>
    private sealed class RestartException : Exception
    {
    }

    /// <summary>What happens to the project between the restart and the donor's next login.</summary>
    public enum Meanwhile
    {
        NothingChanges,
        AnotherDonorFillsIt,
        AnotherDonorLeavesLessRoomThanTheDonation,
        TheProjectIsCancelled
    }

    /// <summary>
    /// The donor's character (live, and as the last save left it), the project, the donation records and the donor's overflow store, in
    /// memory. It counts every write and stops (throws) at the one a restart stops it at. A restart drops everything the character
    /// file did not save.
    /// </summary>
    private sealed class FakeStore : ICityProjectDonationStore
    {
        public FakeStore(CityProject project)
        {
            Project = project;
            Items = Carried;
            FileItems = Carried;
        }

        public CityProject Project { get; set; }

        public Dictionary<string, CityProjectDonation> Records { get; } = new();

        public Dictionary<string, int> Overflow { get; } = new();

        public int Items { get; private set; }

        public HashSet<string> Marks { get; private set; } = new();

        public int FileItems { get; private set; }

        public HashSet<string> FileMarks { get; private set; } = new();

        public List<string> Writes { get; } = new();

        public int? StopAfterWrites { get; set; }

        private void Write(string what)
        {
            if (StopAfterWrites.HasValue && Writes.Count >= StopAfterWrites.Value)
                throw new RestartException();

            Writes.Add(what);
        }

        private void SaveCharacter()
        {
            FileItems = Items;
            FileMarks = new HashSet<string>(Marks);
        }

        /// <summary>The server stops: the character comes back as the file holds it. The records and the project are in the database and stay.</summary>
        public void Restart()
        {
            Items = FileItems;
            Marks = new HashSet<string>(FileMarks);
            StopAfterWrites = null;
            Writes.Clear();
        }

        public void SavePending(CityProjectDonation donation)
        {
            Write("save");
            Records[donation.Id] = donation;
        }

        public void DeletePending(string donationId)
        {
            Write("delete");
            Records.Remove(donationId);
        }

        /// <summary>The donor carries fewer than the donation, so the materials cannot be taken.</summary>
        public bool CannotTake { get; set; }

        public bool TakeAndMark(CityProjectDonation donation)
        {
            // The materials leave the inventory before the file is saved (native destruction waits for the end of the script, so the
            // real store parks them in a holding container first), and the file is saved with the mark after. A stop between the two
            // leaves the file as it was: the materials in it and no mark.
            Write("park");
            if (CannotTake)
                return false;

            Items -= donation.Count;
            Write("mark");
            Marks.Add(donation.Id);
            SaveCharacter();
            return true;
        }

        public void ClearMark(string donationId)
        {
            Write("clear");
            Marks.Remove(donationId);
            SaveCharacter();
        }

        public bool IsApplied(CityProjectDonation donation) => CityProjectDonationRules.IsApplied(Project, donation.Id);

        public bool ProjectTakes(CityProjectDonation donation) => CityProjectDonationRules.CanApply(Project, donation);

        public void ApplyToProject(CityProjectDonation donation)
        {
            Write("apply");
            CityProjectDonationRules.Apply(Project, donation, Now);
        }

        public void ReturnToDonor(CityProjectDonation donation)
        {
            Write("return");
            Overflow[CityProjectDonationRules.GetOverflowRecordId(donation.Id, 0)] = donation.Count;
        }

        /// <summary>Cancels the project as the service does: each donor's held materials go to overflow, the records of held donations go, the project goes.</summary>
        public void CancelProject()
        {
            foreach (var refund in CityProjectRules.GetRefunds(Project))
            {
                Overflow[CityProjectRules.GetOverflowRecordId(Project.Id, refund.DonorId, refund.Resref, 0)] = refund.Count;
            }

            foreach (var id in CityProjectDonationRules.GetAppliedRecordIds(Project, Records.Values))
            {
                Records.Remove(id);
            }

            Project = null;
        }

        public int InProject => Project?.DonorMaterials.GetValueOrDefault(Donor)?.GetValueOrDefault(Parts) ?? 0;

        public int InOverflow => Overflow.Values.Sum();
    }

    private static CityProject OpenProject()
    {
        // Level 2 of the Starport needs 4 Reinforced Construction Parts, so a donation of 3 leaves 1.
        var project = CityProjectRules.Create("city", CityProjectType.StarportUpgrade, 2, "mayor", Now);
        project.State = CityProjectState.Open;
        return project;
    }

    private static CityProjectDonation Plan(CityProject project) =>
        CityProjectDonationRules.Plan(project, Donor, "A Donor", Parts, Carried, Now);

    private static void ApplyMeanwhile(FakeStore store, Meanwhile meanwhile)
    {
        switch (meanwhile)
        {
            case Meanwhile.AnotherDonorFillsIt:
                CityProjectRules.Accept(store.Project, "other", "Other", Parts, CityProjectRules.GetRemaining(store.Project, Parts), Now);
                break;
            case Meanwhile.AnotherDonorLeavesLessRoomThanTheDonation:
                CityProjectRules.Accept(store.Project, "other", "Other", Parts, CityProjectRules.GetRemaining(store.Project, Parts) - 1, Now);
                break;
            case Meanwhile.TheProjectIsCancelled:
                store.CancelProject();
                break;
        }
    }

    private static void AssertOneCopyOfEverything(FakeStore store)
    {
        (store.Items + store.InOverflow + store.InProject).Should().Be(Carried,
            $"every item is in one place: inventory {store.Items}, overflow {store.InOverflow}, project {store.InProject}");
        store.Records.Should().BeEmpty("no record is left to settle again");
        store.Marks.Should().BeEmpty("no mark is left to settle again");
        store.FileMarks.Should().BeEmpty("the cleared mark is saved too");
        store.Items.Should().Be(store.FileItems, "the character file was saved last");
        store.Project?.AppliedDonationIds.Distinct().Count().Should().Be(store.Project?.AppliedDonationIds.Count, "a receipt is written once");
    }

    private static void SettleLogin(FakeStore store)
    {
        CityProjectDonationProcess.SettleAtLogin(
            store,
            store.Marks.ToList(),
            store.Records.Values.Where(record => record.PlayerId == Donor).ToList());
    }

    private const int RunWrites = 6;
    private const int LoginWrites = 3;

    #region Without a restart

    [Test]
    public void ADonation_RecordsFirst_ThenTakesAndMarks_ThenAppliesToTheProject_ThenDeletesTheRecord_AndClearsTheMarkLast()
    {
        var store = new FakeStore(OpenProject());
        var donation = Plan(store.Project);

        var settlement = CityProjectDonationProcess.Run(store, donation);

        settlement.Should().Be(CityProjectDonationSettlement.Apply);
        store.Writes.Should().Equal("save", "park", "mark", "apply", "delete", "clear");
        store.Items.Should().Be(0);
        store.InProject.Should().Be(Carried);
        store.Project.DonatedMaterials[Parts].Should().Be(Carried);
        AssertOneCopyOfEverything(store);
    }

    [Test]
    public void ADonationTheProjectCannotTake_GoesToTheDonorsOverflow_NotLost()
    {
        var project = OpenProject();
        var store = new FakeStore(project);
        var donation = Plan(project);
        project.State = CityProjectState.Cancelling;

        var settlement = CityProjectDonationProcess.Run(store, donation);

        settlement.Should().Be(CityProjectDonationSettlement.Return);
        store.Writes.Should().Equal("save", "park", "mark", "return", "delete", "clear");
        store.InProject.Should().Be(0);
        store.InOverflow.Should().Be(Carried);
        AssertOneCopyOfEverything(store);
    }

    [Test]
    public void ADonationWhoseMaterialsCouldNotBeTaken_IsDropped_AndChangesNothing()
    {
        var store = new FakeStore(OpenProject()) { CannotTake = true };

        var settlement = CityProjectDonationProcess.Run(store, Plan(store.Project));

        settlement.Should().Be(CityProjectDonationSettlement.Drop);
        store.Writes.Should().Equal("save", "park", "delete");
        store.Records.Should().BeEmpty("the record is withdrawn");
        store.Marks.Should().BeEmpty();
        store.FileMarks.Should().BeEmpty();
        store.Items.Should().Be(Carried);
        store.InProject.Should().Be(0);
    }

    #endregion

    #region A restart at every step

    public static IEnumerable<TestCaseData> EveryRestartPoint()
    {
        foreach (var meanwhile in Enum.GetValues<Meanwhile>())
        {
            for (var runStop = 0; runStop <= RunWrites; runStop++)
            {
                for (var loginStop = 0; loginStop <= LoginWrites; loginStop++)
                {
                    yield return new TestCaseData(runStop, meanwhile, loginStop)
                        .SetName($"RestartAfterWrite{runStop}_Then{meanwhile}_LoginRestartedAfterWrite{loginStop}");
                }
            }
        }
    }

    [TestCaseSource(nameof(EveryRestartPoint))]
    public void AfterARestartAtAnyStep_AndAnotherDuringTheLogin_EveryItemEndsInOnePlace(int runStop, Meanwhile meanwhile, int loginStop)
    {
        var store = new FakeStore(OpenProject());
        var donation = Plan(store.Project);

        store.StopAfterWrites = runStop;
        try
        {
            CityProjectDonationProcess.Run(store, donation);
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, meanwhile);

        // The first login can be stopped too; the next one finishes the job.
        store.StopAfterWrites = loginStop;
        try
        {
            SettleLogin(store);
        }
        catch (RestartException)
        {
            store.Restart();
        }

        SettleLogin(store);

        AssertOneCopyOfEverything(store);
    }

    [TestCase(0, Meanwhile.NothingChanges)]
    [TestCase(1, Meanwhile.NothingChanges)]
    [TestCase(2, Meanwhile.NothingChanges)]
    [TestCase(0, Meanwhile.AnotherDonorFillsIt)]
    [TestCase(1, Meanwhile.TheProjectIsCancelled)]
    public void ARestartBeforeTheMaterialsLeave_LeavesThemWithTheDonor(int runStop, Meanwhile meanwhile)
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = runStop;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, meanwhile);
        SettleLogin(store);

        store.Items.Should().Be(Carried, "the character file never lost them");
        store.InOverflow.Should().Be(0);
        store.InProject.Should().Be(0, "the project never heard of the donation");
        AssertOneCopyOfEverything(store);
    }

    [Test]
    public void ARestartAfterTheMaterialsLeave_GivesTheProjectTheDonation_WhenItCanTakeIt()
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = 3;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        store.Items.Should().Be(0, "the character was saved without them");
        store.Marks.Should().HaveCount(1);
        store.Project.DonatedMaterials.Should().BeEmpty("the project has not heard of it yet");

        SettleLogin(store);

        store.InProject.Should().Be(Carried);
        store.InOverflow.Should().Be(0);
        AssertOneCopyOfEverything(store);
    }

    [TestCase(Meanwhile.AnotherDonorFillsIt)]
    [TestCase(Meanwhile.AnotherDonorLeavesLessRoomThanTheDonation)]
    [TestCase(Meanwhile.TheProjectIsCancelled)]
    public void ARestartAfterTheMaterialsLeave_ReturnsThemToTheDonor_WhenTheProjectCannotTakeThem(Meanwhile meanwhile)
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = 3;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        ApplyMeanwhile(store, meanwhile);
        SettleLogin(store);

        store.InProject.Should().Be(0, "the project never held the donation");
        store.InOverflow.Should().Be(Carried);
        AssertOneCopyOfEverything(store);
    }

    [TestCase(4)]
    [TestCase(5)]
    public void ARestartAfterTheProjectHeldTheDonation_NeverGivesItBackToTheDonor(int runStop)
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = runStop;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        // Another donor fills the rest, so the project is no longer open to this donation: it is already the project's.
        ApplyMeanwhile(store, Meanwhile.AnotherDonorFillsIt);
        SettleLogin(store);

        store.InProject.Should().Be(Carried);
        store.InOverflow.Should().Be(0);
        store.Items.Should().Be(0);
        AssertOneCopyOfEverything(store);
    }

    [TestCase(4)]
    [TestCase(5)]
    public void ACancelledProject_RefundsADonationItHeldBeforeARestart_Once(int runStop)
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = runStop;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        store.CancelProject();
        SettleLogin(store);

        store.InOverflow.Should().Be(Carried, "the cancel returned it once, and the login did not return it again");
        store.Items.Should().Be(0);
        AssertOneCopyOfEverything(store);
    }

    [Test]
    public void ALoginWithNothingToSettle_WritesNothing()
    {
        var store = new FakeStore(OpenProject());

        SettleLogin(store);

        store.Writes.Should().BeEmpty();
    }

    [Test]
    public void AMarkWithNoRecord_IsAFinishedDonation_AndOnlyTheMarkGoes()
    {
        var store = new FakeStore(OpenProject());
        store.StopAfterWrites = 5;
        try
        {
            CityProjectDonationProcess.Run(store, Plan(store.Project));
        }
        catch (RestartException)
        {
        }

        store.Restart();
        store.Records.Should().BeEmpty("the record went before the mark");
        store.Marks.Should().HaveCount(1);

        SettleLogin(store);

        store.Writes.Should().Equal("clear");
        store.InProject.Should().Be(Carried);
        AssertOneCopyOfEverything(store);
    }

    [Test]
    public void ARecordForAnotherDonor_IsNotTouchedByThisDonorsLogin()
    {
        var store = new FakeStore(OpenProject());
        var other = CityProjectDonationRules.Plan(store.Project, "other", "Other", Parts, 1, Now);
        store.Records[other.Id] = other;

        SettleLogin(store);

        store.Records.Should().ContainKey(other.Id);
        store.Writes.Should().BeEmpty();
    }

    #endregion
}
