using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the DM toolbar's possession handlers, the DMPresence window helpers, and the Admin button's permission check
/// against the engine model of DM possession. The client keeps the toolbar open across possession, but its view model
/// must be bound to the creature the DM controls for its buttons and bindings to reach the DM.
/// </summary>
public class DMToolbarPossessionTests
{
    private const uint DM = 1;
    private const uint Creature = 2;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionToolbarHandler()
    {
        _harness = Compile($$"""
            public enum GuiWindowType { DMToolbar }

            // NUI windows belong to a client, which the engine finds from the object that client controls.
            public static class Gui
            {
                public static readonly Dictionary<uint, uint> ToolbarBoundTo = new();

                public static bool IsWindowOpen(uint player, GuiWindowType type) =>
                    ToolbarBoundTo.ContainsKey(ReceivingPlayer(player));

                public static void TogglePlayerWindow(uint player, GuiWindowType type,
                    object payload = null, uint tetherObject = OBJECT_INVALID, uint uiTarget = OBJECT_INVALID)
                {
                    if (!GetIsPC(player) && uiTarget == OBJECT_INVALID)
                        return;
                    if (uiTarget == OBJECT_INVALID)
                        uiTarget = player;

                    var client = ReceivingPlayer(uiTarget);
                    if (client == OBJECT_INVALID)
                        return;

                    if (ToolbarBoundTo.ContainsKey(client))
                        ClosePlayerWindow(player, type, uiTarget);
                    else
                        ToolbarBoundTo[client] = uiTarget;
                }

                public static void ClosePlayerWindow(uint player, GuiWindowType type, uint uiTarget = OBJECT_INVALID)
                {
                    if (uiTarget == OBJECT_INVALID)
                        uiTarget = player;

                    ToolbarBoundTo.Remove(ReceivingPlayer(uiTarget));
                }
            }

            public static class DMPresence
            {
                {{Field("Service/DMPresence.cs", "_windowTargets")}}
                {{Method("Service/DMPresence.cs", "GetDM")}}
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
                {{Method("Service/DMPresence.cs", "ShowWindow")}}
                {{Method("Service/DMPresence.cs", "CloseWindow")}}
                {{Method("Service/DMPresence.cs", "GetWindowTargets")}}

                // The engine finds the DM's client from the avatar only while the DM controls it.
                public static readonly List<uint> AvatarFadesSent = new();
                public static void RefreshAvatarFade(uint dm)
                {
                    dm = GetDM(dm);
                    if (ReceivingPlayer(dm) != OBJECT_INVALID)
                        AvatarFadesSent.Add(dm);
                }
            }

            {{Type("Enumeration/AuthorizationLevel.cs", "AuthorizationLevel")}}

            public static class Authorization
            {
                public static readonly HashSet<string> AdminKeys = new();

                public static AuthorizationLevel GetAuthorizationLevel(uint player) =>
                    AdminKeys.Contains(GetPCPublicCDKey(player)) ? AuthorizationLevel.Admin : AuthorizationLevel.Player;
            }

            public static class DMAdminToolsViewModel
            {
                {{Method("Feature/GuiDefinition/ViewModel/DMAdminToolsViewModel.cs", "CanUse")}}
            }

            public static class DMToolbarWindow
            {
                public static uint Self = OBJECT_INVALID;
                public static readonly List<Action> Delayed = new();
                private static uint OBJECT_SELF => Self;
                private static void DelayCommand(float delay, Action action) => Delayed.Add(action);

                {{Method("Feature/DMToolbarWindow.cs", "MoveToolbarOnPossession")}}
                {{Method("Feature/DMToolbarWindow.cs", "RefreshAvatarFadeOnRelease")}}
            }

            public static class Driver
            {
                public static void ShowToolbar(uint actor) => DMPresence.ShowWindow(actor, GuiWindowType.DMToolbar);

                public static void PossessEventStarts(uint self)
                {
                    DMToolbarWindow.Self = self;
                    DMToolbarWindow.MoveToolbarOnPossession();
                    DMToolbarWindow.RefreshAvatarFadeOnRelease();
                }

                public static void RunDelayed()
                {
                    var actions = DMToolbarWindow.Delayed.ToList();
                    DMToolbarWindow.Delayed.Clear();
                    actions.ForEach(action => action());
                }

                public static void MakeAdmin(uint player) => Authorization.AdminKeys.Add(GetPCPublicCDKey(player));

                public static uint ToolbarBoundTo(uint client) =>
                    Gui.ToolbarBoundTo.TryGetValue(client, out var target) ? target : OBJECT_INVALID;
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        ((IDictionary)_harness.GetField("Gui", "ToolbarBoundTo")).Clear();
        ((IDictionary)_harness.GetField("DMPresence", "_windowTargets")).Clear();
        ((IList)_harness.GetField("DMToolbarWindow", "Delayed")).Clear();
        ((ICollection<string>)_harness.GetField("Authorization", "AdminKeys")).Clear();
        ((IList)_harness.GetField("DMPresence", "AvatarFadesSent")).Clear();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void PossessingMovesTheToolbarToTheCreature(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Invoke("Driver", "ShowToolbar", DM);

        // The possess event starts on the avatar, before control moves to the creature.
        _harness.Invoke("Driver", "PossessEventStarts", DM);
        _harness.Possess(DM, Creature, possessionType);
        _harness.Invoke("Driver", "RunDelayed");

        ToolbarBoundTo(DM).Should().Be(Creature, "toolbar buttons and bindings act through the creature the DM controls");
        AvatarFadesSent().Should().BeEmpty("the client would fade the possessed creature instead");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void ReleasingMovesTheToolbarBackToTheAvatar(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Possess(DM, Creature, possessionType);
        _harness.Invoke("Driver", "ShowToolbar", DM);
        ToolbarBoundTo(DM).Should().Be(Creature);

        // Returning to the avatar starts on the possessed creature.
        _harness.Invoke("Driver", "PossessEventStarts", Creature);
        _harness.Release(DM);
        _harness.Invoke("Driver", "RunDelayed");

        ToolbarBoundTo(DM).Should().Be(DM);
        AvatarFadesSent().Should().Equal([DM], "an Appear or Disappear click while possessing could not fade the avatar");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void AdminButtonWorksForAPossessingAdmin(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Invoke("Driver", "MakeAdmin", DM);
        _harness.Possess(DM, Creature, possessionType);

        CanUseAdminTools(Creature).Should().BeTrue("the toolbar is bound to the possessed creature");
        CanUseAdminTools(DM).Should().BeTrue("the avatar's CD key resolves through the creature it controls");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void AdminButtonStaysClosedToAPossessingDMWhoIsNotAnAdmin(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        _harness.Possess(DM, Creature, possessionType);

        CanUseAdminTools(Creature).Should().BeFalse();
        CanUseAdminTools(DM).Should().BeFalse();
    }

    [TestCase(nameof(DMToolbarWindow.MoveToolbarOnPossession))]
    [TestCase(nameof(DMToolbarWindow.RefreshAvatarFadeOnRelease))]
    public void PossessionHandlersRunOnTheEventsThatStillIdentifyTheDM(string handler)
    {
        var scripts = typeof(DMToolbarWindow).GetMethod(handler)!
            .GetCustomAttributes<NWNEventHandler>()
            .Select(attribute => attribute.Script);

        scripts.Should().BeEquivalentTo(ScriptName.OnDMPossessBefore, ScriptName.OnDMPossessFullPowerBefore);
    }

    private uint ToolbarBoundTo(uint client) => _harness.Invoke<uint>("Driver", "ToolbarBoundTo", client);

    private IEnumerable<uint> AvatarFadesSent() => ((IEnumerable)_harness.GetField("DMPresence", "AvatarFadesSent")).Cast<uint>();

    private bool CanUseAdminTools(uint actor) => _harness.Invoke<bool>("DMAdminToolsViewModel", "CanUse", actor);
}
