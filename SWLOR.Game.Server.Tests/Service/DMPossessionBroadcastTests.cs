using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production loops that send something to every online player, or every player in an area, against the
/// engine model of DM possession. A possessing DM's copy must go to the creature they control, because anything
/// addressed to their avatar is dropped.
/// </summary>
public class DMPossessionBroadcastTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint OtherDM = 4;
    private const uint Ally = 5;
    private const uint Encounter = 50;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionBroadcasts()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetControlledCreature")}}
                {{InstanceMethod("Domain/Administration/DMPresenceService.cs", "GetDM")}}
            }

            public static class Party
            {
                // Party membership is keyed by each player's own character, so a possessing DM's stays on their avatar.
                public static readonly List<(uint First, uint Second)> Members = new();
                public static bool IsInParty(uint first, uint second) =>
                    Members.Contains((first, second)) || Members.Contains((second, first));
            }

            {{Type("Service/TelegraphService/TelegraphColorType.cs", "TelegraphColorType")}}

            public static class ObjectSnapshot
            {
                {{Method("Core/ObjectSnapshot.cs", "Players")}}
                {{Method("Core/ObjectSnapshot.cs", "Capture")}}
            }

            public static class PlayerName
            {
                public static string GetChatDisplayName(uint observer, uint target) => $"[{target} as seen by {observer}]";
            }

            public static class ColorToken
            {
                public static string Custom(string text, byte red, byte green, byte blue) => text;
                public static string White(string text) => text;
            }

            public static class HoloNet
            {
                public static void Broadcast(uint player, string message)
                {
                    {{PlayerLoop("Feature/GuiDefinition/ViewModel/HoloNetViewModel.cs", "broadcasts a new HoloNet message")}}
                }
            }

            public enum ChatChannel { DMShout }
            public static class ChatPlugin
            {
                // NWNX chat finds the receiving client the same way SendMessageToPC does.
                public static void SendMessage(ChatChannel channel, string message, uint speaker, uint target) =>
                    SendMessageToPC(target, message);
            }

            public static class DMShout
            {
                public static void Send(uint user, string message)
                {
                    {{PlayerLoop("Feature/ChatCommandDefinition/DMChatCommand.cs", "ChatChannel.DMShout")}}
                }
            }

            public static class QuestEncounter
            {
                private const float ParticipantCreditRange = 50.0f;
                {{Method("Service/QuestService/QuestEncounter.cs", "SendMessageToNearbyPlayers")}}
            }

            public static class EventsPlugin
            {
                public static uint Target = OBJECT_INVALID;
                public static string GetEventData(string name) => Target.ToString("x");
            }

            public static class Telegraph
            {
                public static uint Self;
                public static readonly List<uint> Updated = new();
                private static readonly Dictionary<uint, object> _sentShaderStates = new();
                private static uint OBJECT_SELF => Self;
                private static uint StringToObject(string id) => Convert.ToUInt32(id, 16);
                private static void DelayCommand(float seconds, Action action) => action();
                private static void UpdateShaderForPlayer(uint player) => Updated.Add(player);
                public static void MarkSent(uint viewer) => _sentShaderStates[viewer] = new object();
                public static bool WasSent(uint viewer) => _sentShaderStates.ContainsKey(viewer);
                public static void Reset()
                {
                    Updated.Clear();
                    _sentShaderStates.Clear();
                    EventsPlugin.Target = OBJECT_INVALID;
                    Party.Members.Clear();
                }
                public static void Refresh(uint[] players) => UpdateShadersForPlayers(players);
                // UpdateShaderSlotsForPlayer resolves the party member once per viewer and passes it for every slot.
                public static TelegraphColorType ColorFor(uint viewer, uint telegrapher, bool isHostile) =>
                    DetermineTelegraphColorType(viewer, DMPresence.GetDM(viewer), telegrapher, isHostile);
                {{Method("Service/Telegraph.cs", "DetermineTelegraphColorType")}}
                {{Method("Service/Telegraph.cs", "SelectTelegraphColorType")}}
                {{Method("Service/Telegraph.cs", "UpdateShadersForPlayers")}}
                {{Method("Service/Telegraph.cs", "UpdateShadersForAllPlayers")}}
                {{Method("Service/Telegraph.cs", "UpdateShadersForArea")}}
                {{Method("Service/Telegraph.cs", "ResendShaderStateOnDMPossess")}}
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        _harness.Invoke("Telegraph", "Reset");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void HoloNetBroadcast_ReachesAPossessingDMThroughTheirCreature(int possessionType)
    {
        OnlineCast(possessionType);

        _harness.Invoke("HoloNet", "Broadcast", Player, "Spice for sale");

        _harness.Messages.Should().Equal(
            (Player, "[3 as seen by 3] broadcasts a new HoloNet message: Spice for sale"),
            (OtherDM, "[3 as seen by 4] broadcasts a new HoloNet message: Spice for sale"),
            (DM, "[3 as seen by 2] broadcasts a new HoloNet message: Spice for sale"));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void DMShout_ReachesAPossessingDM(int possessionType)
    {
        OnlineCast(possessionType);

        _harness.Invoke("DMShout", "Send", OtherDM, "Event at the cantina");

        _harness.Messages.Select(message => message.Character).Should().Equal(Player, OtherDM, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void EncounterNotice_ReachesAPossessingDMWhoseCreatureIsNearby(int possessionType)
    {
        OnlineCast(possessionType);
        _harness.Place(Encounter, Cantina);

        _harness.Invoke("QuestEncounter", "SendMessageToNearbyPlayers", Encounter, "The encounter fades.");

        _harness.Messages.Should().Equal((Player, "The encounter fades."), (DM, "The encounter fades."));
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphAreaUpdate_ReachesAPossessingDMWhoseCreatureIsInTheArea(int possessionType)
    {
        OnlineCast(possessionType);

        _harness.Invoke("Telegraph", "UpdateShadersForArea", Cantina);

        var updated = (List<uint>)_harness.GetField("Telegraph", "Updated");
        updated.Should().Equal(Player, Creature);
        updated.Select(_harness.ReceivingPlayer).Should().Equal(Player, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphFullUpdate_ReachesAPossessingDM(int possessionType)
    {
        OnlineCast(possessionType);

        _harness.Invoke("Telegraph", "UpdateShadersForAllPlayers");

        ((List<uint>)_harness.GetField("Telegraph", "Updated")).Select(_harness.ReceivingPlayer)
            .Should().Equal(Player, OtherDM, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphState_IsResentToThePossessedCreature(int possessionType)
    {
        OnlineCast(possessionType);
        _harness.Invoke("Telegraph", "MarkSent", Creature);
        _harness.SetField("Telegraph", "Self", DM);
        _harness.SetField("EventsPlugin", "Target", Creature);

        _harness.Invoke("Telegraph", "ResendShaderStateOnDMPossess");

        _harness.Invoke<bool>("Telegraph", "WasSent", Creature).Should().BeFalse();
        ((List<uint>)_harness.GetField("Telegraph", "Updated")).Should().Equal(Creature);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphState_IsResentToTheAvatarWhenPossessionEnds(int possessionType)
    {
        OnlineCast(possessionType);
        _harness.Invoke("Telegraph", "MarkSent", DM);
        _harness.SetField("Telegraph", "Self", Creature);

        _harness.Invoke("Telegraph", "ResendShaderStateOnDMPossess");

        _harness.Invoke<bool>("Telegraph", "WasSent", DM).Should().BeFalse();
        ((List<uint>)_harness.GetField("Telegraph", "Updated")).Should().Equal(DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphColors_UseThePartyOfAPossessingDMsAvatar(int possessionType)
    {
        OnlineCast(possessionType);
        JoinParty(DM, Player);

        TelegraphColor(Creature, Player).Should().Be("PartyMember");
        TelegraphColor(Creature, Creature).Should().Be("Self");
        TelegraphColor(Creature, Encounter).Should().Be("Hostile");
    }

    [Test]
    public void TelegraphColors_UseAPlayersOwnParty()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(Ally, area: Cantina);
        JoinParty(Player, Ally);

        TelegraphColor(Player, Ally).Should().Be("PartyMember");
        TelegraphColor(Player, Player).Should().Be("Self");
        TelegraphColor(Player, Encounter).Should().Be("Hostile");
    }

    [Test]
    public void TelegraphSlots_PassTheAvatarBehindAPossessedCreatureForPartyColors()
    {
        var slots = Method("Service/Telegraph.cs", "UpdateShaderSlotsForPlayer");

        slots.Should().Contain("var partyMember = DMPresence.GetDM(player);")
            .And.Contain("DetermineTelegraphColorType(player, partyMember, data.Creator, data.IsHostile)");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TelegraphPartyRefresh_ReachesAPossessingDM(int possessionType)
    {
        OnlineCast(possessionType);

        _harness.Invoke("Telegraph", "Refresh", new[] { Player, DM, Encounter });

        var updated = (List<uint>)_harness.GetField("Telegraph", "Updated");
        updated.Should().Equal(Player, Creature);
        updated.Select(_harness.ReceivingPlayer).Should().Equal(Player, DM);
    }

    // A player and a DM in the cantina and spaceport, and a DM whose avatar waits in the spaceport while they
    // possess a creature in the cantina.
    private void OnlineCast(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        PossessingDM(possessionType);
    }

    private void JoinParty(uint first, uint second) =>
        ((List<(uint, uint)>)_harness.GetField("Party", "Members")).Add((first, second));

    private string TelegraphColor(uint viewer, uint telegrapher) =>
        _harness.Invoke("Telegraph", "ColorFor", viewer, telegrapher, true).ToString()!;

    private void PossessingDM(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);
    }
}
