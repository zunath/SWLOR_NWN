using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;

namespace SWLOR.Game.Server.Tests.Feature;

public class QuestLifecycleTests
{
    private Assembly _harness = null!;
    private dynamic _quest = null!;
    private Player _player = null!;

    [OneTimeSetUp]
    public void CompileActualLifecycleWithInMemoryEngineAndDatabaseBoundaries()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var service = Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service");
        var detail = File.ReadAllText(Path.Combine(service, "QuestService", "QuestDetail.cs"))
            .Replace("namespace SWLOR.Game.Server.Service.QuestService", "namespace QuestLifecycleHarness");
        var load = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(service, "Quest.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.Text == "LoadPlayerQuests")
            .WithAttributeLists(default).ToFullString();
        var boundaries = """
            namespace QuestLifecycleHarness
            {
                public static class Native
                {
                    public static bool GetIsPC(uint player) => true;
                    public static bool GetIsDM(uint player) => false;
                    public static bool GetIsDead(uint player) => false;
                    public static int GetCurrentHitPoints(uint player) => 100;
                    public static string GetObjectUUID(uint player) => "player";
                    public static uint GetEnteringObject() => 1;
                    public static void DelayCommand(float delay, Action action) => action();
                    public static void SendMessageToPC(uint player, string text) { }
                    public static void SetLocalString(uint player, string name, string value) { }
                    public static int GetCalendarDay() => 1;
                    public static int GetTimeHour() => 12;
                    public static void RemoveJournalQuestEntry(string id, uint player, bool party) { }
                }
                public static class DB
                {
                    public static SWLOR.Game.Server.Entity.Player Current;
                    public static T Get<T>(string id) => (T)(object)Current;
                    public static void Set<T>(T value) { }
                }
                public static class PlayerPlugin
                {
                    public static readonly List<JournalEntry> Entries = new();
                    public static void AddCustomJournalEntry(uint player, JournalEntry entry, bool update = false) => Entries.Add(entry);
                }
                public static class KeyItem
                {
                    public static void GiveKeyItem(uint player, KeyItemType key) { }
                    public static void RemoveKeyItem(uint player, KeyItemType key) { }
                }
                public static class QuestEncounter { public static void RefreshVisibilityForPlayer(uint player) { } }
                public static class Gui { public static void PublishRefreshEvent<T>(uint player, T value) { } }
                public static class EventsPlugin { public static void SignalEvent(string name, uint player) { } }
                public static class ConversationMenu { public static void Start(uint player, uint target, string name) { } }
                public static class Log { public static void Write(LogGroup group, string message) { } }
                public static class ColorToken { public static string Red(string text) => text; }
                public static class Quest
                {
                    public static QuestDetail Current;
                    public static QuestDetail GetQuestById(string id) => Current;
                    public static QuestDetail GetQuestByIdOrDefault(string id) => id == Current.QuestId ? Current : null;
                    LOAD_METHOD
                }
            }
            """.Replace("LOAD_METHOD", load);
        const string imports = """
            global using System;
            global using System.Collections.Generic;
            global using System.Linq;
            global using SWLOR.Game.Server.Entity;
            global using SWLOR.Game.Server.Service.QuestService;
            global using SWLOR.Game.Server.Service.KeyItemService;
            global using SWLOR.Game.Server.Service.LogService;
            global using SWLOR.NWN.API.NWNX;
            global using static QuestLifecycleHarness.Native;
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(QuestDetail).Assembly.Location).Append(typeof(SWLOR.NWN.API.NWNX.JournalEntry).Assembly.Location)
            .Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("QuestLifecycleHarness",
            [CSharpSyntaxTree.ParseText(imports), CSharpSyntaxTree.ParseText(detail), CSharpSyntaxTree.ParseText(boundaries)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray());
    }

    [SetUp]
    public void Reset()
    {
        _player = new Player("player");
        _quest = Activator.CreateInstance(_harness.GetType("QuestLifecycleHarness.QuestDetail")!)!;
        _quest.QuestId = "quest";
        _quest.Name = "Quest";
        _quest.IsRepeatable = true;
        _quest.States.Add(1, new QuestStateDetail { JournalText = "First step" });
        _quest.States.Add(2, new QuestStateDetail { JournalText = "Return for payment" });
        _harness.GetType("QuestLifecycleHarness.DB")!.GetField("Current")!.SetValue(null, _player);
        _harness.GetType("QuestLifecycleHarness.Quest")!.GetField("Current")!.SetValue(null, (object)_quest);
        Entries.Clear();
    }

    private List<SWLOR.NWN.API.NWNX.JournalEntry> Entries =>
        (List<SWLOR.NWN.API.NWNX.JournalEntry>)_harness.GetType("QuestLifecycleHarness.PlayerPlugin")!
            .GetField("Entries")!.GetValue(null)!;

    [Test]
    public void CompletedQuests_CannotPayOutAgainOrReturnAsActiveOnLogin()
    {
        var reward = new RecordingReward();
        _quest.Rewards.Add(reward);
        ((bool)_quest.Accept(1u, 2u)).Should().BeTrue();
        ((bool)_quest.Advance(1u, 2u)).Should().BeTrue();
        _quest.Complete(1u, 2u, null);
        reward.Grants.Should().Be(1);

        ((bool)_quest.CanComplete(1u)).Should().BeFalse();
        ((bool)_quest.Advance(1u, 2u)).Should().BeFalse();
        _quest.Complete(1u, 2u, null);
        reward.Grants.Should().Be(1);
        _player.Quests["quest"].TimesCompleted.Should().Be(1);

        Entries.Clear();
        _harness.GetType("QuestLifecycleHarness.Quest")!.GetMethod("LoadPlayerQuests")!.Invoke(null, null);
        Entries.Should().BeEmpty();
        _player.Quests.Should().ContainKey("quest", "completion history is needed by prerequisites");

        ((bool)_quest.Accept(1u, 2u)).Should().BeTrue("a repeatable quest can be explicitly accepted again");
        _player.Quests["quest"].DateLastCompleted.Should().BeNull();
        Entries.Should().ContainSingle().Which.State.Should().Be(1);
    }

    [Test]
    public void RestartAndAdvance_DiscardStaleCountersBeforeInitializingTheNewStage()
    {
        _player.Quests["quest"] = new PlayerQuest
        {
            CurrentState = 2, TimesCompleted = 1, DateLastCompleted = DateTime.UtcNow,
            ItemProgresses = new() { ["old_item"] = 5 },
            KillProgresses = new() { [NPCGroupType.Korriban_AlchemizedFrog] = 1 }
        };
        _quest.States[1].AddObjective(new RecordingObjective(_player, "first"));
        _quest.States[2].AddObjective(new RecordingObjective(_player, "second"));

        ((bool)_quest.Accept(1u, 2u)).Should().BeTrue();
        _player.Quests["quest"].ItemProgresses.Should().Equal(new Dictionary<string, int> { ["first"] = 1 });
        _player.Quests["quest"].KillProgresses.Should().BeEmpty();
        ((bool)_quest.Advance(1u, 2u)).Should().BeFalse();
        _player.Quests["quest"].ItemProgresses["first"] = 0;
        ((bool)_quest.Advance(1u, 2u)).Should().BeTrue();
        _player.Quests["quest"].ItemProgresses.Should().Equal(new Dictionary<string, int> { ["second"] = 1 });
        Entries.Last().Text.Should().Be("Return for payment");
    }

    [Test]
    public void ActiveQuestLogin_RepairsChangedObjectivesWithoutResettingExistingProgress()
    {
        _quest.States[1].AddObjective(new CollectItemObjective("peeled_crayfish", 1));
        _quest.States[1].AddObjective(new CollectItemObjective("other_item", 3));
        _player.Quests["quest"] = new PlayerQuest
        {
            CurrentState = 1, ItemProgresses = new() { ["cooked_crayfish"] = 1, ["other_item"] = 1 }
        };
        _harness.GetType("QuestLifecycleHarness.Quest")!.GetMethod("LoadPlayerQuests")!.Invoke(null, null);
        _player.Quests["quest"].ItemProgresses.Should().BeEquivalentTo(
            new Dictionary<string, int> { ["peeled_crayfish"] = 1, ["other_item"] = 1 });
        Entries.Should().ContainSingle().Which.Text.Should().Be("First step");
    }

    private sealed class RecordingReward : IQuestReward
    {
        public bool IsSelectable => false;
        public string MenuName => "Reward";
        public int Grants { get; private set; }
        public void GiveReward(uint player) => Grants++;
    }

    private sealed class RecordingObjective(Player player, string item) : IQuestObjective
    {
        public void Initialize(uint id, string questId) => player.Quests[questId].ItemProgresses[item] = 1;
        public void Advance(uint id, string questId) => player.Quests[questId].ItemProgresses[item] = 0;
        public bool IsComplete(uint id, string questId) => player.Quests[questId].ItemProgresses[item] == 0;
        public string GetCurrentStateText(uint id, string questId) => item;
    }
}
