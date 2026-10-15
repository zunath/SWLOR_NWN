using System.Collections;
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
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
            }

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
                }
                {{Method("Service/Telegraph.cs", "UpdateShadersForAllPlayers")}}
                {{Method("Service/Telegraph.cs", "UpdateShadersForArea")}}
                {{Method("Service/Telegraph.cs", "ResendShaderStateOnDMPossess")}}
            }

            public static class Weather
            {
                public sealed class AreaWeather { public DateTime NextLightningUtc; }
                public sealed class WeatherExposure { }
                public static bool Changed;
                public static readonly List<uint> Effects = new();
                public static readonly List<uint> Exposed = new();
                public static readonly List<uint> Mist = new();
                private static readonly Dictionary<uint, AreaWeather> _areas = new();
                private static readonly Dictionary<uint, WeatherExposure> _exposures = new();
                public static void AddWeatherArea(uint area) => _areas[area] = new AreaWeather();
                public static void Reset()
                {
                    Changed = false;
                    Effects.Clear();
                    Exposed.Clear();
                    Mist.Clear();
                    _areas.Clear();
                    _exposures.Clear();
                }
                private static bool AdjustWeather() => Changed;
                private static bool IsWeatherArea(uint area) => true;
                private static void DoWeatherEffects(uint creature) => Effects.Add(creature);
                private static void ApplyWeatherDamage(uint creature, DateTime now)
                {
                    Exposed.Add(creature);
                    _exposures[creature] = new WeatherExposure();
                }
                private static void EnsureMist(uint area, AreaWeather state) => Mist.Add(area);
                private static void Thunderstorm(uint area) { }
                {{Method("Service/Weather.cs", "OnModuleHeartbeat")}}
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        _harness.Invoke("Telegraph", "Reset");
        _harness.Invoke("Weather", "Reset");
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
    public void WeatherChange_ReachesAPossessingDMWhereTheirCreatureIs(int possessionType)
    {
        OnlineCast(possessionType);
        _harness.Invoke("Weather", "AddWeatherArea", Cantina);
        _harness.Invoke("Weather", "AddWeatherArea", Spaceport);
        _harness.SetField("Weather", "Changed", true);

        _harness.Invoke("Weather", "OnModuleHeartbeat");

        ((List<uint>)_harness.GetField("Weather", "Effects")).Should().Equal(Player, OtherDM, Creature);
        ((List<uint>)_harness.GetField("Weather", "Mist")).Should().BeEquivalentTo(new[] { Cantina, Spaceport });
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void WeatherExposure_FollowsThePossessedCreature(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        PossessingDM(possessionType);
        _harness.Invoke("Weather", "AddWeatherArea", Cantina);
        _harness.Invoke("Weather", "AddWeatherArea", Spaceport);

        _harness.Invoke("Weather", "OnModuleHeartbeat");

        ((List<uint>)_harness.GetField("Weather", "Exposed")).Should().Equal(Player, Creature);
        ((List<uint>)_harness.GetField("Weather", "Mist")).Should().Equal(Cantina);
        ((IDictionary)_harness.GetField("Weather", "_exposures")).Keys.Cast<uint>().Should().BeEquivalentTo(new[] { Player, Creature });
    }

    // A player and a DM in the cantina and spaceport, and a DM whose avatar waits in the spaceport while they
    // possess a creature in the cantina.
    private void OnlineCast(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Spaceport);
        PossessingDM(possessionType);
    }

    private void PossessingDM(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);
    }
}
