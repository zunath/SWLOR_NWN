using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

public class EmotesWindowLifecycleTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionCommandAndWindowLifecycle()
    {
        _harness = Compile($$"""
            public enum GuiWindowType { Emotes }
            public enum AuthorizationLevel { All }
            public class GuiPayloadBase { }
            public class GuiRectangle { public float X, Y, Width = 150, Height = 300; }
            public class Player
            {
                public string Id;
                public Player(string id) { Id = id; }
                public Dictionary<GuiWindowType, GuiRectangle> WindowGeometries = new();
            }
            public static class DB
            {
                public static readonly Dictionary<string, Player> Records = new();
                public static T Get<T>(string id) where T : class => Records.GetValueOrDefault(id) as T;
                public static void Set(Player player) => Records[player.Id] = player;
            }
            public class ViewModel
            {
                public GuiRectangle Geometry = new();
                public uint BoundTo;
                public int Binds, Closes;
                public void Bind(uint player, int token, GuiRectangle initial, GuiWindowType type,
                    GuiPayloadBase payload, uint tether) { BoundTo = player; Binds++; }
                public Action OnWindowClosed() => () => Closes++;
            }
            public class GuiPlayerWindow
            {
                public ViewModel ViewModel = new();
                public int WindowToken;
            }
            public class Template
            {
                public GuiRectangle InitialGeometry = new();
                public object Window = new();
                public string WindowId = "GUI_WINDOW_Emotes";
                public bool KeepsOpenOnAreaChange;
                public Func<GuiPlayerWindow> CreatePlayerWindowAction = () => new();
            }
            public static class NativeGui
            {
                public static uint Entering, Exiting;
                public static readonly Dictionary<uint, int> Windows = new();
                private static int _token;
                public static uint GetEnteringObject() => Entering;
                public static uint GetExitingObject() => Exiting;
                public static object JsonObjectGet(object json, string key) => json;
                public static int JsonGetInt(object json) => 1;
                public static string JsonGetString(object json) => "";
                public static int NuiFindWindow(uint target, string id) => Windows.GetValueOrDefault(target);
                public static int NuiCreate(uint target, object json, string id) => Windows[target] = ++_token;
                public static void NuiDestroy(uint target, int token) => Windows.Remove(target);
            }
            public static class Gui
            {
                private static readonly Dictionary<GuiWindowType, Template> _windowTemplates = new()
                    { [GuiWindowType.Emotes] = new() };
                {{Field("Service/Gui.cs", "_playerWindows")}}
                private static uint GetEnteringObject() => NativeGui.GetEnteringObject();
                private static uint GetExitingObject() => NativeGui.GetExitingObject();
                private static object JsonObjectGet(object json, string key) => NativeGui.JsonObjectGet(json, key);
                private static int JsonGetInt(object json) => NativeGui.JsonGetInt(json);
                private static string JsonGetString(object json) => NativeGui.JsonGetString(json);
                private static int NuiFindWindow(uint target, string id) => NativeGui.NuiFindWindow(target, id);
                private static int NuiCreate(uint target, object json, string id) => NativeGui.NuiCreate(target, json, id);
                private static void NuiDestroy(uint target, int token) => NativeGui.NuiDestroy(target, token);
                {{Method("Service/Gui.cs", "CreatePlayerWindows")}}
                {{Method("Service/Gui.cs", "SavePlayerWindowGeometry")}}
                {{Method("Service/Gui.cs", "SaveWindowGeometry")}}
                {{Method("Service/Gui.cs", "BuildWindowId")}}
                {{Method("Service/Gui.cs", "IsWindowOpen")}}
                {{Method("Service/Gui.cs", "TogglePlayerWindow")}}
                {{Method("Service/Gui.cs", "ClosePlayerWindow")}}
                {{Method("Service/Gui.cs", "GetPlayerWindow")}}
                {{Method("Service/Gui.cs", "CloseAllWindows")}}
                public static int Owners => _playerWindows.Count;
                public static void Reset() => _playerWindows.Clear();
            }
            public static class DMPresence
            {
                {{InstanceField("Domain/Administration/DMPresenceService.cs", "_windowTargets")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetDM")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetControlledCreature")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "IsControlledByDM")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "ToggleWindow")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetWindowTargets")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetShownOn")}}
                public static void Reset() => _windowTargets.Clear();
            }
            public class CommandBuilder
            {
                public static readonly Dictionary<string, Action<uint, uint, object, string[]>> Commands = new();
                private string[] _aliases;
                public CommandBuilder Create(params string[] aliases) { _aliases = aliases; return this; }
                public CommandBuilder Description(string text) => this;
                public CommandBuilder Permissions(AuthorizationLevel level) => this;
                public void Action(Action<uint, uint, object, string[]> action)
                { foreach (var alias in _aliases) Commands[alias] = action; }
            }
            public class SystemChatCommand
            {
                private readonly CommandBuilder _builder = new();
                public SystemChatCommand() { EmotesWindowCommand(); }
                {{Method("Feature/ChatCommandDefinition/SystemChatCommand.cs", "EmotesWindowCommand")}}
            }
            public static class Driver
            {
                public static void Reset()
                {
                    Gui.Reset(); DMPresence.Reset(); NativeGui.Windows.Clear(); DB.Records.Clear();
                    new SystemChatCommand();
                }
                public static void Login(uint player)
                {
                    NativeGui.Entering = player;
                    DB.Records.TryAdd(GetObjectUUID(player), new Player(GetObjectUUID(player)));
                    Gui.CreatePlayerWindows();
                }
                public static void Logout(uint player)
                {
                    NativeGui.Exiting = player;
                    Gui.SavePlayerWindowGeometry();
                    NativeGui.Windows.Clear();
                    World.Players.RemoveAll(p => p.Character == player);
                }
                public static void Command(uint actor, string alias) =>
                    CommandBuilder.Commands[alias](actor, OBJECT_INVALID, null, Array.Empty<string>());
                public static void Transition(uint actor)
                { NativeGui.Entering = actor; Gui.CloseAllWindows(); }
                public static object Window(uint owner) => Gui.GetPlayerWindow(owner, GuiWindowType.Emotes);
                public static uint BoundTo(uint owner) => Gui.GetPlayerWindow(owner, GuiWindowType.Emotes).ViewModel.BoundTo;
                public static int Owners() => Gui.Owners;
                public static int OpenWindows() => NativeGui.Windows.Count;
                public static int Binds(uint owner) => Gui.GetPlayerWindow(owner, GuiWindowType.Emotes).ViewModel.Binds;
                public static void DirectCreatureToggle(uint actor) => Gui.TogglePlayerWindow(actor, GuiWindowType.Emotes);
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        _harness.Invoke("Driver", "Reset");
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
        _harness.Invoke("Driver", "Transition", Player);
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
        _harness.Invoke("Driver", "Logout", Player);
        OpenWindows.Should().Be(0);
        Login(Player, isDM);
        Window(Player).Should().BeSameAs(window);
        Command(Player);
        OpenWindows.Should().Be(1);
        _harness.Invoke<int>("Driver", "Binds", Player).Should().Be(2);
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
        _harness.Possess(DM, Creature, possessionType);
        Action direct = () => _harness.Invoke("Driver", "DirectCreatureToggle", Creature);
        direct.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<KeyNotFoundException>();

        Command(Creature);
        OpenWindows.Should().Be(1);
        BoundTo(DM).Should().Be(Creature);
        Command(Creature);
        OpenWindows.Should().Be(0);
        Command(Creature);
        _harness.Invoke("Driver", "Transition", Creature);
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
        _harness.Possess(DM, Creature, possessionType);
        Command(Creature);
        OpenWindows.Should().Be(1);
        BoundTo(DM).Should().Be(Creature);
        _harness.Release(DM);
        _harness.Possess(DM, 4, possessionType);
        Command(4);
        OpenWindows.Should().Be(1);
        BoundTo(DM).Should().Be(4);
        _harness.Release(DM);
        Command(DM);
        OpenWindows.Should().Be(1);
        BoundTo(DM).Should().Be(DM);
        Command(DM);
        OpenWindows.Should().Be(0);
        Command(DM);
        OpenWindows.Should().Be(1);
        Window(DM).Should().BeSameAs(window);
        Owners.Should().Be(1);
    }

    private void Login(uint player, bool isDM = false)
    {
        _harness.AddPlayer(player, isDM);
        _harness.Invoke("Driver", "Login", player);
    }
    private void Command(uint actor, string alias = "emotesgui") => _harness.Invoke("Driver", "Command", actor, alias);
    private object Window(uint owner) => _harness.Invoke("Driver", "Window", owner);
    private uint BoundTo(uint owner) => _harness.Invoke<uint>("Driver", "BoundTo", owner);
    private int OpenWindows => _harness.Invoke<int>("Driver", "OpenWindows");
    private int Owners => _harness.Invoke<int>("Driver", "Owners");
}
