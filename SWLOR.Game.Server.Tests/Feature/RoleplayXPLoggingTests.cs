using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.RoleplayXPService;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;

namespace SWLOR.Game.Server.Tests.Feature;

public class RoleplayXPLoggingTests
{
    private Assembly _harness = null!;
    private Player _player = null!;
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [OneTimeSetUp]
    public void CompileActualDistributionAndOOCProcessingWithRecordingBoundaries()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        root.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Feature", "RoleplayXP.cs"));
        var methods = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.Text is "ProcessPlayerRoleplayXP" or "ProcessOOCMessage")
            .Select(method => method.WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                .NormalizeWhitespace().ToFullString());
        var boundaries = """
            namespace RoleplayXPLoggingHarness
            {
                public static class Native
                {
                    public static bool Dead;
                    public static int SocialModifier;
                    public static OOCMessageContext Context;
                    public static bool GetIsPC(uint player) => true;
                    public static bool GetIsDM(uint player) => false;
                    public static bool GetIsDead(uint player) => Dead;
                    public static int GetCurrentHitPoints(uint player) => Dead ? 0 : 100;
                    public static string GetObjectUUID(uint player) => "player";
                    public static string GetPCPlayerName(uint player) => "account";
                    public static string GetPCPublicCDKey(uint player) => "TESTCDKEY";
                    public static int GetAbilityModifier(AbilityType ability, uint player) => SocialModifier;
                    public static uint GetArea(uint player) => 2;
                    public static string GetResRef(uint obj) => "test_area";
                    public static void SendMessageToPC(uint player, string text) { }
                }
                public static class DB
                {
                    public static Player Current;
                    public static string Saved;
                    public static T Get<T>(string id) => (T)(object)Current;
                    public static void Set(Player player) { Saved = JsonConvert.SerializeObject(player); }
                }
                public static class Property
                {
                    public static int CantinaLevel;
                    public static int GetEffectiveUpgradeLevel(string id, PropertyUpgradeType type) => CantinaLevel;
                }
                public class ApplicationSettings
                {
                    public static ApplicationSettings Get() => new();
                    public int RoleplayXPDailyOOCAllowance => 2;
                    public int RoleplayXPDMPresentOOCAllowance => 3;
                    public int? RoleplayXPEventAreaOOCAllowance => null;
                    public int RoleplayXPOOCPenaltyPercent => 10;
                }
                public static class Log
                {
                    public static readonly List<Dictionary<string, object>> Entries = new();
                    public static void WriteStructured(LogGroup group, string template, params object[] values)
                    {
                        var tokens = Regex.Matches(template, @"\{(\w+)(?::[^}]+)?\}");
                        if (tokens.Count != values.Length) throw new Exception("Log properties do not match values.");
                        var entry = tokens.Select((token, index) => (token.Groups[1].Value, values[index]))
                            .ToDictionary(pair => pair.Value, pair => pair.Item2);
                        entry["Template"] = template;
                        entry["Group"] = group;
                        entry["Saved"] = DB.Saved;
                        Entries.Add(entry);
                    }
                }
                public static class RoleplayXP
                {
                    private static void RefreshRPMessageTimestamp(uint player, DateTime now) { }
                    private static OOCMessageContext GetOOCMessageContext(uint player) => Native.Context;
                    METHODS
                }
            }
            """.Replace("METHODS", string.Join(Environment.NewLine, methods));
        const string imports = """
            global using System;
            global using System.Collections.Generic;
            global using System.Linq;
            global using System.Text.RegularExpressions;
            global using Newtonsoft.Json;
            global using SWLOR.Game.Server.Entity;
            global using SWLOR.Game.Server.Service.LogService;
            global using SWLOR.Game.Server.Service.PropertyService;
            global using SWLOR.Game.Server.Service.RoleplayXPService;
            global using SWLOR.NWN.API.NWScript.Enum;
            global using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;
            global using static RoleplayXPLoggingHarness.Native;
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(Player).Assembly.Location).Append(typeof(SWLOR.NWN.API.NWScript.Enum.AbilityType).Assembly.Location)
            .Append(typeof(JsonConvert).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("RoleplayXPLoggingHarness",
            [CSharpSyntaxTree.ParseText(imports), CSharpSyntaxTree.ParseText(boundaries)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray());
    }

    [SetUp]
    public void Reset()
    {
        _player = new Player("player") { Name = "Canonical character", UnallocatedXP = 100 };
        SetField("DB", "Current", _player);
        SetField("DB", "Saved", null);
        SetField("Native", "Dead", false);
        SetField("Native", "SocialModifier", 0);
        SetField("Native", "Context", OOCMessageContext.Standard);
        SetField("Property", "CantinaLevel", 0);
        Entries.Clear();
    }

    [TestCase(0, 0, 1500)]
    [TestCase(3, 30, 1050)]
    [TestCase(15, 100, 0)]
    public void Distribution_LogsIdentityAndPreResetCountsEvenWhenPenaltyConsumesAward(int steps, int percent, int awarded)
    {
        _player.RoleplayProgress.RPPoints = 50;
        _player.RoleplayProgress.PendingOOCPenaltySteps = steps;
        _player.RoleplayProgress.OOCMessagesSinceLastDistribution = 42;
        var before = DateTime.UtcNow;

        Invoke("ProcessPlayerRoleplayXP", 1u);

        var entry = Entries.Should().ContainSingle().Which;
        AssertIdentity(entry);
        ((DateTime)entry["OccurredUtc"]).Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        entry["RPPoints"].Should().Be(50);
        entry["OOCMessagesSinceLastDistribution"].Should().Be(42UL);
        entry["OOCPenaltySteps"].Should().Be(steps);
        entry["OOCPenaltyPercent"].Should().Be(percent);
        entry["XPBeforePenalty"].Should().Be(1500);
        entry["XPLostToOOCPenalty"].Should().Be(1500 - awarded);
        entry["AwardedXP"].Should().Be(awarded);
        entry["UnallocatedXP"].Should().Be(100 + awarded);
        var saved = JsonConvert.DeserializeObject<Player>((string)entry["Saved"])!;
        saved.RoleplayProgress.OOCMessagesSinceLastDistribution.Should().Be(0);
        saved.RoleplayProgress.PendingOOCPenaltySteps.Should().Be(0);
        saved.RoleplayProgress.TotalRPExpGained.Should().Be((ulong)awarded);
    }

    [Test]
    public void Distribution_LogsBonusesBeforeApplyingPenalty()
    {
        _player.RoleplayProgress.RPPoints = 54;
        _player.RoleplayProgress.PendingOOCPenaltySteps = 2;
        SetField("Native", "SocialModifier", 2);
        SetField("Property", "CantinaLevel", 4);

        Invoke("ProcessPlayerRoleplayXP", 1u);

        var entry = Entries.Single();
        entry["BonusXP"].Should().Be(100);
        entry["SocialModifier"].Should().Be(2);
        entry["CantinaLevel"].Should().Be(4);
        entry["XPBeforePenalty"].Should().Be(2650);
        entry["AwardedXP"].Should().Be(2120);
    }

    [Test]
    public void SkippedDistribution_PreservesCountAndPenaltiesUntilNextAward()
    {
        _player.RoleplayProgress.RPPoints = 49;
        _player.RoleplayProgress.PendingOOCPenaltySteps = 3;
        _player.RoleplayProgress.OOCMessagesSinceLastDistribution = 7;

        Invoke("ProcessPlayerRoleplayXP", 1u);

        var entry = Entries.Single();
        AssertIdentity(entry);
        entry["Reason"].Should().Be("InsufficientRPPoints");
        entry["OOCMessagesSinceLastDistribution"].Should().Be(7UL);
        _player.RoleplayProgress.PendingOOCPenaltySteps.Should().Be(3);
        _player.RoleplayProgress.OOCMessagesSinceLastDistribution.Should().Be(7);
        entry["Saved"].Should().BeNull();
    }

    [TestCase(OOCMessageContext.Standard, 3, 1)]
    [TestCase(OOCMessageContext.DMPresent, 3, 0)]
    [TestCase(OOCMessageContext.EventArea, 3, 0)]
    public void OOCMessage_LogsAllowedAndPenalizedMessagesWithContext(OOCMessageContext context, int count, int steps)
    {
        SetField("Native", "Context", context);
        for (var index = 0; index < count; index++)
            Invoke("ProcessOOCMessage", 1u, _player, Now, ChatChannel.PlayerParty, "//brb");

        var entry = Entries.Last();
        AssertIdentity(entry);
        entry["OccurredUtc"].Should().Be(Now);
        entry["Channel"].Should().Be(ChatChannel.PlayerParty);
        entry["Context"].Should().Be(context);
        entry["OOCMessagesSinceLastDistribution"].Should().Be((ulong)count);
        entry["PendingOOCPenaltySteps"].Should().Be(steps);
        entry["NextAwardOOCPenaltyPercent"].Should().Be(steps * 10);
        entry["Message"].Should().Be("//brb");
        JsonConvert.DeserializeObject<Player>((string)entry["Saved"])!.RoleplayProgress
            .OOCMessagesSinceLastDistribution.Should().Be((ulong)count);
    }

    [Test]
    public void DeadPlayer_DoesNotLogOrConsumeDistribution()
    {
        SetField("Native", "Dead", true);
        _player.RoleplayProgress.RPPoints = 50;
        _player.RoleplayProgress.OOCMessagesSinceLastDistribution = 5;
        Invoke("ProcessPlayerRoleplayXP", 1u);
        Entries.Should().BeEmpty();
        _player.RoleplayProgress.OOCMessagesSinceLastDistribution.Should().Be(5);
    }

    private static void AssertIdentity(Dictionary<string, object> entry)
    {
        entry["Group"].Should().Be(LogGroup.RoleplayXP);
        entry["PlayerId"].Should().Be("player");
        entry["CharacterName"].Should().Be("Canonical character");
        entry["AccountName"].Should().Be("account");
        entry["CDKey"].Should().Be("TESTCDKEY");
    }

    private List<Dictionary<string, object>> Entries => (List<Dictionary<string, object>>)_harness
        .GetType("RoleplayXPLoggingHarness.Log")!.GetField("Entries")!.GetValue(null)!;

    private void SetField(string type, string field, object value) => _harness
        .GetType("RoleplayXPLoggingHarness." + type)!.GetField(field)!.SetValue(null, value);

    private void Invoke(string method, params object[] arguments) => _harness
        .GetType("RoleplayXPLoggingHarness.RoleplayXP")!.GetMethod(method)!.Invoke(null, arguments);
}
