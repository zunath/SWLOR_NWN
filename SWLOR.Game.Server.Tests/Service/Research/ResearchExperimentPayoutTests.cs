using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>
/// Covers the experiment XP payout across a server stop at every point between its writes: each stop ends with the XP paid
/// exactly once, or at worst waiting in the player's pending XP, never twice.
/// </summary>
public class ResearchExperimentPayoutTests
{
    private const string JobId = "job-1";
    private const int ChartXp = 600;

    /// <summary>The XP a level 5 experiment that used all its trials pays on a chart XP of 600.</summary>
    private const int Expected = 558;

    private sealed class CrashException : Exception;

    /// <summary>A database of two separate records, each saved as a copy, with a point where the server can stop.</summary>
    private sealed class FakeWorld : IResearchPayoutWorld
    {
        public Player PersistedPlayer { get; private set; }
        public ResearchJob PersistedJob { get; private set; }
        public List<string> Events { get; } = new();
        public int XpGiven { get; private set; }
        public bool Alive { get; set; } = true;
        public string CrashAt { get; set; }

        public bool CanReceiveXp() => Alive;

        public int BaseXp(ResearchJob job) => ChartXp;

        public void SavePlayer(Player dbPlayer)
        {
            Events.Add("savePlayer");
            Crash("savePlayer");
            PersistedPlayer = Clone(dbPlayer);
        }

        public void SaveJob(ResearchJob job)
        {
            Events.Add("saveJob");
            Crash("saveJob");
            PersistedJob = Clone(job);
        }

        /// <summary>The craft session's rule: the record is cleared and saved first, then the XP is given. Nothing happens while dead.</summary>
        public void GrantPendingXp()
        {
            Events.Add("grant");
            Crash("grant");
            if (!Alive || PersistedPlayer?.PendingCraftXp == null || PersistedPlayer.PendingCraftXp.Count == 0)
                return;

            var owed = PersistedPlayer.PendingCraftXp.Values.Sum();
            PersistedPlayer.PendingCraftXp.Clear();
            XpGiven += owed;
        }

        private void Crash(string point)
        {
            if (CrashAt != point)
                return;

            CrashAt = null;
            throw new CrashException();
        }
    }

    private static T Clone<T>(T value) => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(value));

    private static ResearchJob SettledJob() => new()
    {
        Id = JobId,
        PlayerId = "player-1",
        Experiment = new ResearchExperimentRecord
        {
            Status = ResearchExperimentStatus.Settled,
            TargetLevel = 5,
            TrialsTotal = 6,
            TrialsUsed = 6
        }
    };

    private static (FakeWorld World, Player Player, ResearchJob Job) Fresh()
    {
        var world = new FakeWorld();
        var player = new Player("player-1");
        world.SavePlayer(player);
        var job = SettledJob();
        world.SaveJob(job);
        world.Events.Clear();
        return (world, player, job);
    }

    /// <summary>A restart: the next open reads both records as they were saved.</summary>
    private static int Reopen(FakeWorld world)
    {
        var player = Clone(world.PersistedPlayer);
        var job = Clone(world.PersistedJob);
        return new ResearchExperimentPayout(world).Pay(player, job);
    }

    private static void PayAndStop(FakeWorld world, Player player, ResearchJob job, string crashAt)
    {
        world.CrashAt = crashAt;
        var act = () => new ResearchExperimentPayout(world).Pay(player, job);
        act.Should().Throw<CrashException>();
    }

    [Test]
    public void ThePaymentIsRecordedWithAnIdAndXpInOnePlayerSave_ThenTheJobIsMarked_ThenTheXpIsGiven()
    {
        var (world, player, job) = Fresh();

        var xp = new ResearchExperimentPayout(world).Pay(player, job);

        xp.Should().Be(Expected);
        world.Events.Should().Equal("savePlayer", "saveJob", "grant");
        world.XpGiven.Should().Be(Expected);
        world.PersistedJob.Experiment.XpGranted.Should().BeTrue();
        world.PersistedPlayer.PaidExperimentJobs.Should().Contain(JobId);
    }

    [Test]
    public void TheIdAndTheXpAreSavedTogether_SoNoStopSeparatesThem()
    {
        var (world, player, job) = Fresh();

        PayAndStop(world, player, job, "saveJob");

        world.PersistedPlayer.PaidExperimentJobs.Should().Contain(JobId);
        world.PersistedPlayer.PendingCraftXp[SkillType.Fabrication].Should().Be(Expected);
    }

    [Test]
    public void AStopBeforeThePlayerSave_PaysNothing_AndTheNextOpenPaysItOnce()
    {
        var (world, player, job) = Fresh();

        PayAndStop(world, player, job, "savePlayer");

        world.PersistedPlayer.PaidExperimentJobs.Should().BeEmpty();
        world.PersistedPlayer.PendingCraftXp.Should().BeEmpty();
        world.PersistedJob.Experiment.XpGranted.Should().BeFalse();
        world.XpGiven.Should().Be(0);

        Reopen(world).Should().Be(Expected);
        Reopen(world).Should().Be(0);
        world.XpGiven.Should().Be(Expected);
    }

    [Test]
    public void AStopAfterThePlayerSaveAndBeforeTheJobSave_IsRepairedByTheNextOpen_WithoutPayingTwice()
    {
        var (world, player, job) = Fresh();

        PayAndStop(world, player, job, "saveJob");

        world.PersistedJob.Experiment.XpGranted.Should().BeFalse("the saved job still shows the XP as owed");
        world.XpGiven.Should().Be(0);

        Reopen(world).Should().Be(0, "the player's record says it is paid, so nothing is paid again");

        world.PersistedJob.Experiment.XpGranted.Should().BeTrue();
        world.XpGiven.Should().Be(Expected);

        Reopen(world).Should().Be(0);
        world.XpGiven.Should().Be(Expected, "a third open adds nothing");
    }

    [Test]
    public void AStopAfterTheJobSaveAndBeforeTheGrant_LeavesTheXpWaiting_AndItIsGivenOnceLater()
    {
        var (world, player, job) = Fresh();

        PayAndStop(world, player, job, "grant");

        world.PersistedJob.Experiment.XpGranted.Should().BeTrue();
        world.PersistedPlayer.PendingCraftXp[SkillType.Fabrication].Should().Be(Expected, "it waits for the respawn, login or crafting window");
        world.XpGiven.Should().Be(0);
        ResearchExperimentLifecycle.IsExperimentXpOwed(Clone(world.PersistedJob)).Should().BeFalse("the job is marked, so an open does not pay it again");

        world.GrantPendingXp();
        world.XpGiven.Should().Be(Expected);

        world.GrantPendingXp();
        Reopen(world).Should().Be(0);
        world.XpGiven.Should().Be(Expected);
    }

    [Test]
    public void ARepeatOpen_PaysNothingMore()
    {
        var (world, player, job) = Fresh();
        var payout = new ResearchExperimentPayout(world);
        payout.Pay(player, job).Should().Be(Expected);

        payout.Pay(player, job).Should().Be(0);
        Reopen(world).Should().Be(0);

        world.XpGiven.Should().Be(Expected);
    }

    [Test]
    public void AJobInThePlayersSet_IsNeverPaidAgain_EvenWhenItsOwnMarkWasLost()
    {
        var (world, player, job) = Fresh();
        player.PaidExperimentJobs.Add(JobId);

        new ResearchExperimentPayout(world).Pay(player, job).Should().Be(0);

        job.Experiment.XpGranted.Should().BeTrue("the job is marked and saved");
        world.PersistedJob.Experiment.XpGranted.Should().BeTrue();
        world.Events.Should().Equal("saveJob", "grant");
        world.XpGiven.Should().Be(0);
    }

    [Test]
    public void AJobMarkedByAnEarlierBuild_IsNotPaidAgain()
    {
        var (world, player, job) = Fresh();
        job.Experiment.XpGranted = true;

        new ResearchExperimentPayout(world).Pay(player, job).Should().Be(0);

        world.Events.Should().BeEmpty();
        player.PaidExperimentJobs.Should().BeEmpty();
    }

    [Test]
    public void ADeadPlayer_IsPaidNothingAndNothingIsMarked_UntilTheyAreAlive()
    {
        var (world, player, job) = Fresh();
        world.Alive = false;
        var payout = new ResearchExperimentPayout(world);

        payout.Pay(player, job).Should().Be(0);

        world.Events.Should().BeEmpty();
        job.Experiment.XpGranted.Should().BeFalse();
        player.PaidExperimentJobs.Should().BeEmpty();

        world.Alive = true;
        payout.Pay(player, job).Should().Be(Expected);
        world.XpGiven.Should().Be(Expected);
    }

    [Test]
    public void APaidJobOfAPlayerWhoDiedBeforeTheGrant_KeepsItsXpWaitingUntilTheyAreAlive()
    {
        var (world, player, job) = Fresh();
        PayAndStop(world, player, job, "grant");
        world.Alive = false;

        Reopen(world).Should().Be(0);
        world.XpGiven.Should().Be(0);
        world.PersistedPlayer.PendingCraftXp[SkillType.Fabrication].Should().Be(Expected);

        world.Alive = true;
        world.GrantPendingXp();
        world.XpGiven.Should().Be(Expected);
    }

    [Test]
    public void AnExperimentThatEarnedNothing_StillRecordsThePayoutAndMarksTheJob()
    {
        var (world, player, job) = Fresh();
        job.Experiment.TrialsUsed = 0;

        new ResearchExperimentPayout(world).Pay(player, job).Should().Be(0);

        world.PersistedPlayer.PaidExperimentJobs.Should().Contain(JobId);
        world.PersistedPlayer.PendingCraftXp.Should().BeEmpty();
        world.PersistedJob.Experiment.XpGranted.Should().BeTrue();
    }

    [Test]
    public void AnUnsettledExperimentOrAMissingRecord_IsNeverPaid()
    {
        var (world, player, job) = Fresh();
        job.Experiment.Status = ResearchExperimentStatus.InProgress;
        var payout = new ResearchExperimentPayout(world);

        payout.Pay(player, job).Should().Be(0);
        payout.Pay(player, new ResearchJob()).Should().Be(0);
        payout.Pay(null, SettledJob()).Should().Be(0);

        world.Events.Should().BeEmpty();
    }

    [Test]
    public void IsOwed_ChecksThePlayersSetAsWellAsTheJobsMark()
    {
        var player = new Player("player-1");
        var job = SettledJob();

        ResearchExperimentPayout.IsOwed(player, job).Should().BeTrue();

        player.PaidExperimentJobs.Add(JobId);
        ResearchExperimentPayout.IsOwed(player, job).Should().BeFalse("the player's record says it is paid");
        ResearchExperimentPayout.IsPaid(player, job).Should().BeTrue();

        player.PaidExperimentJobs.Clear();
        job.Experiment.XpGranted = true;
        ResearchExperimentPayout.IsOwed(player, job).Should().BeFalse();
        ResearchExperimentPayout.IsOwed(player, null).Should().BeFalse();
    }

    [Test]
    public void Forget_TakesADeletedJobsIdOffTheSet_AndOnlyThatId()
    {
        var player = new Player("player-1");
        player.PaidExperimentJobs.Add(JobId);
        player.PaidExperimentJobs.Add("job-2");

        ResearchExperimentPayout.Forget(player, JobId).Should().BeTrue();
        ResearchExperimentPayout.Forget(player, JobId).Should().BeFalse("it is already gone");
        ResearchExperimentPayout.Forget(player, null).Should().BeFalse();
        ResearchExperimentPayout.Forget(null, JobId).Should().BeFalse();

        player.PaidExperimentJobs.Should().BeEquivalentTo(new[] { "job-2" });
    }

    [Test]
    public void ARecordSavedBeforeTheSetExisted_ReadsAsNotPaid_AndTheFirstPayoutCreatesTheSet()
    {
        var player = new Player("player-1") { PaidExperimentJobs = null };
        var world = new FakeWorld();
        var job = SettledJob();

        ResearchExperimentPayout.IsPaid(player, job).Should().BeFalse();

        new ResearchExperimentPayout(world).Pay(player, job).Should().Be(Expected);

        player.PaidExperimentJobs.Should().Contain(JobId);
    }
}
