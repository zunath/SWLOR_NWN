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

public class QuestKillCreditTests
{
    private Assembly _harness = null!;

    [OneTimeSetUp]
    public void CompileActualDeathHandlerWithRecordingBoundaries()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        var source = File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service", "Quest.cs"));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.Text == "ProgressKillTargetObjectives")
            .WithAttributeLists(default).ToFullString();
        var boundaries = """
            using System;
            using System.Collections.Generic;
            using SWLOR.Game.Server.Entity;
            using SWLOR.Game.Server.Service.NPCService;
            namespace KillCreditHarness;
            public enum CreatureType { PlayerCharacter }
            public static class Quest
            {
                public static uint OBJECT_SELF = 2, OBJECT_INVALID = 0;
                public static float Distance, Range;
                public static int Credits, Advances;
                public static bool SameArea = true;
                public static bool HasProof = true;
                public static int GetLocalInt(uint o, string key) => (int)NPCGroupType.Viscara_ValleyCairnmogs;
                public static List<string> GetQuestsAssociatedWithNPCGroup(NPCGroupType group) => new() { "test" };
                public static uint GetNearestCreature(CreatureType type, int n, uint o) => 1;
                public static uint GetFirstFactionMember(uint o) => 1;
                public static uint GetNextFactionMember(uint o) => 0;
                public static bool GetIsObjectValid(uint o) => o != 0;
                public static bool GetIsPC(uint o) => true;
                public static bool GetIsDM(uint o) => false;
                public static bool GetIsDead(uint o) => false;
                public static int GetCurrentHitPoints(uint o) => 100;
                public static uint GetArea(uint o) => SameArea || o != 1 ? 10u : 11u;
                public static float GetDistanceBetween(uint a, uint b) => Distance;
                public static string GetObjectUUID(uint o) => "player";
                public static Detail GetQuestById(string id) => new();
                METHOD
            }
            public static class QuestEncounter
            {
                public static bool IsQuestEncounterCreature(uint o) => false;
                public static void ProgressKillCredit(uint o, NPCGroupType group, List<string> quests) { }
                public static void ClearEncounterForCreature(uint o) { }
            }
            public static class Enmity { public static uint GetHighestEnmityTarget(uint o) => 1; }
            public static class DB
            {
                public static T Get<T>(string id) => (T)(object)new Player("player")
                    { Quests = new() { ["test"] = new PlayerQuest { CurrentState = 1 } } };
            }
            public class Detail
            {
                public Dictionary<int, State> States = new() { [1] = new() };
                public void Advance(uint player, uint creature) => Quest.Advances++;
            }
            public class State { public object[] GetObjectives() => new object[] { new KillTargetObjective() }; }
            public class KillTargetObjective
            {
                public NPCGroupType Group => NPCGroupType.Viscara_ValleyCairnmogs;
                public float MaximumCreditDistance => Quest.Range;
                public bool CountsKill(uint player, uint creature) => Quest.HasProof;
                public void Advance(uint player, string id) => Quest.Credits++;
            }
            """.Replace("METHOD", method);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Player).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("KillCreditHarness", [CSharpSyntaxTree.ParseText(boundaries)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray());
    }

    [TestCase(50f, 50f, 1, true)]
    [TestCase(50.1f, 50f, 0, true)]
    [TestCase(52f, 55f, 1, true)]
    [TestCase(55f, 55f, 1, true)]
    [TestCase(55.1f, 55f, 0, true)]
    [TestCase(10f, 50f, 0, false)]
    public void DeathHandler_RequiresCreditRangeAndProof(float distance, float range, int expected, bool hasProof)
    {
        var quest = _harness.GetType("KillCreditHarness.Quest")!;
        quest.GetField("Distance")!.SetValue(null, distance);
        quest.GetField("Range")!.SetValue(null, range);
        quest.GetField("HasProof")!.SetValue(null, hasProof);
        quest.GetField("Credits")!.SetValue(null, 0);
        quest.GetField("Advances")!.SetValue(null, 0);
        quest.GetMethod("ProgressKillTargetObjectives")!.Invoke(null, null);
        quest.GetField("Credits")!.GetValue(null).Should().Be(expected);
        quest.GetField("Advances")!.GetValue(null).Should().Be(expected);
    }

    [Test]
    public void ExistingKillObjectives_KeepTheirDefaultRange()
    {
        new KillTargetObjective(NPCGroupType.Viscara_ValleyCairnmogs, 10).MaximumCreditDistance.Should().Be(50f);
    }
}
