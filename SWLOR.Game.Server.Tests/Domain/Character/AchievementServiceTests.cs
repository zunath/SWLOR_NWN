using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the achievement instance: the engine is replaced by recording fakes (NWScript and the player plugin)
/// and the database by an in-memory one, so no server is needed.
/// </summary>
[TestFixture]
public class AchievementServiceTests
{
    private const uint PlayerObject = 1001;
    private const string CdKey = "CDKEY0001";
    private const string PlayerId = "player-uuid-1";
    private const uint ObjectInvalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private FakePlayerPlugin _plugin = null!;
    private InMemoryDBService _db = null!;
    private AchievementService _service = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _pluginScope = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturn("GetIsDM", false);
        _engine.SetReturn("GetPCPublicCDKey", CdKey);
        _engine.SetReturn("GetObjectUUID", PlayerId);
        _plugin = new FakePlayerPlugin();
        _db = new InMemoryDBService();
        _service = new AchievementService(_db);
        _engineScope = NWScriptRuntime.Override(_engine);
        _pluginScope = PlayerPluginRuntime.Override(_plugin);
    }

    [TearDown]
    public void TearDown()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
    }

    private static List<AchievementType> ActiveTypes() =>
        Enum.GetValues(typeof(AchievementType)).Cast<AchievementType>()
            .Where(type => type.GetAttribute<AchievementType, AchievementAttribute>().IsActive)
            .ToList();

    // ---- the cache

    [Test]
    public void LoadAchievements_caches_exactly_the_active_achievements()
    {
        _service.LoadAchievements();

        _service.GetActiveAchievements().Keys.Should().BeEquivalentTo(ActiveTypes());
        _service.GetActiveAchievements().Should().NotContainKey(AchievementType.Invalid);
    }

    [Test]
    public void LoadAchievements_can_run_again_without_changing_the_cache()
    {
        _service.LoadAchievements();
        var first = _service.GetActiveAchievements();

        _service.LoadAchievements();

        _service.GetActiveAchievements().Should().BeEquivalentTo(first);
    }

    [Test]
    public void GetAchievement_returns_the_attribute_of_the_type()
    {
        _service.LoadAchievements();

        var achievement = _service.GetAchievement(AchievementType.KillEnemies1);

        achievement.Name.Should().Be("Kill Enemies I");
        achievement.Description.Should().Be("Kill 10 enemies.");
        achievement.IsActive.Should().BeTrue();
    }

    [Test]
    public void GetAchievement_fails_for_a_type_that_is_not_cached()
    {
        var beforeLoad = () => _service.GetAchievement(AchievementType.KillEnemies1);
        beforeLoad.Should().Throw<KeyNotFoundException>("nothing is cached before the module loads the achievements");

        _service.LoadAchievements();
        var inactive = () => _service.GetAchievement(AchievementType.Invalid);

        inactive.Should().Throw<KeyNotFoundException>("the Invalid achievement is not active, so it is never cached");
    }

    [Test]
    public void GetActiveAchievements_hands_out_a_copy()
    {
        _service.LoadAchievements();

        _service.GetActiveAchievements().Clear();

        _service.GetActiveAchievements().Should().NotBeEmpty();
    }

    [Test]
    public void Each_instance_keeps_its_own_cache()
    {
        var other = new AchievementService(new InMemoryDBService());

        _service.LoadAchievements();

        other.GetActiveAchievements().Should().BeEmpty("state lives on the instance, not in static fields");
    }

    // ---- the GUI ids

    [Test]
    public void ReserveGuiIds_reserves_six_ids_under_the_name_the_system_always_used()
    {
        _service.ReserveGuiIds();

        var reservation = Gui.GetSystemReservation("Achievement");
        reservation.Count.Should().Be(6);
        reservation.EndId.Should().Be(reservation.StartId + 5);
    }

    [Test]
    public void ReserveGuiIds_twice_keeps_the_same_reservation()
    {
        _service.ReserveGuiIds();
        var first = Gui.GetSystemReservation("Achievement").StartId;

        _service.ReserveGuiIds();
        new AchievementService(_db).ReserveGuiIds();

        Gui.GetSystemReservation("Achievement").StartId.Should().Be(first);
    }

    // ---- granting

    [Test]
    public void GiveAchievement_ignores_a_target_that_is_not_a_player()
    {
        _engine.SetReturn("GetIsPC", false);

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
        _engine.CountOf("GetPCPublicCDKey").Should().Be(0);
    }

    [Test]
    public void GiveAchievement_ignores_a_DM()
    {
        _engine.SetReturn("GetIsDM", true);

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        _db.Reads.Should().BeEmpty();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void GiveAchievement_does_nothing_when_the_account_already_has_it()
    {
        var account = new Account(CdKey);
        var earned = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        account.Achievements[AchievementType.KillEnemies1] = earned;
        _db.With(account).With(new Player { Id = PlayerId });
        PrepareGui();

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        _db.Saved.Should().BeEmpty();
        account.Achievements[AchievementType.KillEnemies1].Should().Be(earned, "the first time stays");
        _engine.CountOf("PostString").Should().Be(0);
    }

    [Test]
    public void GiveAchievement_saves_a_new_account_with_the_achievement_and_shows_the_notification()
    {
        _db.With(new Player { Id = PlayerId });
        PrepareGui();
        var before = DateTime.UtcNow;

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        var after = DateTime.UtcNow;
        var saved = _db.Saved.Should().ContainSingle().Subject.Should().BeOfType<Account>().Subject;
        saved.Id.Should().Be(CdKey);
        saved.Achievements.Keys.Should().Equal(AchievementType.KillEnemies1);
        saved.Achievements[AchievementType.KillEnemies1].Should().BeOnOrAfter(before).And.BeOnOrBefore(after);

        var posts = _engine.CallsTo("PostString");
        posts[0].Arguments[1].Should().Be("Achievement Unlocked");
        posts[1].Arguments[1].Should().Be(" Kill Enemies I");
        _plugin.CallsTo("PlaySound").Should().ContainSingle()
            .Which.Arguments.Should().Equal(PlayerObject, "gui_prompt", ObjectInvalid);
    }

    [Test]
    public void GiveAchievement_adds_to_the_account_that_is_already_stored()
    {
        var account = new Account(CdKey);
        account.Achievements[AchievementType.KillEnemies2] = DateTime.UtcNow.AddDays(-1);
        _db.With(account).With(new Player { Id = PlayerId });
        PrepareGui();

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(account);
        account.Achievements.Keys.Should().BeEquivalentTo(new[] { AchievementType.KillEnemies1, AchievementType.KillEnemies2 });
    }

    [Test]
    public void GiveAchievement_saves_but_shows_nothing_when_the_player_turned_notifications_off()
    {
        var player = new Player { Id = PlayerId };
        player.Settings.DisplayAchievementNotification = false;
        _db.With(player);
        PrepareGui();

        _service.GiveAchievement(PlayerObject, AchievementType.KillEnemies1);

        _db.Saved.Should().ContainSingle().Which.Should().BeOfType<Account>();
        _engine.CountOf("PostString").Should().Be(0);
        _plugin.CallsTo("PlaySound").Should().BeEmpty();
    }

    // ---- the window

    [Test]
    public void DisplayAchievementNotificationWindow_centres_the_name_and_draws_the_frame()
    {
        PrepareGui();
        var start = Gui.GetSystemReservation("Achievement").StartId;
        var centre = Gui.CenterStringInWindow("Kill Enemies I", 4, 26);

        _service.DisplayAchievementNotificationWindow(PlayerObject, "Kill Enemies I");

        var posts = _engine.CallsTo("PostString");
        posts[0].Arguments.Should().Equal(PlayerObject, "Achievement Unlocked", centre + 2, 5, ScreenAnchor.TopRight, 10.0f,
            Gui.ColorWhite, Gui.ColorYellow, start, Gui.TextName);
        posts[1].Arguments.Should().Equal(PlayerObject, " Kill Enemies I", centre + 4, 7, ScreenAnchor.TopRight, 10.0f,
            Gui.ColorWhite, Gui.ColorYellow, start + 1, Gui.TextName);
        posts.Should().HaveCountGreaterThan(2, "the frame is drawn after the two lines of text");
    }

    private void PrepareGui()
    {
        _service.ReserveGuiIds();
        _service.LoadAchievements();
    }
}
