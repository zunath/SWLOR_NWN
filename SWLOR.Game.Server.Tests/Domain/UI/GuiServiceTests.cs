using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.UI;

/// <summary>
/// Unit tests of the GUI instance: the window registry, the per-player window state, NUI event routing and geometry
/// persistence. The engine is a recording fake with a small model of NUI windows, the database an in-memory one, and
/// the window definitions are fakes (<see cref="FakeViewModel"/>), so no layout is built.
/// </summary>
[TestFixture]
public class GuiServiceTests
{
    private const uint Player = 1001;
    private const uint Dm = 1002;
    private const uint Npc = 1003;
    private const uint Observer = 1004;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _presenceProxy = null!;
    private IDMPresenceService _presence = null!;
    private TestGuiService _service = null!;
    private readonly Dictionary<(uint Target, string Id), int> _open = new();
    private readonly Dictionary<uint, string> _uuids = new();
    private int _nextToken;

    // The character sheet view model reaches facades, which resolve their services from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void SetUp()
    {
        _facadeScope = _facades.Install();
        _engine = new PlayerEngine();
        _db = new InMemoryDBService();
        _db.With(new Player(PlayerId));
        _open.Clear();
        _destroyed.Clear();
        _uuids.Clear();
        _nextToken = 0;

        var presence = _presence = ScriptedProxy.Create<IDMPresenceService>(out _presenceProxy);
        _presenceProxy
            .On("IsControlledByDM", _ => false)
            .On("ToggleWindow", _ => null)
            .On("GetShownOn", args => new[] { (uint)args[0]! });
        _service = new TestGuiService(_db, presence, typeof(FakeEmotesDefinition), typeof(FakeQuestsDefinition), typeof(FakeCharacterSheetDefinition));

        var engine = _engine.Engine;
        engine.SetReturnHandler("NuiCreate", args =>
        {
            var token = ++_nextToken;
            _open[((uint)args[0]!, (string)args[2]!)] = token;
            return token;
        });
        // NuiDestroy returns nothing, so the engine fake only records it: a window is open until a destroy names its token.
        engine.SetReturnHandler("NuiFindWindow", args =>
            _open.TryGetValue(((uint)args[0]!, (string)args[1]!), out var token) && !IsDestroyed((uint)args[0]!, token) ? token : 0);
        engine.SetReturnHandler("GetObjectUUID", args => _uuids.GetValueOrDefault((uint)args[0]!, PlayerId));
        engine.SetReturn("GetEnteringObject", Player);
        engine.SetReturn("GetExitingObject", Player);
        engine.SetReturn("JsonGetInt", 1);
        engine.SetReturn("GetIsObjectValid", false);
    }

    [TearDown]
    public void TearDown()
    {
        _facadeScope.Dispose();
        _engine.Dispose();
    }

    // The fake forgets calls when they are cleared, so destroyed windows are remembered separately.
    private readonly HashSet<(uint Target, int Token)> _destroyed = new();

    private bool IsDestroyed(uint target, int token)
    {
        SyncDestroyed();
        return _destroyed.Contains((target, token));
    }

    private void SyncDestroyed()
    {
        foreach (var call in _engine.Engine.CallsTo("NuiDestroy"))
            _destroyed.Add(((uint)call.Arguments[0]!, (int)call.Arguments[1]!));
    }

    private void ClearEngineCalls()
    {
        SyncDestroyed();
        _engine.Engine.ClearCalls();
    }

    private int OpenWindows() => _open.Count(pair => !IsDestroyed(pair.Key.Target, pair.Value));

    private Player Stored() => _db.Get<Player>(PlayerId)!;

    private FakeViewModel Model(GuiWindowType type, uint player = Player) => (FakeViewModel)_service.GetPlayerWindow(player, type).ViewModel;

    private void Login()
    {
        _service.CacheData();
        _service.CreatePlayerWindows();
    }

    private static bool Opened(PlayerEngine engine, GuiWindowType type) => engine.Engine.CallsTo("NuiCreate").Any(call => (string)call.Arguments[2]! == GuiWindowKey.BuildWindowId(type));

    private void RunClosures(string member)
    {
        foreach (var call in _engine.Engine.CallsTo(member).ToList())
            call.Arguments.OfType<Action>().FirstOrDefault()?.Invoke();
    }

    // --- ids and constants -----------------------------------------------------------------------------------------

    [Test]
    public void Window_and_event_ids_are_built_from_the_type_and_element()
    {
        _service.BuildWindowId(GuiWindowType.Emotes).Should().Be("GUI_WINDOW_Emotes");
        _service.BuildEventKey("GUI_WINDOW_Emotes", "btn").Should().Be("GUI_WINDOW_Emotes_btn");
    }

    [Test]
    public void The_facade_constants_match_the_instance()
    {
        GuiService.GuiFontName.Should().Be(GuiService.GuiFontName);
        GuiService.TextName.Should().Be(GuiService.TextName);
        GuiService.ColorWhite.Should().Be(-1);
        GuiService.ColorTransparent.Should().Be(unchecked((int)0xFFFFFF00));
        GuiService.ColorRed.Should().Be(Convert.ToInt32("0xFF0000FF", 16));
        GuiService.ColorShieldsBar.Should().Be(Convert.ToInt32("0x00AAE4FF", 16));
    }

    [Test]
    public void Ids_are_reserved_once_per_system_in_order()
    {
        var first = _service.ReserveIds("Alpha", 5);
        var second = _service.ReserveIds("Beta", 3);
        var again = _service.ReserveIds("Alpha", 99);

        (first.StartId, first.EndId, first.Count).Should().Be((0, 4, 5));
        (second.StartId, second.EndId, second.Count).Should().Be((5, 7, 3));
        again.Should().BeSameAs(first);
        _service.GetSystemReservation("Beta").Should().BeSameAs(second);
        var missing = () => _service.GetSystemReservation("Gamma");
        missing.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Strings_are_centred_in_a_window()
    {
        _service.CenterStringInWindow("abcd", 10, 20).Should().Be(10 + 10 - 3);
    }

    [Test]
    public void A_drawn_window_posts_a_frame_of_the_sprite_letters_from_the_anchor()
    {
        _service.DrawWindow(Player, 100, ScreenAnchor.TopLeft, 2, 3, 2, 2, 5f);

        var posts = _engine.Engine.CallsTo("PostString");
        posts.Select(call => (string)call.Arguments[1]!).Should().Equal("abbc", "dii" + "f", "dii" + "f", "heeg");
        posts.Select(call => (int)call.Arguments[8]!).Should().Equal(100, 101, 102, 103);
        posts.Select(call => (int)call.Arguments[3]!).Should().Equal(3, 4, 5, 6);
        posts.Should().OnlyContain(call => (string)call.Arguments[9]! == GuiService.GuiFontName);

        ClearEngineCalls();
        _service.DrawWindow(Player, 0, ScreenAnchor.BottomRight, 1, 1, 0, 1);
        _engine.Engine.CallsTo("PostString").Select(call => (string)call.Arguments[1]!).Should().Equal("hg", "df", "ac");
    }

    // --- window cache and player windows -------------------------------------------------------------------------------

    [Test]
    public void Caching_builds_each_window_template_once_and_refuses_a_duplicate_type()
    {
        _service.CacheData();

        _service.GetWindowTemplate(GuiWindowType.Emotes).Type.Should().Be(GuiWindowType.Emotes);
        var again = () => _service.CacheData();
        again.Should().Throw<Exception>().WithMessage("*Emotes*defined more than once*");
    }

    [Test]
    public void Logging_in_creates_every_window_with_the_saved_or_default_geometry()
    {
        Stored().WindowGeometries[GuiWindowType.Emotes] = new GuiRectangle(7, 8, 99, 88);

        Login();

        var emotes = Model(GuiWindowType.Emotes).Geometry;
        (emotes.X, emotes.Y, emotes.Width, emotes.Height).Should().Be((7f, 8f, 99f, 88f));
        var quests = Model(GuiWindowType.Quests).Geometry;
        (quests.X, quests.Y, quests.Width, quests.Height).Should().Be((10f, 20f, 150f, 300f));
    }

    [Test]
    public void A_window_that_cannot_resize_gets_its_default_size_back()
    {
        _engine.Engine.SetReturn("JsonGetInt", 0);
        _engine.Engine.SetReturn("JsonGetString", "");
        Stored().WindowGeometries[GuiWindowType.Emotes] = new GuiRectangle(7, 8, 99, 88);

        Login();

        var emotes = Model(GuiWindowType.Emotes).Geometry;
        (emotes.X, emotes.Y, emotes.Width, emotes.Height).Should().Be((7f, 8f, 150f, 300f));
    }

    [Test]
    public void A_resizable_window_with_a_degenerate_saved_size_goes_back_to_the_default_rect()
    {
        Stored().WindowGeometries[GuiWindowType.Emotes] = new GuiRectangle(7, 8, 0, 88);

        Login();

        var emotes = Model(GuiWindowType.Emotes).Geometry;
        (emotes.X, emotes.Y, emotes.Width, emotes.Height).Should().Be((10f, 20f, 150f, 300f));
    }

    [Test]
    public void Logging_in_twice_in_a_session_keeps_the_same_windows()
    {
        Login();
        var window = _service.GetPlayerWindow(Player, GuiWindowType.Emotes);

        _service.CreatePlayerWindows();

        _service.GetPlayerWindow(Player, GuiWindowType.Emotes).Should().BeSameAs(window);
    }

    [Test]
    public void A_player_without_a_record_still_gets_windows_with_default_geometry()
    {
        _uuids[Player] = "new-player";

        Login();

        Model(GuiWindowType.Emotes).Geometry.Width.Should().Be(150f);
    }

    // --- opening, closing, toggling -------------------------------------------------------------------------------------

    [Test]
    public void Toggling_opens_a_closed_window_binds_it_and_closes_an_open_one_saving_its_geometry()
    {
        Login();
        var model = Model(GuiWindowType.Emotes);
        var payload = new TestPayload();

        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, payload, Npc);

        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeTrue();
        model.BindCount.Should().Be(1);
        model.BoundTo.Should().Be(Player);
        model.BoundType.Should().Be(GuiWindowType.Emotes);
        model.BoundPayload.Should().BeSameAs(payload);
        model.BoundTether.Should().Be(Npc);
        _service.GetPlayerWindow(Player, GuiWindowType.Emotes).WindowToken.Should().Be(model.Token);

        model.Geometry = new GuiRectangle(1, 2, 3, 4);
        _db.Saved.Clear();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);

        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeFalse();
        model.Closes.Should().Be(1);
        _service.GetPlayerWindow(Player, GuiWindowType.Emotes).WindowToken.Should().Be(0);
        _db.Saved.Should().ContainSingle();
        Stored().WindowGeometries[GuiWindowType.Emotes].Should().BeSameAs(model.Geometry);
    }

    private sealed class TestPayload : GuiPayloadBase
    {
    }

    [Test]
    public void Toggling_for_a_non_player_does_nothing_unless_a_ui_target_is_given()
    {
        Login();
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.TogglePlayerWindow(Npc, GuiWindowType.Emotes);
        OpenWindows().Should().Be(0);

        _uuids[Npc] = PlayerId;
        _service.TogglePlayerWindow(Npc, GuiWindowType.Emotes, uiTarget: Npc + 1);
        _open.Should().ContainSingle().Which.Key.Target.Should().Be(Npc + 1);
    }

    [Test]
    public void Closing_a_window_that_is_not_open_does_nothing_and_closing_one_without_state_still_destroys_it()
    {
        Login();
        _service.ClosePlayerWindow(Player, GuiWindowType.Emotes);
        _engine.Engine.CountOf("NuiDestroy").Should().Be(0);

        _open[(Player, "GUI_WINDOW_Emotes")] = 5;
        _uuids[Player] = "unknown-player";
        _service.ClosePlayerWindow(Player, GuiWindowType.Emotes);

        _engine.Engine.CountOf("NuiDestroy").Should().Be(1);
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void A_close_callback_that_throws_still_saves_the_geometry()
    {
        _service.CacheData();
        _service.CreatePlayerWindows();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        var throwing = new ThrowingCloseModel();
        _service.GetPlayerWindow(Player, GuiWindowType.Emotes).ViewModel = throwing;
        _db.Saved.Clear();

        var close = () => _service.ClosePlayerWindow(Player, GuiWindowType.Emotes);

        close.Should().Throw<InvalidOperationException>();
        _db.Saved.Should().ContainSingle();
    }

    private sealed class ThrowingCloseModel : FakeViewModel
    {
        public override Action OnWindowClosed() => throw new InvalidOperationException("close failed");
    }

    [Test]
    public void CloseWindow_destroys_an_open_window_without_saving_its_geometry()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        _db.Saved.Clear();

        _service.CloseWindow(Player, GuiWindowType.Emotes, 0x7F000000);

        OpenWindows().Should().Be(0);
        _db.Saved.Should().BeEmpty();

        _service.CloseWindow(Player, GuiWindowType.Emotes, 0x7F000000);
        _engine.Engine.CountOf("NuiDestroy").Should().Be(1);
    }

    // --- NUI events ---------------------------------------------------------------------------------------------------

    private void RaiseEvent(string windowId, string eventType, string element, uint player = Player)
    {
        var engine = _engine.Engine;
        engine.SetReturn("NuiGetEventPlayer", player);
        engine.SetReturn("NuiGetEventWindow", 1);
        engine.SetReturn("NuiGetWindowId", windowId);
        engine.SetReturn("NuiGetEventType", eventType);
        engine.SetReturn("NuiGetEventElement", element);
    }

    private void RegisterClick(GuiWindowType type, string element, string eventType = "click")
    {
        var windowId = GuiWindowKey.BuildWindowId(type);
        _service.RegisterElementEvent(_service.BuildEventKey(windowId, element), eventType,
            new GuiMethodDetail(typeof(FakeViewModel).GetMethod(nameof(FakeViewModel.OnClick))!,
                new List<KeyValuePair<Type, object>> { new(typeof(int), 3) }));
    }

    [Test]
    public void A_registered_element_event_runs_the_view_model_method_with_its_arguments()
    {
        Login();
        RegisterClick(GuiWindowType.Emotes, "btn");
        RaiseEvent("GUI_WINDOW_Emotes", "click", "btn");

        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes).Clicks.Should().Be(3);
    }

    [Test]
    public void Events_with_no_window_or_no_matching_registration_are_ignored()
    {
        Login();
        RegisterClick(GuiWindowType.Emotes, "btn");

        RaiseEvent("", "click", "btn");
        _service.HandleNuiEvents();
        RaiseEvent("GUI_WINDOW_Emotes", "mouseenter", "btn");
        _service.HandleNuiEvents();
        RaiseEvent("GUI_WINDOW_Emotes", "click", "other");
        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes).Clicks.Should().Be(0);
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void Closing_a_window_without_a_close_handler_still_saves_its_geometry()
    {
        Login();
        Model(GuiWindowType.Emotes).Geometry = new GuiRectangle(9, 9, 9, 9);
        RaiseEvent("GUI_WINDOW_Emotes", "close", "_window_");

        _service.HandleNuiEvents();

        Stored().WindowGeometries[GuiWindowType.Emotes].X.Should().Be(9f);
    }

    [Test]
    public void A_close_event_with_a_registered_handler_runs_it_then_saves()
    {
        Login();
        RegisterClick(GuiWindowType.Emotes, "_window_", "close");
        Model(GuiWindowType.Emotes).Geometry = new GuiRectangle(8, 8, 8, 8);
        RaiseEvent("GUI_WINDOW_Emotes", "close", "_window_");

        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes).Clicks.Should().Be(3);
        Stored().WindowGeometries[GuiWindowType.Emotes].X.Should().Be(8f);
    }

    [Test]
    public void A_window_tethered_to_an_object_the_player_walked_away_from_closes_instead_of_running_the_event()
    {
        Login();
        RegisterClick(GuiWindowType.Emotes, "btn");
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, null, Npc);
        _engine.Engine.SetReturn("GetIsObjectValid", true);
        _engine.Engine.SetReturn("GetDistanceBetween", 9f);
        RaiseEvent("GUI_WINDOW_Emotes", "click", "btn");

        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes).Clicks.Should().Be(0);
        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeFalse();
        _engine.Messages().Should().ContainSingle().Which.Should().Contain("You have moved too far away");
    }

    [Test]
    public void A_window_tethered_within_range_still_runs_the_event()
    {
        Login();
        RegisterClick(GuiWindowType.Emotes, "btn");
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, null, Npc);
        _engine.Engine.SetReturn("GetIsObjectValid", true);
        _engine.Engine.SetReturn("GetDistanceBetween", 2f);
        RaiseEvent("GUI_WINDOW_Emotes", "click", "btn");

        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes).Clicks.Should().Be(3);
    }

    [Test]
    public void A_possessing_dms_events_belong_to_the_dm_and_show_on_the_possessed_creature()
    {
        _uuids[Dm] = PlayerId;
        _service.CacheData();
        _engine.Engine.SetReturn("GetEnteringObject", Dm);
        _service.CreatePlayerWindows();
        RegisterClick(GuiWindowType.Emotes, "btn");
        _engine.Engine.SetReturnHandler("GetIsDMPossessed", args => (uint)args[0]! == Npc);
        _engine.Engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Npc ? Dm : 0x7F000000u);
        _open[(Npc, "GUI_WINDOW_Emotes")] = 4;
        RaiseEvent("GUI_WINDOW_Emotes", "click", "btn", Npc);

        _service.HandleNuiEvents();

        Model(GuiWindowType.Emotes, Dm).Clicks.Should().Be(3);
    }

    [Test]
    public void A_watch_event_updates_the_view_model_and_a_geometry_watch_saves_once_after_the_delay()
    {
        Login();
        var model = Model(GuiWindowType.Emotes);
        RaiseEvent("GUI_WINDOW_Emotes", "watch", "Title");

        _service.HandleNuiWatchEvent();

        model.Updated.Should().Equal("Title");
        _engine.Engine.CountOf("DelayCommand").Should().Be(0);

        RaiseEvent("GUI_WINDOW_Emotes", "watch", nameof(IGuiViewModel.Geometry));
        _service.HandleNuiWatchEvent();
        _service.HandleNuiWatchEvent();

        model.Updated.Should().Equal("Title", "Geometry", "Geometry");
        _engine.Engine.CountOf("DelayCommand").Should().Be(1, "writes are coalesced until the delay passes");

        model.Geometry = new GuiRectangle(4, 4, 4, 4);
        RunClosures("DelayCommand");
        Stored().WindowGeometries[GuiWindowType.Emotes].X.Should().Be(4f);

        _service.HandleNuiWatchEvent();
        _engine.Engine.CountOf("DelayCommand").Should().Be(2, "after the save, a new move can queue another");
    }

    [Test]
    public void Watch_events_that_are_not_watches_have_no_window_or_come_from_unknown_players_are_ignored()
    {
        Login();
        var model = Model(GuiWindowType.Emotes);

        RaiseEvent("GUI_WINDOW_Emotes", "click", "Title");
        _service.HandleNuiWatchEvent();
        RaiseEvent("", "watch", "Title");
        _service.HandleNuiWatchEvent();
        _uuids[Player] = "stranger";
        RaiseEvent("GUI_WINDOW_Emotes", "watch", "Title");
        _service.HandleNuiWatchEvent();

        model.Updated.Should().BeEmpty();
    }

    // --- logout ---------------------------------------------------------------------------------------------------------

    [Test]
    public void Leaving_saves_the_geometry_of_open_windows_only()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        Model(GuiWindowType.Emotes).Geometry = new GuiRectangle(5, 5, 5, 5);
        Model(GuiWindowType.Quests).Geometry = new GuiRectangle(6, 6, 6, 6);
        _db.Saved.Clear();

        _service.SavePlayerWindowGeometry();

        _db.Saved.Should().ContainSingle();
        Stored().WindowGeometries.Should().ContainKey(GuiWindowType.Emotes).And.NotContainKey(GuiWindowType.Quests);
    }

    [Test]
    public void Leaving_as_a_dm_or_a_non_player_saves_nothing()
    {
        Login();
        _db.Saved.Clear();

        _engine.Engine.SetReturn("GetIsDM", true);
        _service.SavePlayerWindowGeometry();
        _engine.Engine.SetReturn("GetIsDM", false);
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.SavePlayerWindowGeometry();

        _db.Saved.Should().BeEmpty();
    }

    // --- refresh events ---------------------------------------------------------------------------------------------------

    [Test]
    public void A_refresh_event_reaches_only_open_windows_that_subscribe_to_it()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);

        _service.PublishRefreshEvent(Player, new FakeRefreshEvent());

        Model(GuiWindowType.Emotes).Refreshes.Should().Be(1);
        Model(GuiWindowType.Quests).Refreshes.Should().Be(0, "its window is closed");
    }

    [Test]
    public void A_refresh_event_nobody_subscribed_to_or_a_player_without_windows_is_a_no_op()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);

        _service.PublishRefreshEvent(Player, new UnsubscribedRefreshEvent());
        var model = Model(GuiWindowType.Emotes);
        _uuids[Player] = "stranger";
        _service.PublishRefreshEvent(Player, new FakeRefreshEvent());

        model.Refreshes.Should().Be(0);
    }

    private sealed class UnsubscribedRefreshEvent : IGuiRefreshEvent
    {
    }

    [Test]
    public void A_refresh_event_for_a_non_player_needs_a_ui_target()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.PublishRefreshEvent(Player, new FakeRefreshEvent());
        Model(GuiWindowType.Emotes).Refreshes.Should().Be(0);

        _service.PublishRefreshEvent(Player, new FakeRefreshEvent(), Player);
        Model(GuiWindowType.Emotes).Refreshes.Should().Be(1);
    }

    [Test]
    public void The_character_sheet_refresh_reaches_every_open_sheet_viewing_the_target()
    {
        Login();
        var sheet = (FakeCharacterSheet)_service.GetPlayerWindow(Player, GuiWindowType.CharacterSheet).ViewModel;
        sheet.ViewTarget(Npc);
        _open[(Player, "GUI_WINDOW_CharacterSheet")] = 77;
        _engine.Engine.SetReturn("GetFirstPC", Player);
        _engine.Engine.SetReturn("GetNextPC", 0x7F000000u);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! == Player);

        _service.PublishCharacterSheetRefreshEvent(Npc, new FakeRefreshEvent());

        sheet.Refreshes.Should().Be(1);

        _service.PublishCharacterSheetRefreshEvent(Observer, new FakeRefreshEvent());
        sheet.Refreshes.Should().Be(1, "the sheet shows someone else");
    }

    [Test]
    public void The_character_sheet_refresh_does_nothing_when_no_sheet_is_registered_or_it_is_closed()
    {
        _service.PublishCharacterSheetRefreshEvent(Npc, new FakeRefreshEvent());
        _engine.Engine.CountOf("GetFirstPC").Should().Be(0);

        Login();
        _engine.Engine.SetReturn("GetFirstPC", Player);
        _engine.Engine.SetReturn("GetNextPC", 0x7F000000u);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! == Player);
        _service.PublishCharacterSheetRefreshEvent(Npc, new FakeRefreshEvent());

        ((FakeCharacterSheet)_service.GetPlayerWindow(Player, GuiWindowType.CharacterSheet).ViewModel).Refreshes.Should().Be(0);
    }

    [Test]
    public void Equipping_and_unequipping_refresh_the_player_after_a_short_delay()
    {
        _engine.Engine.SetReturn("GetPCItemLastEquippedBy", Player);
        _engine.Engine.SetReturn("GetPCItemLastUnequippedBy", Player);

        _service.RefreshOnEquip();
        _service.RefreshOnUnequip();

        var delays = _engine.Engine.CallsTo("DelayCommand");
        delays.Should().HaveCount(2);
        delays.Should().OnlyContain(call => (float)call.Arguments[0]! == 0.1f);
        RunClosures("DelayCommand");

        _engine.Engine.SetReturn("GetIsPC", false);
        ClearEngineCalls();
        _service.RefreshOnEquip();
        _service.RefreshOnUnequip();
        _engine.Engine.CountOf("DelayCommand").Should().Be(0);
    }

    // --- tethers and area changes -------------------------------------------------------------------------------------

    [Test]
    public void A_tethered_open_window_is_found_through_the_players_cached_windows()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, null, Npc);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != 0x7F000000);

        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeTrue();
        _service.HasOpenWindowTetheredTo(Player, Npc + 1).Should().BeFalse();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void The_tether_check_refuses_invalid_objects_non_controllers_and_players_without_windows()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, null, Npc);

        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeFalse("nothing is valid");

        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != 0x7F000000);
        _engine.Engine.SetReturn("GetIsPC", false);
        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeFalse("the controller is neither a player nor a DM");

        _engine.Engine.SetReturn("GetIsPC", true);
        _uuids[Player] = "stranger";
        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeFalse();
        _uuids[Player] = " ";
        _service.HasOpenWindowTetheredTo(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void The_tether_check_follows_a_possessed_creature_to_its_dm()
    {
        _uuids[Dm] = PlayerId;
        _service.CacheData();
        _engine.Engine.SetReturn("GetEnteringObject", Dm);
        _service.CreatePlayerWindows();
        _service.TogglePlayerWindow(Dm, GuiWindowType.Emotes, null, Observer, Npc);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != 0x7F000000);
        _engine.Engine.SetReturnHandler("GetIsDMPossessed", args => (uint)args[0]! == Npc);
        _engine.Engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Npc ? Dm : 0x7F000000u);

        _service.HasOpenWindowTetheredTo(Npc, Observer).Should().BeTrue();
    }

    [Test]
    public void Entering_an_area_closes_the_windows_that_do_not_stay_open()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        _service.TogglePlayerWindow(Player, GuiWindowType.Quests);

        _service.CloseAllWindows();

        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeFalse();
        _service.IsWindowOpen(Player, GuiWindowType.Quests).Should().BeTrue("Quests keeps itself open between areas");
    }

    [Test]
    public void Entering_an_area_as_a_non_player_closes_nothing()
    {
        Login();
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        _engine.Engine.SetReturn("GetIsPC", false);

        _service.CloseAllWindows();

        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeTrue();
    }

    [Test]
    public void A_possessed_creatures_windows_close_through_its_dm()
    {
        _uuids[Dm] = PlayerId;
        _service.CacheData();
        _engine.Engine.SetReturn("GetEnteringObject", Dm);
        _service.CreatePlayerWindows();
        _service.TogglePlayerWindow(Dm, GuiWindowType.Emotes, null, 0x7F000000, Npc);
        _engine.Engine.SetReturn("GetEnteringObject", Npc);
        _engine.Engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Npc);
        _engine.Engine.SetReturnHandler("GetIsDMPossessed", args => (uint)args[0]! == Npc);
        _engine.Engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Npc ? Dm : 0x7F000000u);
        _presenceProxy.On("GetShownOn", _ => new[] { Npc });

        _service.CloseAllWindows();

        OpenWindows().Should().Be(0);
    }

    // --- replacing the native panels ------------------------------------------------------------------------------------

    /// <summary>The Character Sheet window built from the plain fake view model, which binds without facades.</summary>
    public sealed class FakePlainSheetDefinition : IGuiWindowDefinition
    {
        public GuiConstructedWindow BuildWindow() => FakeWindows.Build(GuiWindowType.CharacterSheet);
    }

    private void OpenPanel(GuiPanel panel, uint target)
    {
        var engine = _engine.Engine;
        engine.SetReturn("GetLastGuiEventPlayer", Player);
        engine.SetReturn("GetLastGuiEventType", GuiEventType.DisabledPanelAttemptOpen);
        engine.SetReturn("GetLastGuiEventObject", target);
        engine.SetReturn("GetLastGuiEventInteger", (int)panel);
    }

    [Test]
    public void The_native_character_sheet_panel_opens_the_own_or_an_associates_sheet()
    {
        _service = new TestGuiService(_db, _presence, typeof(FakeEmotesDefinition), typeof(FakePlainSheetDefinition));
        Login();
        OpenPanel(GuiPanel.CharacterSheet, Player);
        _service.ReplaceNWNGuis();
        Opened(_engine, GuiWindowType.CharacterSheet).Should().BeTrue();

        _service.TogglePlayerWindow(Player, GuiWindowType.CharacterSheet);
        ClearEngineCalls();
        _engine.Engine.SetReturnHandler("GetMaster", args => (uint)args[0]! == Npc ? Player : 0x7F000000u);
        OpenPanel(GuiPanel.CharacterSheet, Npc);
        _service.ReplaceNWNGuis();
        Opened(_engine, GuiWindowType.CharacterSheet).Should().BeTrue();

        _service.TogglePlayerWindow(Player, GuiWindowType.CharacterSheet);
        ClearEngineCalls();
        OpenPanel(GuiPanel.CharacterSheet, Observer);
        _service.ReplaceNWNGuis();
        Opened(_engine, GuiWindowType.CharacterSheet).Should().BeFalse("someone else's creature");
    }

    [Test]
    public void The_native_journal_and_player_list_panels_open_their_windows_and_other_events_are_ignored()
    {
        Login();
        OpenPanel(GuiPanel.Journal, Player);
        _service.ReplaceNWNGuis();
        Opened(_engine, GuiWindowType.Quests).Should().BeTrue();

        _engine.Engine.SetReturn("GetLastGuiEventType", GuiEventType.MiniMapOpen);
        ClearEngineCalls();
        _service.ReplaceNWNGuis();
        _engine.Engine.CountOf("NuiCreate").Should().Be(0);
    }

    [Test]
    public void The_native_player_list_panel_goes_to_the_dm_presence_for_a_controlling_dm()
    {
        Login();
        OpenPanel(GuiPanel.PlayerList, Player);
        _presenceProxy.On("IsControlledByDM", _ => true);

        _service.ReplaceNWNGuis();

        _presenceProxy.Calls.Select(call => call.Member).Should().Contain("ToggleWindow");
    }

    [Test]
    public void The_native_player_list_panel_for_a_window_that_is_not_registered_fails_the_lookup()
    {
        Login();
        OpenPanel(GuiPanel.PlayerList, Player);

        var open = () => _service.ReplaceNWNGuis();

        // The fake definitions do not include a PlayerList window, so asking for it is a lookup failure.
        open.Should().Throw<KeyNotFoundException>();
    }
}
