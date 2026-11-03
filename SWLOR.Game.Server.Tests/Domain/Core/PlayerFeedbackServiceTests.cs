using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the player feedback instance: the engine and the player plugin are recording fakes with real local
/// variables, the database an in-memory one, the settings and the clock the test's own (the service reads them through
/// <c>protected virtual</c> methods) and the nearby broadcast a recorder.
/// </summary>
[TestFixture]
public class PlayerFeedbackServiceTests
{
    private const uint Player = 1001;
    private const uint Creature = 1002;
    private const uint Master = 1003;
    private const uint Invalid = 0x7F000000;
    private const string PlayerId = "player-uuid-1";
    private static readonly long Start = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc).Ticks;

    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private IDisposable _pluginScope = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private RecordingMessaging _messaging = null!;
    private TestPlayerFeedbackService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != Invalid);
        _engine.SetReturnHandler("GetObjectUUID", _ => PlayerId);
        Players.Clear();
        DMs.Clear();
        PossessedDMs.Clear();
        _engine.SetReturnHandler("GetIsPC", arguments => Players.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDM", arguments => DMs.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", arguments => PossessedDMs.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetName", _ => "Wolf");
        _locals = new LocalVariables(_engine);
        _engineScope = NWScriptRuntime.Override(_engine);
        _playerPlugin = new FakePlayerPlugin();
        _pluginScope = PlayerPluginRuntime.Override(_playerPlugin);
        _db = new InMemoryDBService();
        _messaging = new RecordingMessaging();
        _service = new TestPlayerFeedbackService(_db, _messaging)
        {
            Settings = TestSettings.Create("testing"),
            Now = Start
        };
    }

    [TearDown]
    public void TearDown()
    {
        _pluginScope.Dispose();
        _engineScope.Dispose();
    }

    private void MakePlayer(uint id, bool isPlayer = true, bool isDM = false, bool isPossessedDM = false)
    {
        if (isPlayer)
            Players.Add(id);
        if (isDM)
            DMs.Add(id);
        if (isPossessedDM)
            PossessedDMs.Add(id);
    }

    private HashSet<uint> Players { get; } = new();

    private HashSet<uint> DMs { get; } = new();

    private HashSet<uint> PossessedDMs { get; } = new();

    private void UseProductionSettings() => _service.Settings = TestSettings.Create("production");

    private string[] Messages() => _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToArray();

    // ---- diagnostics

    [TestCase(ServerEnvironmentType.Test, true, true)]
    [TestCase(ServerEnvironmentType.Development, true, true)]
    [TestCase(ServerEnvironmentType.Production, true, false)]
    [TestCase(ServerEnvironmentType.Production, false, false)]
    [TestCase(ServerEnvironmentType.Development, false, false)]
    [TestCase(ServerEnvironmentType.Test, false, false)]
    [TestCase(ServerEnvironmentType.Invalid, true, false)]
    [TestCase(ServerEnvironmentType.All, true, false)]
    public void Diagnostics_RequireAnExplicitDiagnosticEnvironment(
        ServerEnvironmentType environment, bool isExplicit, bool expected)
    {
        _service.AreDiagnosticsEnabled(environment, isExplicit).Should().Be(expected);
    }

    [TestCase("testing", true)]
    [TestCase("test", true)]
    [TestCase("development", true)]
    [TestCase("dev", true)]
    [TestCase("production", false)]
    [TestCase("prod", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("stagingg", false)]
    public void The_environment_setting_decides_whether_diagnostics_are_enabled(string? environment, bool expected)
    {
        _service.Settings = TestSettings.Create(environment);

        _service.DiagnosticsEnabled.Should().Be(expected);
    }

    [Test]
    public void The_servers_own_settings_are_the_default()
    {
        var plain = new PlayerFeedbackService(_db, _messaging);

        var read = () => plain.DiagnosticsEnabled;

        read.Should().NotThrow("the default reads ApplicationSettings.Get, which a process can always build");
    }

    [Test]
    public void A_diagnostic_message_reaches_the_player_in_testing()
    {
        _service.SendDiagnosticToPlayer(Player, "Detail");

        Messages().Should().Equal("Detail");
    }

    [Test]
    public void A_diagnostic_message_is_not_sent_in_production()
    {
        UseProductionSettings();

        _service.SendDiagnosticToPlayer(Player, "Detail");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void Diagnostic_floating_text_is_shown_in_testing_and_hidden_in_production()
    {
        _service.ShowDiagnosticFloatingText("Detail", Creature, true);
        UseProductionSettings();
        _service.ShowDiagnosticFloatingText("Hidden", Creature);

        var shown = _engine.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Subject;
        shown.Arguments.Should().Equal("Detail", Creature, true);
    }

    // ---- cooldown floating text

    private void AllowCooldownText(bool setting = true) =>
        _db.With(new Player { Id = PlayerId, Settings = new PlayerSettings { DisplayCooldownFloatingText = setting } });

    [Test]
    public void Cooldown_text_goes_privately_to_the_activating_player_without_a_chat_entry()
    {
        MakePlayer(Player);
        AllowCooldownText();

        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeTrue();

        var shown = _playerPlugin.CallsTo("FloatingTextStringOnCreature").Should().ContainSingle().Subject;
        shown.Arguments.Should().Equal(Player, Player, ColorToken.Yellow("Force Push: 3s"), false);
        _engine.CountOf("SendMessageToPC").Should().Be(0, "the normal denial stays in chat; this notification never adds another entry");
        _locals.String(Player, "FEEDBACK_COOLDOWN_FLOATING_LAST_SENT").Should().Be(Start.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void Cooldown_text_is_throttled_to_one_every_two_seconds_across_abilities()
    {
        MakePlayer(Player);
        AllowCooldownText();

        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeTrue();
        _service.Now = Start + TimeSpan.FromSeconds(1.9).Ticks;
        _service.ShowCooldownFloatingText(Player, "Force Leap", "2s").Should().BeFalse("the throttle is shared by every ability");
        _service.Now = Start + TimeSpan.FromSeconds(2).Ticks;
        _service.ShowCooldownFloatingText(Player, "Force Leap", "2s").Should().BeTrue();

        _playerPlugin.CountOf("FloatingTextStringOnCreature").Should().Be(2);
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Cooldown_text_is_only_for_players_who_are_not_dungeon_masters(bool isPlayer, bool isDM, bool isPossessedDM)
    {
        MakePlayer(Player, isPlayer, isDM, isPossessedDM);
        AllowCooldownText();

        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeFalse();

        _db.Reads.Should().BeEmpty("the settings are not even read");
        _playerPlugin.CountOf("FloatingTextStringOnCreature").Should().Be(0);
    }

    [Test]
    public void Cooldown_text_needs_the_player_setting()
    {
        MakePlayer(Player);
        AllowCooldownText(setting: false);

        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeFalse();
        _playerPlugin.CountOf("FloatingTextStringOnCreature").Should().Be(0);
    }

    [Test]
    public void Cooldown_text_needs_a_saved_player_with_settings()
    {
        MakePlayer(Player);

        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeFalse("no player is saved");

        _db.With(new Player { Id = PlayerId, Settings = null });
        _service.ShowCooldownFloatingText(Player, "Force Push", "3s").Should().BeFalse("the saved player has no settings");
        _playerPlugin.CountOf("FloatingTextStringOnCreature").Should().Be(0);
    }

    // ---- warnings

    [Test]
    public void A_warning_is_sent_to_a_player_once_per_interval()
    {
        MakePlayer(Player);
        UseProductionSettings();

        _service.SendWarningToPlayer(Player, "KEY", "First", 60);
        _service.Now = Start + TimeSpan.FromSeconds(59).Ticks;
        _service.SendWarningToPlayer(Player, "KEY", "Too soon", 60);
        _service.Now = Start + TimeSpan.FromSeconds(60).Ticks;
        _service.SendWarningToPlayer(Player, "KEY", "Again", 60);

        Messages().Should().Equal("First", "Again");
        _locals.String(Player, "FEEDBACK_WARNING_KEY").Should().Be(_service.Now.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void The_default_interval_is_a_minute()
    {
        MakePlayer(Player);
        UseProductionSettings();

        _service.SendWarningToPlayer(Player, "KEY", "First");
        _service.Now = Start + TimeSpan.FromSeconds(59).Ticks;
        _service.SendWarningToPlayer(Player, "KEY", "Too soon");

        Messages().Should().Equal("First");
    }

    [Test]
    public void Warnings_with_different_keys_are_limited_separately()
    {
        MakePlayer(Player);
        UseProductionSettings();

        _service.SendWarningToPlayer(Player, "ONE", "One", 60);
        _service.SendWarningToPlayer(Player, "TWO", "Two", 60);

        Messages().Should().Equal("One", "Two");
    }

    [Test]
    public void Every_warning_is_sent_in_testing()
    {
        MakePlayer(Player);

        _service.SendWarningToPlayer(Player, "KEY", "First", 60);
        _service.SendWarningToPlayer(Player, "KEY", "Second", 60);

        Messages().Should().Equal("First", "Second");
    }

    [Test]
    public void A_warning_is_only_for_players()
    {
        UseProductionSettings();

        _service.SendWarningToPlayer(Creature, "KEY", "Nobody home", 60);

        _engine.CountOf("SendMessageToPC").Should().Be(0);
        _engine.CountOf("SetLocalString").Should().Be(0, "the limit is not started for a non-player");
    }

    [Test]
    public void A_nearby_warning_goes_through_the_broadcast_once_per_interval_for_the_source_creature()
    {
        UseProductionSettings();
        BuildMessageDelegate build = receiver => $"Warning for {receiver}";

        _service.SendWarningNearby(Creature, "PARALYZED", build, 5, 12f);
        _service.Now = Start + TimeSpan.FromSeconds(4).Ticks;
        _service.SendWarningNearby(Creature, "PARALYZED", build, 5, 12f);
        _service.Now = Start + TimeSpan.FromSeconds(5).Ticks;
        _service.SendWarningNearby(Creature, "PARALYZED", build, 5, 12f);

        _messaging.Broadcasts.Should().HaveCount(2);
        _messaging.Broadcasts[0].Should().Be((Creature, build, 12f));
        _locals.String(Creature, "FEEDBACK_WARNING_PARALYZED").Should().Be(_service.Now.ToString(System.Globalization.CultureInfo.InvariantCulture), "the limit sits on the source creature");
    }

    [Test]
    public void A_nearby_warning_needs_a_valid_creature_and_a_message_builder()
    {
        UseProductionSettings();

        _service.SendWarningNearby(Invalid, "KEY", receiver => "text");
        _service.SendWarningNearby(Creature, "KEY", null!);

        _messaging.Broadcasts.Should().BeEmpty();
        _engine.CountOf("SetLocalString").Should().Be(0);
    }

    [Test]
    public void A_nearby_warning_uses_the_default_interval_and_range()
    {
        UseProductionSettings();
        BuildMessageDelegate build = receiver => "text";

        _service.SendWarningNearby(Creature, "KEY", build);

        _messaging.Broadcasts.Should().Equal((Creature, build, 10f));
    }

    [Test]
    public void Every_nearby_warning_is_sent_in_testing()
    {
        BuildMessageDelegate build = receiver => "text";

        _service.SendWarningNearby(Creature, "KEY", build, 60);
        _service.SendWarningNearby(Creature, "KEY", build, 60);

        _messaging.Broadcasts.Should().HaveCount(2);
    }

    [TestCase(2)]
    [TestCase(5)]
    [TestCase(60)]
    public void AutomaticWarnings_SendImmediatelyThenWaitForTheInterval(int intervalSeconds)
    {
        var now = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc).Ticks;
        _service.IsWarningDue(now, 0, intervalSeconds).Should().BeTrue();
        _service.IsWarningDue(now, now, intervalSeconds).Should().BeFalse();
        _service.IsWarningDue(now + TimeSpan.FromSeconds(intervalSeconds - 1).Ticks, now, intervalSeconds).Should().BeFalse();
        _service.IsWarningDue(now + TimeSpan.FromSeconds(intervalSeconds).Ticks, now, intervalSeconds).Should().BeTrue();
        _service.IsWarningDue(now, now + 1, intervalSeconds).Should().BeTrue("clock corrections must not silence warnings indefinitely");
    }

    [Test]
    public void A_negative_interval_is_treated_as_none()
    {
        _service.IsWarningDue(Start + 1, Start, -5).Should().BeTrue();
    }

    [Test]
    public void The_real_clock_is_the_default()
    {
        var plain = new PlayerFeedbackService(_db, _messaging);
        MakePlayer(Player);
        AllowCooldownText();
        var before = DateTime.UtcNow.Ticks;

        plain.ShowCooldownFloatingText(Player, "Force Push", "3s");

        long.Parse(_locals.String(Player, "FEEDBACK_COOLDOWN_FLOATING_LAST_SENT")).Should().BeInRange(before, DateTime.UtcNow.Ticks);
    }

    // ---- resource restored

    [TestCase(3, "STM", "Restored 3 STM.")]
    [TestCase(7, "FP", "Restored 7 FP.")]
    [TestCase(0, "STM", null)]
    [TestCase(-1, "FP", null)]
    public void ResourceFeedback_ReportsPositiveActualGainsOnly(int restored, string resource, string? expected)
    {
        _service.BuildResourceRestoredMessage(restored, resource).Should().Be(expected);
    }

    [Test]
    public void A_players_own_restoration_is_reported_to_them_in_testing()
    {
        MakePlayer(Player);

        _service.SendResourceRestored(Player, 3, "STM");

        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments
            .Should().Equal(Player, ColorToken.Combat("Restored 3 STM."));
    }

    [Test]
    public void A_companions_restoration_is_reported_to_its_owner_with_the_companions_name()
    {
        MakePlayer(Master);
        _engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == Creature ? Master : Invalid);
        _service.SendResourceRestored(Creature, 4, "FP");

        _engine.CallsTo("SendMessageToPC").Should().ContainSingle().Which.Arguments
            .Should().Equal(Master, ColorToken.Combat("Wolf: Restored 4 FP."));
    }

    [Test]
    public void Restoration_is_not_reported_in_production()
    {
        MakePlayer(Player);
        UseProductionSettings();

        _service.SendResourceRestored(Player, 3, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [TestCase(0)]
    [TestCase(-2)]
    public void Nothing_restored_is_not_reported(int restored)
    {
        MakePlayer(Player);

        _service.SendResourceRestored(Player, restored, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void An_invalid_creature_is_not_reported()
    {
        _service.SendResourceRestored(Invalid, 3, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void A_creature_with_no_player_owner_is_not_reported()
    {
        _engine.SetReturnHandler("GetMaster", _ => Invalid);

        _service.SendResourceRestored(Creature, 3, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void An_owner_that_is_not_a_player_is_not_told()
    {
        _engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == Creature ? Master : Invalid);

        _service.SendResourceRestored(Creature, 3, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0, "the owner is not a player character");
    }

    [Test]
    public void A_dungeon_master_is_not_told()
    {
        MakePlayer(Player, isPlayer: true, isDM: true);

        _service.SendResourceRestored(Player, 3, "STM");

        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    private sealed class RecordingMessaging : IMessagingService
    {
        public List<(uint Sender, BuildMessageDelegate Build, float Range)> Broadcasts { get; } = new();

        public void SendMessageNearbyToPlayers(uint sender, string message, float range = 10f) =>
            Broadcasts.Add((sender, _ => message, range));

        public void SendMessageNearbyToPlayers(uint sender, BuildMessageDelegate buildMessage, float range = 10f) =>
            Broadcasts.Add((sender, buildMessage, range));
    }

    private sealed class TestPlayerFeedbackService : PlayerFeedbackService
    {
        public TestPlayerFeedbackService(IDBService db, IMessagingService messaging)
            : base(db, messaging)
        {
        }

        public ApplicationSettings Settings { get; set; } = null!;

        public long Now { get; set; }

        protected override ApplicationSettings GetSettings() => Settings;

        protected override long UtcNowTicks() => Now;
    }
}
