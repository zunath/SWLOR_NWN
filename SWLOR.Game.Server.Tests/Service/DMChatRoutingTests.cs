using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production DM chat routing against the engine model of DM possession: the Comms filter, the DM emote
/// style shared by an avatar and the creatures it possesses, the DM Talk and Whisper channels, who overhears local
/// speech, and the area shout.
/// </summary>
public class DMChatRoutingTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint OtherDM = 4;
    private const uint FarPlayer = 5;
    private const uint PlayerFamiliar = 6;
    private const uint Npc = 50;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private const int AllParties = 0;
    private const int OwnParty = 1;
    private const int Regular = 1;
    private const int Novel = 2;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionChatRouting()
    {
        _harness = Compile($$"""
            {{Type("Enumeration/EmoteStyle.cs", "EmoteStyle")}}
            {{Type("Enumeration/DMCommsFilter.cs", "DMCommsFilter")}}
            public enum ChatChannel { PlayerTalk = 1, PlayerShout = 2, PlayerWhisper = 3, PlayerParty = 6, DMTalk = 17, DMShout = 18, DMWhisper = 19, DMParty = 22 }

            public sealed class Player { public string Id; public EmoteStyle EmoteStyle = EmoteStyle.Regular; }
            public sealed class DMPreferences
            {
                public string Id;
                public EmoteStyle EmoteStyle = EmoteStyle.Regular;
                public DMCommsFilter CommsFilter = DMCommsFilter.AllParties;
            }

            public static class DB
            {
                public static readonly Dictionary<string, object> Records = new();
                public static T Get<T>(string id) where T : class => Records.TryGetValue(id, out var record) ? record as T : null;
                public static void Set(Player player) => Records[player.Id] = player;
                public static void Set(DMPreferences preferences) => Records[preferences.Id] = preferences;
                public static void AddPlayer(uint player) => Records[player.ToString()] = new Player { Id = player.ToString() };
            }

            public static class DMPresence
            {
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
                {{Method("Service/DMPresence.cs", "IsControlledByDM")}}
                {{Method("Service/DMPresence.cs", "GetDM")}}
            }

            public static class DMPreference
            {
                private const string IdPrefix = "DM:";
                // The engine resolves a CD key from the object a player controls, like every other client lookup.
                private static string GetPCPublicCDKey(uint obj)
                {
                    var player = ReceivingPlayer(obj);
                    return player == OBJECT_INVALID ? string.Empty : "KEY" + player;
                }
                public static bool SetCommsFilter(uint actor, int filter) =>
                    Update(actor, preferences => preferences.CommsFilter = (DMCommsFilter)filter);
                {{Method("Service/DMPreference.cs", "Get")}}
                {{Method("Service/DMPreference.cs", "Update")}}
                {{Method("Service/DMPreference.cs", "GetId")}}
            }

            public static class Party
            {
                // Party membership is keyed by each player's own character, so a possessing DM's stays on their avatar.
                public static readonly List<(uint First, uint Second)> Members = new();
                public static bool IsInParty(uint first, uint second) =>
                    first == second || Members.Contains((first, second)) || Members.Contains((second, first));
            }

            public static class EventsPlugin
            {
                public static readonly Dictionary<string, string> Data = new();
                public static string GetEventData(string tag) => Data.TryGetValue(tag, out var value) ? value : string.Empty;
            }

            public static class Activity
            {
                public static readonly List<uint> Cleared = new();
                public static void ClearBusy(uint creature) => Cleared.Add(creature);
            }

            public static class Communication
            {
                // The DM possess events run on whatever the DM controls when they click Possess or Release.
                public static uint Self;
                private static uint OBJECT_SELF => Self;
                public static readonly Dictionary<(uint, string), uint> LocalObjects = new();
                private static uint StringToObject(string id) => uint.TryParse(id, out var obj) ? obj : OBJECT_INVALID;
                private static void SetLocalObject(uint obj, string name, uint value) => LocalObjects[(obj, name)] = value;
                private static uint GetLocalObject(uint obj, string name) =>
                    LocalObjects.TryGetValue((obj, name), out var value) ? value : OBJECT_INVALID;
                private static void DeleteLocalObject(uint obj, string name) => LocalObjects.Remove((obj, name));
                public static List<uint> NearbyListeners(uint sender, float range)
                {
                    var recipients = new List<uint> { sender };
                    AddNearbyListeners(sender, OnlinePlayers(), range, new HashSet<uint>(), recipients);
                    return recipients;
                }
                public static List<uint> OnlinePlayers()
                {
                    var players = new List<uint>();
                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                        players.Add(player);
                    return players;
                }
                public static List<uint> Readers(uint sender) => GetDMCommsAudience(sender, OnlinePlayers()).Readers;
                public static List<uint> Muted(uint sender) => GetDMCommsAudience(sender, OnlinePlayers()).Muted.ToList();
                public static int GetStyle(uint actor) => (int)GetEmoteStyle(actor);
                public static bool SetStyle(uint actor, int style) => SetEmoteStyle(actor, (EmoteStyle)style);
                public static bool IsLocalSpeech(int channel) =>
                    IsTalkChannel((ChatChannel)channel) || IsWhisperChannel((ChatChannel)channel);
                {{Method("Service/Communication.cs", "OnDMPossess")}}
                {{Method("Service/Communication.cs", "AddNearbyListeners")}}
                {{Method("Service/Communication.cs", "GetDMCommsAudience")}}
                {{Method("Service/Communication.cs", "IsTalkChannel")}}
                {{Method("Service/Communication.cs", "IsWhisperChannel")}}
                {{Method("Service/Communication.cs", "GetEmoteStyle")}}
                {{Method("Service/Communication.cs", "SetEmoteStyle")}}
            }

            public static class AreaShout
            {
                {{Method("Feature/ChatCommandDefinition/DMChatCommand.cs", "GetAreaShoutRecipients")}}
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        ((IDictionary)_harness.GetField("DB", "Records")).Clear();
        ((IList)_harness.GetField("Party", "Members")).Clear();
        ((IDictionary)_harness.GetField("EventsPlugin", "Data")).Clear();
        ((IList)_harness.GetField("Activity", "Cleared")).Clear();
        ((IDictionary)_harness.GetField("Communication", "LocalObjects")).Clear();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void Comms_ReachEveryDMIncludingOneWhoIsPossessing(int possessionType)
    {
        _harness.AddPlayer(Player);
        _harness.AddPlayer(OtherDM, isDM: true);
        PossessingDM(possessionType);

        var readers = Readers(Player);

        readers.Should().Equal(OtherDM, Creature);
        readers.Select(_harness.ReceivingPlayer).Should().Equal(OtherDM, DM);
        Muted(Player).Should().BeEmpty();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void OwnPartyFilter_HidesOtherPartiesComms(int possessionType)
    {
        _harness.AddPlayer(Player);
        _harness.AddPlayer(OtherDM, isDM: true);
        PossessingDM(possessionType);
        SetCommsFilter(Creature, OwnParty);

        Readers(Player).Should().Equal(OtherDM);
        Muted(Player).Should().Equal(Creature);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void OwnPartyFilter_KeepsTheDMsOwnPartyWhilePossessing(int possessionType)
    {
        _harness.AddPlayer(Player);
        PossessingDM(possessionType);
        JoinParty(Player, DM);
        SetCommsFilter(Creature, OwnParty);

        Readers(Player).Should().Equal(Creature);
        Muted(Player).Should().BeEmpty();
    }

    [Test]
    public void OwnPartyFilter_ChecksThePartyOfThePossessingDMWhoSentIt()
    {
        _harness.AddPlayer(OtherDM, isDM: true);
        PossessingDM(DMPossess);
        JoinParty(DM, OtherDM);
        SetCommsFilter(OtherDM, OwnParty);

        Readers(Creature).Should().Contain(OtherDM);
    }

    [Test]
    public void CommsFilterSetOnTheAvatarFollowsTheDMIntoPossession()
    {
        _harness.AddPlayer(Player);
        _harness.AddPlayer(DM, isDM: true);
        SetCommsFilter(DM, OwnParty);
        _harness.Possess(DM, Creature, DMPossess);

        Muted(Player).Should().Equal(Creature);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void EmoteStyle_IsSharedByTheDMAvatarAndPossessedCreatures(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true);
        EmoteStyle(DM).Should().Be(Regular);

        SetEmoteStyle(DM, Novel).Should().BeTrue();
        _harness.Possess(DM, Creature, possessionType);

        EmoteStyle(Creature).Should().Be(Novel, "the style set on the avatar applies while possessing");
        SetEmoteStyle(Creature, Regular).Should().BeTrue("the toggle works while possessing a creature");
        EmoteStyle(Creature).Should().Be(Regular);
    }

    [Test]
    public void EmoteStyle_PlayersKeepTheirCharacterSetting()
    {
        _harness.AddPlayer(Player);
        SetEmoteStyle(Player, Novel).Should().BeFalse("a player without a record has nowhere to save it");
        EmoteStyle(Player).Should().Be(Regular);

        _harness.Invoke("DB", "AddPlayer", Player);
        SetEmoteStyle(Player, Novel).Should().BeTrue();
        EmoteStyle(Player).Should().Be(Novel);
        ((IDictionary)_harness.GetField("DB", "Records")).Keys.Cast<string>().Should().Equal(Player.ToString());
    }

    [Test]
    public void EmoteStyle_UncontrolledCreaturesUseRegular()
    {
        EmoteStyle(Npc).Should().Be(Regular);
        SetEmoteStyle(Npc, Novel).Should().BeFalse();
    }

    [TestCase(1, true)]
    [TestCase(3, true)]
    [TestCase(17, true)]
    [TestCase(19, true)]
    [TestCase(2, false)]
    [TestCase(6, false)]
    [TestCase(18, false)]
    [TestCase(22, false)]
    public void DMTalkAndWhisperAreHandledAsLocalSpeech(int channel, bool expected)
    {
        // The engine sends a DM avatar's Talk and Whisper on the DM channels and only uses the player channels while
        // the DM possesses a creature.
        _harness.Invoke<bool>("Communication", "IsLocalSpeech", channel).Should().Be(expected);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void AreaShout_ReachesEveryoneInTheAreaIncludingAPossessingDM(int possessionType)
    {
        _harness.AddPlayer(OtherDM, isDM: true, area: Cantina);
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(FarPlayer, area: Spaceport);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);

        var recipients = _harness.Invoke<List<uint>>("AreaShout", "GetAreaShoutRecipients", OtherDM);

        recipients.Should().Equal(OtherDM, Player, Creature);
        recipients.Select(_harness.ReceivingPlayer).Should().Equal(OtherDM, Player, DM);
    }

    [Test]
    public void AreaShout_FromAPossessedCreatureStaysInItsArea()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(FarPlayer, area: Spaceport);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, DMPossess, Cantina);

        _harness.Invoke<List<uint>>("AreaShout", "GetAreaShoutRecipients", Creature)
            .Should().Equal(Creature, Player);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void LocalSpeech_ReachesAPossessingDMThroughTheCreature(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        PossessEvent(DM, Creature);
        _harness.Possess(DM, Creature, possessionType, Cantina);

        var listeners = NearbyListeners(Player);

        listeners.Should().Equal(Player, Creature);
        listeners.Select(_harness.ReceivingPlayer).Should().Equal(Player, DM);
    }

    [Test]
    public void LocalSpeech_ReachesTheDMAvatarAfterRelease()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(DM, isDM: true, area: Cantina);
        PossessEvent(DM, Creature);
        _harness.Possess(DM, Creature, DMPossess, Spaceport);
        PossessEvent(Creature, DMPossessionHarness.Invalid);
        _harness.Release(DM);

        NearbyListeners(Player).Should().Equal(Player, DM);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void LocalSpeech_StillReachesTheDMAfterTheEngineRefusesToPossessAFamiliar(int possessionType)
    {
        // The possess events fire before the engine checks the target. The engine then refuses any creature that has a
        // master, so the DM never leaves their avatar and no release event follows.
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(DM, isDM: true, area: Cantina);
        _harness.Place(PlayerFamiliar, Spaceport);

        PossessEvent(DM, PlayerFamiliar);

        var listeners = NearbyListeners(Player);
        listeners.Should().Equal(Player, DM);
        listeners.Select(_harness.ReceivingPlayer).Should().Equal(Player, DM);

        // Possessing a creature the engine accepts and releasing it again changes nothing.
        PossessEvent(DM, Creature);
        _harness.Possess(DM, Creature, possessionType, Spaceport);
        PossessEvent(Creature, DMPossessionHarness.Invalid);
        _harness.Release(DM);
        NearbyListeners(Player).Should().Equal(Player, DM);
    }

    [Test]
    public void LocalSpeech_SkipsListenersInOtherAreas()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(FarPlayer, area: Spaceport);
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);

        NearbyListeners(Player).Should().Equal(Player);
    }

    [Test]
    public void PossessEvent_ClearsTheTargetsBusyStatus()
    {
        _harness.AddPlayer(DM, isDM: true);

        PossessEvent(DM, Creature);
        PossessEvent(Creature, DMPossessionHarness.Invalid);

        ((IList)_harness.GetField("Activity", "Cleared")).Cast<uint>().Should().Equal(Creature);
    }

    private void PossessingDM(int possessionType)
    {
        _harness.AddPlayer(DM, isDM: true, area: Spaceport);
        _harness.Possess(DM, Creature, possessionType, Cantina);
    }

    private void JoinParty(uint first, uint second) =>
        ((List<(uint, uint)>)_harness.GetField("Party", "Members")).Add((first, second));

    private void SetCommsFilter(uint actor, int filter) =>
        _harness.Invoke<bool>("DMPreference", "SetCommsFilter", actor, filter).Should().BeTrue();

    // Runs the DM possess handler the way NWNX does, before the engine handles the Possess or Release click.
    private void PossessEvent(uint self, uint target)
    {
        _harness.SetField("Communication", "Self", self);
        ((IDictionary<string, string>)_harness.GetField("EventsPlugin", "Data"))["TARGET"] = target.ToString();
        _harness.Invoke("Communication", "OnDMPossess");
    }

    private List<uint> NearbyListeners(uint sender) =>
        _harness.Invoke<List<uint>>("Communication", "NearbyListeners", sender, 20f);

    private List<uint> Readers(uint sender) => _harness.Invoke<List<uint>>("Communication", "Readers", sender);

    private List<uint> Muted(uint sender) => _harness.Invoke<List<uint>>("Communication", "Muted", sender);

    private int EmoteStyle(uint actor) => _harness.Invoke<int>("Communication", "GetStyle", actor);

    private bool SetEmoteStyle(uint actor, int style) => _harness.Invoke<bool>("Communication", "SetStyle", actor, style);
}
