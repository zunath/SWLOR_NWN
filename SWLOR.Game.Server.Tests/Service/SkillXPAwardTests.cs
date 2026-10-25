using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class SkillXPAwardTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void LoadHarness() => _harness = Harness;

    /// <summary>The production award flow compiled once and shared with the rank limit tests.</summary>
    internal static Type Harness => _sharedHarness ??= CompileProductionXPAwardWithControlledPlayerAndFeedback();
    private static Type? _sharedHarness;

    private static Type CompileProductionXPAwardWithControlledPlayerAndFeedback()
    {
        var root = new DirectoryInfo(Path.GetDirectoryName(SourcePath())!);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        var methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root!.FullName,
                "SWLOR.Game.Server", "Service", "Skill.cs"))).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "GiveSkillXP" or "ShouldDecaySkillRank" or "ApplyAbilityPoint" or "GetRankLimit" or "GetRankCeiling"
                or "GetDecayCandidates" or "IsBlockedBySkillCap" or "GetTotalContributingSkillRanks");

        // Run the production award flow, replacing native inputs, storage, and feedback only.
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using SWLOR.Game.Server.Entity;
            using SWLOR.Game.Server.Extension;
            using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
            using SWLOR.Game.Server.Service.SkillService;
            using SWLOR.Game.Server.Service.StatService;
            using SWLOR.NWN.API.NWScript.Enum;
            using RuntimeSkill = SWLOR.Game.Server.Service.Skill;

            public static class SkillXPHarness
            {
                private const uint OBJECT_INVALID = uint.MaxValue;
                private const int SkillCap = RuntimeSkill.SkillCap;
                private static readonly Random Random = new(0);
                public static Player Saved;
                public static readonly List<string> Messages = new();
                public static readonly List<string> Warnings = new();
                public static readonly List<string> WarningKeys = new();
                public static int Saves;
                public static readonly List<int> QueuedXP = new();
                public static int Refreshes;
                private static readonly XPQueue _xpMessages = new();
                {{string.Join(Environment.NewLine, methods)}}
                private static bool IsSkillCapContributingSkill(SkillType skill) => GetSkillDetails(skill).ContributesToSkillCap;
                private static bool GetIsPC(uint player) => true;
                private static bool GetIsDM(uint player) => false;
                private static bool GetIsDead(uint player) => false;
                private static int GetCurrentHitPoints(uint player) => 100;
                private static string GetObjectUUID(uint player) => "test-player";
                private static int GetAbilityScore(uint player, AbilityType type) => 0;
                private static int GetRequiredXP(int rank) => RuntimeSkill.GetRequiredXP(rank);
                private static SkillAttribute GetSkillDetails(SkillType skill) => skill.GetAttribute<SkillType, SkillAttribute>();
                private static void SendMessageToPC(uint player, string message) => Messages.Add(message);
                private static void FloatingTextStringOnCreature(string message, uint player, bool faction) => Messages.Add(message);
                private static class DB
                {
                    public static T Get<T>(string id) => (T)(object)Saved;
                    public static void Set(Player player) { Saved = player; Saves++; }
                }
                private static class Stat
                {
                    public static int GetStatAdjustment(uint player, StatType type) => 0;
                }
                private static class ColorToken
                {
                    public static string Red(string message) => message;
                    public static string Green(string message) => message;
                }
                private static class PlayerFeedback
                {
                    public static void SendWarningToPlayer(uint player, string key, string message) { WarningKeys.Add(key); Warnings.Add(message); }
                }
                private sealed class XPQueue
                {
                    public void Add(uint player, uint source, SkillType skill, int xp) => QueuedXP.Add(xp);
                }
                private static class Gui
                {
                    public static void PublishRefreshEvent(uint player, SkillXPRefreshEvent refresh) => Refreshes++;
                }
                private static class EventsPlugin
                {
                    public static void SignalEvent(string name, uint player) {}
                    public static void PushEventData(string key, string value) {}
                }
                public static void Award(Player player, int xp, uint source) => AwardSkill(player, SkillType.Mimicry, xp, source);
                public static int AwardSkill(Player player, SkillType skill, int xp, uint source)
                {
                    Saved = player;
                    Messages.Clear(); Warnings.Clear(); WarningKeys.Clear(); QueuedXP.Clear(); Refreshes = 0; Saves = 0;
                    return GiveSkillXP(1, skill, xp, ignoreBonuses: true,
                        applyHenchmanPenalty: false, xpMessageSource: source);
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(Skill).Assembly.Location)
            .Append(typeof(AbilityType).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("SkillXPAwardHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray()).GetType("SkillXPHarness")!;
    }

    [TestCase(400, true, uint.MaxValue)]
    [TestCase(400, true, 10u)]
    [TestCase(400, false, 10u)]
    [TestCase(399, true, uint.MaxValue)]
    public void MaxedMimicry_DoesNotWarnOrReportEarnedXP(int totalRanks, bool locked, uint source)
    {
        var player = CreatePlayer(50, totalRanks, locked);
        Award(player, 100, source);

        Warnings.Should().BeEmpty();
        Messages.Should().BeEmpty();
        Field<List<int>>("QueuedXP").Should().BeEmpty();
        player.Skills[SkillType.Mimicry].Rank.Should().Be(50);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        player.Skills.Values.Sum(skill => skill.Rank).Should().Be(totalRanks);
        Field<int>("Refreshes").Should().Be(0);
    }

    [TestCase(50, 0)]
    [TestCase(100, 0)]
    [TestCase(150, 50)]
    public void MaxedMimicry_StillRepaysXPDebt(int debt, int remainingDebt)
    {
        var player = CreatePlayer(50, 400, true);
        player.XPDebt = debt;
        Award(player, 100);

        player.XPDebt.Should().Be(remainingDebt);
        Warnings.Should().BeEmpty();
        Messages.Should().ContainSingle().Which.Should().Contain("debt");
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
    }

    [Test]
    public void UnmaxedMimicry_StillWarnsWhenTotalCapBlocksProgress()
    {
        var player = CreatePlayer(49, 400, true);
        Award(player, 100);

        Warnings.Should().ContainSingle().Which.Should().Contain("You cannot gain Mimicry XP");
        Messages.Should().BeEmpty();
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
    }

    [Test]
    public void UnmaxedMimicry_StillEarnsXPBelowTheTotalCap()
    {
        var player = CreatePlayer(49, 399, true);
        Award(player, 100);

        Warnings.Should().BeEmpty();
        Messages.Should().ContainSingle().Which.Should().Contain("You earned Mimicry skill experience. (100)");
        player.Skills[SkillType.Mimicry].XP.Should().Be(100);
    }

    [Test]
    public void ReachingMimicryMaximum_DiscardsExcessXPWithoutWarning()
    {
        var player = CreatePlayer(49, 399, true);
        Award(player, Skill.GetRequiredXP(49) + 100);

        Warnings.Should().BeEmpty();
        player.Skills[SkillType.Mimicry].Rank.Should().Be(50);
        player.Skills[SkillType.Mimicry].XP.Should().Be(0);
        player.Skills.Values.Sum(skill => skill.Rank).Should().Be(400);
    }

    private static Player CreatePlayer(int mimicryRank, int totalRanks, bool locked)
    {
        var player = new Player();
        player.Skills[SkillType.Mimicry] = new() { Rank = mimicryRank, IsLocked = true };
        var remaining = totalRanks - mimicryRank;
        foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Mimicry &&
                     skill.GetAttribute<SkillType, SkillAttribute>().ContributesToSkillCap))
        {
            var rank = Math.Min(remaining, skill.GetAttribute<SkillType, SkillAttribute>().MaxRank);
            player.Skills[skill] = new() { Rank = rank, IsLocked = locked };
            remaining -= rank;
            if (remaining == 0)
                break;
        }
        remaining.Should().Be(0);
        return player;
    }

    private void Award(Player player, int xp, uint source = uint.MaxValue) =>
        _harness.GetMethod("Award")!.Invoke(null, new object[] { player, xp, source });
    private T Field<T>(string name) => (T)_harness.GetField(name)!.GetValue(null)!;
    private List<string> Warnings => Field<List<string>>("Warnings");
    private List<string> Messages => Field<List<string>>("Messages");
    private static string SourcePath([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;
}
