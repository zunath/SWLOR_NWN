using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Research;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The database and engine side of research experiments: settling open jobs at login and logout, the file sequence stamps and
/// the Fabrication XP a job pays. The rules are covered by the experiment lifecycle tests.
/// </summary>
[TestFixture]
public class CraftResearchExperimentServiceTests
{
    private const uint Handle = 1001;

    private sealed class TestResearch : CraftResearchExperimentService
    {
        public TestResearch(IDBService db, ICraftService craft, ICraftSessionsService sessions)
            : base(db, craft, sessions)
        {
        }

        public List<string> Logs { get; } = new();

        protected override void WriteLog(string message) => Logs.Add(message);

        protected override int GetFabricationRank(uint player) => 5;
    }

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private Player _player = null!;
    private TestResearch _service = null!;
    private int _pendingGrants;

    [OneTimeSetUp]
    public void LoadTheSkills()
    {
        using var engine = new PlayerEngine();
        using var events = EventsPluginRuntime.Override(new FakeEventsPlugin());
        Skill.CacheData();
        Skill.CacheXPChartData();
    }

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _engine.Engine.SetReturn("GetName", "Tester");
        _db = new InMemoryDBService();
        _player = new Player(PlayerEngine.PlayerId);
        _db.With(_player);
        _pendingGrants = 0;

        var craft = ScriptedProxy.Create<ICraftService>(out var craftScript);
        craftScript.On("GetRecipe", _ => new RecipeDetail { Level = 20 });
        craftScript.On("get_SessionWorld", _ => new NwnCraftSessionWorld());
        var sessions = ScriptedProxy.Create<ICraftSessionsService>(out var sessionsScript);
        sessionsScript.On("GrantPendingXp", _ =>
        {
            _pendingGrants++;
            return true;
        });
        _service = new TestResearch(_db, craft, sessions);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    private ResearchJob OpenJob(string id, int startSequence = 0, int deliverySequence = 0) => new()
    {
        Id = id,
        PlayerId = PlayerEngine.PlayerId,
        Recipe = RecipeType.VenomCoating1,
        Level = 3,
        StartSequence = startSequence,
        DeliverySequence = deliverySequence,
        Experiment = ResearchExperimentLifecycle.Open(3, 4, 600, false)
    };

    private ResearchJob SettledJob(string id)
    {
        var job = OpenJob(id);
        job.Experiment.Status = ResearchExperimentStatus.Settled;
        return job;
    }

    [Test]
    public void SettleOpenExperiments_settles_each_open_job_of_the_player_and_counts_them()
    {
        _service.SettleOpenExperiments(" ").Should().Be(0);

        _db.With(OpenJob("open-job"));
        var settled = _service.SettleOpenExperiments(PlayerEngine.PlayerId);

        settled.Should().Be(1);
        _db.Saved.OfType<ResearchJob>().Single().Experiment.Status.Should().Be(ResearchExperimentStatus.Settled);
        _db.Queries.Should().HaveCount(1);
    }

    [Test]
    public void SettleOpenExperiments_leaves_a_job_without_an_open_experiment_alone()
    {
        _db.With(SettledJob("done-job"));

        _service.SettleOpenExperiments(PlayerEngine.PlayerId).Should().Be(0);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void RecoverAtLogin_does_nothing_without_a_player_id()
    {
        _engine.Engine.SetReturn("GetObjectUUID", "");

        _service.RecoverAtLogin(Handle);

        _db.Queries.Should().BeEmpty();
    }

    [Test]
    public void RecoverAtLogin_settles_each_job_against_the_character_file_just_loaded()
    {
        _engine.Engine.SetReturn("GetLocalInt", 5);
        var predatesStart = OpenJob("predates-start", startSequence: 9);
        var delivered = OpenJob("delivered", startSequence: 2, deliverySequence: 4);
        var notDelivered = OpenJob("not-delivered", startSequence: 2, deliverySequence: 8);
        var current = OpenJob("current", startSequence: 2);
        _player.PaidExperimentJobs = new HashSet<string> { "predates-start", "delivered", "not-delivered", "current" };
        foreach (var job in new[] { predatesStart, delivered, notDelivered, current })
            _db.With(job);

        _service.RecoverAtLogin(Handle);

        _db.Deleted.Should().BeEquivalentTo(new[] { "predates-start", "delivered" });
        notDelivered.DeliverySequence.Should().Be(0);
        _db.Saved.OfType<ResearchJob>().Should().Contain(notDelivered);
        current.DeliverySequence.Should().Be(0);
        _service.Logs.Should().HaveCount(2);
        _service.Logs[0].Should().Contain("predates-start").And.Contain("discarded");
        _service.Logs[1].Should().Contain("not-delivered").And.Contain("restored");
    }

    [Test]
    public void StampStart_and_FinishDelivery_stamp_the_next_file_sequence_and_save_the_job()
    {
        _engine.Engine.SetReturn("GetLocalInt", 6);
        var job = OpenJob("stamped");
        _db.With(job);

        _service.StampStart(Handle, job);
        job.StartSequence.Should().Be(7);
        _db.Saved.OfType<ResearchJob>().Should().Contain(job);

        _service.FinishDelivery(Handle, job);
        job.DeliverySequence.Should().Be(7);
        _db.Deleted.Should().Contain("stamped");
    }

    [Test]
    public void DeleteJob_removes_the_job_and_forgets_its_paid_mark()
    {
        var job = OpenJob("paid-job");
        _player.PaidExperimentJobs = new HashSet<string> { "paid-job", "other" };
        _db.With(job);

        _service.DeleteJob(null!);
        _service.DeleteJob(job);

        _db.Deleted.Should().Equal("paid-job");
        _player.PaidExperimentJobs.Should().Equal("other");
        _db.Saved.OfType<Player>().Should().ContainSingle();
    }

    [Test]
    public void DeleteJob_without_a_player_id_only_deletes_the_job()
    {
        var job = OpenJob("anonymous");
        job.PlayerId = null;

        _service.DeleteJob(job);

        _db.Deleted.Should().Equal("anonymous");
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void GrantExperimentXp_pays_nothing_for_a_job_that_is_not_settled_or_a_missing_player()
    {
        _service.GrantExperimentXp(Handle, null!).Should().Be(0);
        _service.GrantExperimentXp(Handle, OpenJob("open")).Should().Be(0);

        _engine.Engine.SetReturn("GetObjectUUID", "nobody");
        _service.GrantExperimentXp(Handle, SettledJob("settled")).Should().Be(0);
    }

    [Test]
    public void IsExperimentXpOwed_is_true_until_the_xp_is_recorded_as_paid()
    {
        var job = SettledJob("owed");

        _service.IsExperimentXpOwed(Handle, job).Should().BeTrue();

        _player.PaidExperimentJobs = new HashSet<string> { "owed" };
        _service.IsExperimentXpOwed(Handle, job).Should().BeFalse();
    }

    [Test]
    public void GrantCompletionXp_records_the_payout_once_and_grants_the_pending_xp()
    {
        var job = OpenJob("completed");

        var xp = _service.GrantCompletionXp(Handle, job, 4);

        xp.Should().Be(ResearchXpRules.CompletionXp(_service.BaseXp(Handle, job), 4));
        xp.Should().BeGreaterThan(0);
        _player.PaidExperimentJobs.Should().Contain(ResearchExperimentPayout.CompletionKey("completed"));
        _player.PendingCraftXp[SkillType.Fabrication].Should().Be(xp);
        _pendingGrants.Should().Be(1);

        _service.GrantCompletionXp(Handle, job, 4).Should().Be(0, "the completion was already paid");
        _player.PendingCraftXp[SkillType.Fabrication].Should().Be(xp);
        _pendingGrants.Should().Be(2);
    }

    [Test]
    public void GrantCompletionXp_pays_nothing_to_a_player_who_does_not_own_the_job_or_cannot_be_loaded()
    {
        var other = OpenJob("not-mine");
        other.PlayerId = "someone-else";
        _service.GrantCompletionXp(Handle, other, 4).Should().Be(0);

        _engine.Engine.SetReturn("GetObjectUUID", "nobody");
        _service.GrantCompletionXp(Handle, OpenJob("job"), 4).Should().Be(0);
        _pendingGrants.Should().Be(0);
    }

    [Test]
    public void BaseXp_is_the_chart_xp_of_the_recipe_level_against_the_fabrication_rank()
    {
        _service.BaseXp(Handle, OpenJob("job")).Should().Be(ResearchXpRules.BaseXp(20, 5));
    }
}
