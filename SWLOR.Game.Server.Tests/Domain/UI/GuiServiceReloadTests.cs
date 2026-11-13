using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.UI;

/// <summary>
/// What a hot reload does with the open NUI windows (P4-15, <see cref="GuiService.CloseWindowsForReload"/>): the old build closes
/// every one as the player would, the new build reopens those whose view model takes no payload, and a carried conversation's
/// window leaves the screen without its close callback.
/// </summary>
[TestFixture]
public class GuiServiceReloadTests
{
    private const uint Player = 1001;
    private const uint Tether = 2002;
    private const uint Invalid = 0x7F000000;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private TestGuiService _service = null!;
    private readonly Dictionary<(uint Target, string Id), int> _open = new();
    private readonly HashSet<int> _destroyed = new();
    private int _nextToken;
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
        _nextToken = 0;
        var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceProxy);
        presenceProxy.On("GetShownOn", args => new[] { (uint)args[0]! }).On("IsControlledByDM", _ => false);
        _service = new TestGuiService(_db, presence, typeof(FakeEmotesDefinition), typeof(FakeQuestsDefinition));

        var engine = _engine.Engine;
        engine.SetReturnHandler("NuiCreate", args =>
        {
            var token = ++_nextToken;
            _open[((uint)args[0]!, (string)args[2]!)] = token;
            return token;
        });
        engine.SetReturnHandler("NuiFindWindow", args =>
        {
            foreach (var call in engine.CallsTo("NuiDestroy"))
                _destroyed.Add((int)call.Arguments[1]!);
            return _open.TryGetValue(((uint)args[0]!, (string)args[1]!), out var token) && !_destroyed.Contains(token) ? token : 0;
        });
        engine.SetReturn("GetObjectUUID", PlayerId);
        engine.SetReturn("JsonGetInt", 1);
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! == Player || (uint)args[0]! == Tether);

        _service.CacheData();
        _service.CreatePlayerWindowsFor(Player);
    }

    [TearDown]
    public void TearDown()
    {
        _facadeScope.Dispose();
        _engine.Dispose();
    }

    private FakeViewModel Model(GuiWindowType type) => (FakeViewModel)_service.GetPlayerWindow(Player, type).ViewModel;

    [Test]
    public void The_old_build_closes_every_open_window_as_the_player_would()
    {
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes, tetherObject: Tether);
        _service.TogglePlayerWindow(Player, GuiWindowType.Quests);
        Model(GuiWindowType.Emotes).Geometry = new GuiRectangle(5, 6, 70, 80);

        var reopen = _service.CloseWindowsForReload(Player);

        reopen.Should().BeEmpty("the fake view models are not payload-free view models of the game");
        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeFalse();
        _service.IsWindowOpen(Player, GuiWindowType.Quests).Should().BeFalse();
        Model(GuiWindowType.Emotes).Closes.Should().Be(1, "the close callback runs as when the player closes the window");
        Model(GuiWindowType.Quests).Closes.Should().Be(1);
        _db.Get<Player>(PlayerId)!.WindowGeometries[GuiWindowType.Emotes].Should().BeEquivalentTo(new GuiRectangle(5, 6, 70, 80));
        _service.CloseWindowsForReload(Player).Should().BeEmpty("nothing is open any more");
    }

    [Test]
    public void The_new_build_reopens_the_carried_windows_unless_open_or_their_object_is_gone()
    {
        _service.TogglePlayerWindow(Player, GuiWindowType.Quests);
        var created = _engine.Engine.CountOf("NuiCreate");

        _service.ReopenWindowsForReload(Player, new[]
        {
            new GuiReloadWindow(GuiWindowType.Quests, Player, Invalid),
            new GuiReloadWindow(GuiWindowType.Emotes, 999, Invalid),
            new GuiReloadWindow(GuiWindowType.Emotes, Player, 998),
            new GuiReloadWindow(GuiWindowType.Emotes, Player, Tether),
        });

        _engine.Engine.CountOf("NuiCreate").Should().Be(created + 1, "only the closed window on a valid object with a valid tether opens");
        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeTrue();
        Model(GuiWindowType.Emotes).BoundPayload.Should().BeNull("a reopened window gets no payload, as when the player opens it");
        Model(GuiWindowType.Emotes).BoundTether.Should().Be(Tether);
    }

    [Test]
    public void A_detached_window_leaves_the_screen_without_its_close_callback()
    {
        _service.TogglePlayerWindow(Player, GuiWindowType.Emotes);
        Model(GuiWindowType.Emotes).Geometry = new GuiRectangle(1, 2, 30, 40);

        _service.DetachWindowForReload(Player, GuiWindowType.Emotes, Invalid);

        _service.IsWindowOpen(Player, GuiWindowType.Emotes).Should().BeFalse();
        Model(GuiWindowType.Emotes).Closes.Should().Be(0);
        _service.GetPlayerWindow(Player, GuiWindowType.Emotes).WindowToken.Should().Be(0);
        _db.Get<Player>(PlayerId)!.WindowGeometries[GuiWindowType.Emotes].Should().BeEquivalentTo(new GuiRectangle(1, 2, 30, 40));
    }

    [Test]
    public void Only_a_view_model_without_a_payload_is_reopened()
    {
        var takesNoPayload = typeof(GuiService).GetMethod("TakesNoPayload", BindingFlags.NonPublic | BindingFlags.Static)!;
        bool Reopens(Type viewModel) => (bool)takesNoPayload.Invoke(null, new object[]
        {
            new GuiPlayerWindow((IGuiViewModel)RuntimeHelpers.GetUninitializedObject(viewModel)),
        })!;

        var payloadFree = typeof(GuiService).Assembly.GetTypes().First(type => !type.IsAbstract && type.BaseType is { IsGenericType: true } baseType &&
            baseType.GetGenericTypeDefinition() == typeof(GuiViewModelBase<,>) && baseType.GetGenericArguments()[1] == typeof(GuiPayloadBase));
        Reopens(payloadFree).Should().BeTrue();
        Reopens(typeof(ConversationViewModel)).Should().BeFalse();
        Reopens(typeof(FakeViewModel)).Should().BeFalse();
    }
}
