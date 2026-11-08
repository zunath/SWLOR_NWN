using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Tests.Support.DMPossessionEngine;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Emotes window from login to the chat command: the real GUI and DM presence instances over the engine model of DM
/// possession, with the window itself a fake view model. (This used to recompile the production methods with Roslyn.)
/// </summary>
[TestFixture]
public class EmotesWindowLifecycleTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;

    private DMPossessionEngine _world = null!;
    private InMemoryDBService _db = null!;
    private DMPresenceService _presence = null!;
    private TestGuiService _gui = null!;
    private FacadeContainer _facades = null!;
    private IDisposable _scope = null!;
    private Dictionary<string, ChatCommandDetail> _commands = null!;
    private readonly Dictionary<(uint Target, string Id), int> _windows = new();
    private readonly HashSet<int> _destroyed = new();
    private readonly HashSet<uint> _known = new();
    private int _tokens;

    [SetUp]
    public void SetUp()
    {
        _world = new DMPossessionEngine();
        _db = new InMemoryDBService();
        _presence = new DMPresenceService(FacadeForwarder.LazyFor<IDMObjectSearchService>(), FacadeForwarder.LazyFor<IGuiService>());
        _gui = new TestGuiService(_db, _presence, typeof(FakeEmotesDefinition));
        _facades = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDMPresenceService>(_presence));
            services.Replace(ServiceDescriptor.Singleton<IGuiService>(_gui));
        });
        _scope = _facades.Install();
        _windows.Clear();
        _destroyed.Clear();
        _known.Clear();
        _tokens = 0;

        _world.Engine.SetReturnHandler("NuiCreate", args => _windows[((uint)args[0]!, (string)args[2]!)] = ++_tokens);
        // NuiDestroy returns nothing, so the fake only records it: a window is open until a destroy names its token.
        _world.Engine.SetReturnHandler("NuiFindWindow", args =>
        {
            SyncDestroyed();
            return _windows.TryGetValue(((uint)args[0]!, (string)args[1]!), out var token) && !_destroyed.Contains(token) ? token : 0;
        });
        _world.Engine.SetReturn("JsonGetInt", 1);

        _gui.CacheData();
        _commands = new SystemChatCommand().BuildChatCommands();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _facades.Dispose();
        _world.Dispose();
    }

    [TestCase("emotegui")]
    [TestCase("emotesgui")]
    public void PlayerCanOpenCloseAndReopenAfterLoginAndTransition(string alias)
    {
        Login(Player);
        var window = Window(Player);
        Command(Player, alias);
        OpenWindows.Should().Be(1);
        Command(Player, alias);
        OpenWindows.Should().Be(0);
        Command(Player, alias);
        Transition(Player);
        OpenWindows.Should().Be(0);
        Command(Player, alias);
        OpenWindows.Should().Be(1);
        Window(Player).Should().BeSameAs(window);
        Owners.Should().Be(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReconnectReusesRegisteredWindowsAndRebindsTheNewClient(bool isDM)
    {
        Login(Player, isDM);
        var window = Window(Player);
        Command(Player);
        Logout(Player);
        OpenWindows.Should().Be(0);
        Login(Player, isDM);
        Window(Player).Should().BeSameAs(window);
        Command(Player);
        OpenWindows.Should().Be(1);
        Model(Player).BindCount.Should().Be(2);
        Command(Player);
        OpenWindows.Should().Be(0);
        Command(Player);
        OpenWindows.Should().Be(1);
        Owners.Should().Be(1);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void PossessedCreatureIsNotRegisteredButCommandUsesTheDMsWindow(int possessionType)
    {
        Login(DM, isDM: true);
        _world.Possess(DM, Creature, possessionType);
        var direct = () => _gui.TogglePlayerWindow(Creature, GuiWindowType.Emotes);
        direct.Should().Throw<KeyNotFoundException>();

        Command(Creature);
        OpenWindows.Should().Be(1);
        Model(DM).BoundTo.Should().Be(Creature);
        Command(Creature);
        OpenWindows.Should().Be(0);
        Command(Creature);
        Transition(Creature);
        OpenWindows.Should().Be(0);
        Command(Creature);
        OpenWindows.Should().Be(1);
        Owners.Should().Be(1, "a possessed NPC must never receive its own player window cache");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void ChangingControlledCreatureMovesOneWindowWithoutReplacingItsViewModel(int possessionType)
    {
        Login(DM, isDM: true);
        var window = Window(DM);
        Command(DM);
        _world.Possess(DM, Creature, possessionType);
        Command(Creature);
        OpenWindows.Should().Be(1);
        Model(DM).BoundTo.Should().Be(Creature);
        _world.Release(DM);
        _world.Possess(DM, 4, possessionType);
        Command(4);
        OpenWindows.Should().Be(1);
        Model(DM).BoundTo.Should().Be(4);
        _world.Release(DM);
        Command(DM);
        OpenWindows.Should().Be(1);
        Model(DM).BoundTo.Should().Be(DM);
        Command(DM);
        OpenWindows.Should().Be(0);
        Command(DM);
        OpenWindows.Should().Be(1);
        Window(DM).Should().BeSameAs(window);
        Owners.Should().Be(1);
    }

    private void Login(uint player, bool isDM = false)
    {
        if (_known.Add(player))
        {
            _world.AddPlayer(player, isDM);
            _db.With(new Player(player.ToString()));
        }

        _world.Engine.SetReturn("GetEnteringObject", player);
        _gui.CreatePlayerWindows();
    }

    private void Logout(uint player)
    {
        _world.Engine.SetReturn("GetExitingObject", player);
        _gui.SavePlayerWindowGeometry();
        foreach (var token in _windows.Values)
            _destroyed.Add(token);
    }

    private void Command(uint actor, string alias = "emotesgui") =>
        _commands[alias].DoAction(actor, Invalid, null!, Array.Empty<string>());

    private void Transition(uint actor)
    {
        _world.Engine.SetReturn("GetEnteringObject", actor);
        _gui.CloseAllWindows();
    }

    private GuiPlayerWindow Window(uint owner) => _gui.GetPlayerWindow(owner, GuiWindowType.Emotes);
    private FakeViewModel Model(uint owner) => (FakeViewModel)Window(owner).ViewModel;
    private int OpenWindows
    {
        get
        {
            SyncDestroyed();
            return _windows.Values.Count(token => !_destroyed.Contains(token));
        }
    }

    private void SyncDestroyed()
    {
        foreach (var call in _world.Engine.CallsTo("NuiDestroy"))
            _destroyed.Add((int)call.Arguments[1]!);
    }
    // How many of the characters in play have a window cache; looking up a character without one throws.
    private int Owners => new uint[] { DM, Creature, Player, 4 }.Count(owner =>
    {
        try
        {
            Window(owner);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    });
}
