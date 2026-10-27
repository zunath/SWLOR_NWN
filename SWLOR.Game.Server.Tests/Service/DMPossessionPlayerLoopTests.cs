using System.Collections;
using FluentAssertions;
using NUnit.Framework;
using static SWLOR.Game.Server.Tests.Service.DMPossessionHarness;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Runs the production GetFirstPC/GetNextPC loops that treat DMs differently against the engine model of DM
/// possession. A possessing DM's avatar must still count as a DM, and anything sent to the DM must go to the
/// creature they control.
/// </summary>
public class DMPossessionPlayerLoopTests
{
    private const uint DM = 1;
    private const uint Creature = 2;
    private const uint Player = 3;
    private const uint OtherDM = 4;
    private const uint QuietPlayer = 5;
    private const uint Npc = 50;
    private const uint Cantina = 100;
    private const uint Spaceport = 101;

    private DMPossessionHarness _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionPlayerLoops()
    {
        _harness = Compile($$"""
            public static class DMPresence
            {
                {{Method("Service/DMPresence.cs", "GetControlledCreature")}}
                {{Method("Service/DMPresence.cs", "IsControlledByDM")}}
                {{Method("Service/DMPresence.cs", "IsDMPresentInArea")}}
                {{Method("Service/DMPresence.cs", "SendMessage")}}
            }

            public static class DMPlayerDirectory
            {
                {{Method("Service/DMPlayerDirectory.cs", "GetOnlinePlayers")}}
            }

            public static class CombatRecorder
            {
                private static uint _area;
                public static readonly List<uint> Registered = new();
                private static int GetParticipantId(uint creature)
                {
                    Registered.Add(creature);
                    return Registered.Count;
                }
                {{Method("Service/CombatRecorder.cs", "RegisterPlayersInArea")}}
            }

            public static class AI
            {
                [Flags]
                public enum AIFlag { None = 0, ReturnHome = 1 }
                public static AIFlag GetAIFlag(uint creature) => AIFlag.ReturnHome;
                private static bool GetIsEnemy(uint target, uint source) => true;
                {{Method("Service/AI.cs", "ShouldUseCombatLeash")}}
            }

            public static class Mimicry
            {
                public enum FeatType { None, Strike, Technique }
                public sealed class AbilityDetail { }
                private const float WitnessRadius = 15.0f;
                private static readonly Dictionary<FeatType, FeatType> _techniqueByNpcFeat = new() { [FeatType.Strike] = FeatType.Technique };
                private static readonly Dictionary<FeatType, AbilityDetail> _techniques = new() { [FeatType.Technique] = new AbilityDetail() };
                public static readonly List<uint> Witnesses = new();
                private static void TryRecordWitness(uint npc, uint player, FeatType techniqueFeat, AbilityDetail techniqueDetail) =>
                    Witnesses.Add(player);
                private static void TryRecordAnalysis(uint npc, uint player) { }
                public static void UseStrike(uint activator) => OnCreatureAbilityUsed(activator, FeatType.Strike);
                {{Method("Service/Mimicry.cs", "OnCreatureAbilityUsed")}}
                {{Method("Service/Mimicry.cs", "GetNearbyPlayers")}}
            }

            {{Type("Service/RoleplayXPService/OOCMessageContext.cs", "OOCMessageContext")}}
            public static class Communication { public const string EventCommsAreaVariable = "EVENT_COMMS_AREA"; }
            public static class RoleplayXP
            {
                private static bool GetLocalBool(uint obj, string name) => false;
                {{Method("Feature/RoleplayXP.cs", "GetOOCMessageContext")}}
            }

            public sealed class PlayerSettings { public bool DisplayServerResetReminders; }
            public sealed class Player { public PlayerSettings Settings = new(); }
            public static class DB
            {
                public static readonly Dictionary<string, object> Records = new();
                public static void AddPlayer(uint player, bool reminders) =>
                    Records[player.ToString()] = new Player { Settings = new PlayerSettings { DisplayServerResetReminders = reminders } };
                public static T Get<T>(string id) where T : class => Records.TryGetValue(id, out var record) ? (T)record : null;
            }
            public static class RestartReminder
            {
                public static void Send(string message)
                {
                    {{PlayerLoop("Feature/ServerTasks.cs", "DisplayServerResetReminders")}}
                }
            }
            """);
    }

    [SetUp]
    public void Reset()
    {
        _harness.Reset();
        ((IList)_harness.GetField("CombatRecorder", "Registered")).Clear();
        ((IList)_harness.GetField("Mimicry", "Witnesses")).Clear();
        ((IDictionary)_harness.GetField("DB", "Records")).Clear();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void DMPlayerSearch_ListsPlayersButNotDMs(int possessionType)
    {
        _harness.AddPlayer(Player);
        _harness.AddPlayer(OtherDM, isDM: true);
        PossessingDM(possessionType, Cantina, Cantina);

        _harness.Invoke<List<uint>>("DMPlayerDirectory", "GetOnlinePlayers").Should().Equal(Player);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void CombatRecorder_RegistersPlayersButNotDMs(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Cantina);
        PossessingDM(possessionType, Cantina, Cantina);
        _harness.SetField("CombatRecorder", "_area", Cantina);

        _harness.Invoke("CombatRecorder", "RegisterPlayersInArea");

        ((List<uint>)_harness.GetField("CombatRecorder", "Registered")).Should().Equal(Player);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void CombatLeash_IgnoresAPossessingDMsAvatar(int possessionType)
    {
        _harness.Place(Npc, Cantina);
        PossessingDM(possessionType, Cantina, Spaceport);

        ShouldUseCombatLeash().Should().BeFalse();
    }

    [Test]
    public void CombatLeash_AppliesWhenAHostilePlayerIsInTheArea()
    {
        _harness.Place(Npc, Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Cantina);
        _harness.AddPlayer(Player, area: Cantina);

        ShouldUseCombatLeash().Should().BeTrue();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void TechniqueWitnesses_IncludePlayersButNotDMs(int possessionType)
    {
        _harness.Place(Npc, Cantina);
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Cantina);
        PossessingDM(possessionType, Cantina, Cantina);

        _harness.Invoke("Mimicry", "UseStrike", Npc);

        ((List<uint>)_harness.GetField("Mimicry", "Witnesses")).Should().Equal(Player);
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void OOCMessageContext_FlagsAnAreaWhereADMPossessesACreature(int possessionType)
    {
        _harness.AddPlayer(Player, area: Cantina);
        PossessingDM(possessionType, Spaceport, Cantina);

        GetOOCMessageContext(Player).Should().Be("DMPresent");
    }

    [Test]
    public void OOCMessageContext_IsStandardWithoutADMInTheArea()
    {
        _harness.AddPlayer(Player, area: Cantina);
        _harness.AddPlayer(OtherDM, isDM: true, area: Spaceport);

        GetOOCMessageContext(Player).Should().Be("Standard");
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void RestartReminder_ReachesDMsAndOptedInPlayers(int possessionType)
    {
        _harness.AddPlayer(Player);
        _harness.AddPlayer(QuietPlayer);
        _harness.AddPlayer(OtherDM, isDM: true);
        PossessingDM(possessionType, Spaceport, Cantina);
        _harness.Invoke("DB", "AddPlayer", Player, true);
        _harness.Invoke("DB", "AddPlayer", QuietPlayer, false);

        _harness.Invoke("RestartReminder", "Send", "Restarting soon.");

        _harness.Messages.Should().Equal((Player, "Restarting soon."), (OtherDM, "Restarting soon."), (DM, "Restarting soon."));
    }

    private void PossessingDM(int possessionType, uint avatarArea, uint creatureArea)
    {
        _harness.AddPlayer(DM, isDM: true, area: avatarArea);
        _harness.Possess(DM, Creature, possessionType, creatureArea);
    }

    private bool ShouldUseCombatLeash() => _harness.Invoke<bool>("AI", "ShouldUseCombatLeash", Npc);

    private string GetOOCMessageContext(uint player) =>
        _harness.Invoke("RoleplayXP", "GetOOCMessageContext", player).ToString()!;
}
