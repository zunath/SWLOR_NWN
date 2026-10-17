using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace SWLOR.Game.Server.Tests.Feature;

public class DisguiseAuditTests
{
    private Type _harness = null!;

    [OneTimeSetUp]
    public void CompileProductionMutations()
    {
        var disguiseSource = ReadSource("Service/Disguise.cs");
        var methods = new[]
        {
            "ApplyActiveDisguiseOnEnter", "BuildIdentityKey", "GetIdentityKey", "GetDisplayDescriptor",
            "GetActiveDisguise", "CreateDisguise", "SaveDisguise", "Activate", "Deactivate", "Retire",
            "Unretire", "DeleteRetiredDisguise", "ResetActivationCooldowns", "ValidatePrivateName", "IsActiveDisguise"
        };
        var disguiseMethods = disguiseSource.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => methods.Contains(method.Identifier.ValueText))
            .Select(method => method.WithAttributeLists(default).ToFullString());
        var constants = disguiseSource.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(field => field.Modifiers.Any(SyntaxKind.ConstKeyword));
        var descriptorSource = ReadSource("Service/PlayerDescriptor.cs");
        var descriptorMethods = descriptorSource.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText is "SetUnknownDisplayName" or "EnsureUnknownDisplayName")
            .Select(method => method.WithAttributeLists(default).ToFullString());
        var getDm = ReadSource("Service/DMPresence.cs").DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == "GetDM");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Newtonsoft.Json;
            using static World;

            public sealed class IndexedAttribute : Attribute { }
            public class EntityBase { public string Id { get; set; } = Guid.NewGuid().ToString(); }
            {{ReadSource("Entity/PlayerDisguise.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single()}}
            {{ReadSource("Service/PlayerListService/PlayerListFlagType.cs").DescendantNodes().OfType<EnumDeclarationSyntax>().Single()}}
            {{ReadSource("Service/PlayerListService/PlayerListFlagAttribute.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single()}}
            {{ReadSource("Service/DisguiseService/SaveDisguiseResult.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single()}}
            {{ReadSource("Service/DisguiseService/ActivateDisguiseResult.cs").DescendantNodes().OfType<ClassDeclarationSyntax>().Single()}}
            {{ReadSource("Service/DisguiseService/DeleteRetiredDisguiseResult.cs").DescendantNodes().OfType<EnumDeclarationSyntax>().Single()}}
            {{ReadSource("Service/DisguiseService/DisguisePaymentMethod.cs").DescendantNodes().OfType<EnumDeclarationSyntax>().Single()}}

            public sealed class Player : EntityBase
            {
                public string ActiveDisguiseId = "", UnknownDisplayName = "Base descriptor";
                public int UndisguisedPortraitId = -1, UndisguisedSoundSetId = -1, UnallocatedXP = 100000;
                public string UndisguisedPortraitResref = "", UndisguisedDescription = "";
                public bool HasUndisguisedDescriptionSnapshot;
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0x7f000000;
                public static uint EnteringObject = 1;
                public static string Name = "Canonical Player";
                public static bool GetIsObjectValid(uint player) => player is 1 or 2 or 3;
                public static bool GetIsPC(uint player) => player == 1;
                public static bool GetIsDM(uint player) => false;
                public static bool GetIsDMPossessed(uint player) => player == 3;
                public static uint GetMaster(uint player) => player == 3 ? 2u : OBJECT_INVALID;
                public static string GetObjectUUID(uint player) => player == 3 ? "possessed-id" : player == 2 ? "staff-id" : "owner-id";
                public static string GetName(uint player) => player == 3 ? "Possessed NPC" : player == 2 ? "Staff Actor" : Name;
                public static uint GetEnteringObject() => EnteringObject;
                public static string GetPCPublicCDKey(uint player) => "public-cd-key";
                public static int GetGold(uint player) => 100000;
                public static void TakeGoldFromCreature(int amount, uint player, bool destroy) { }
                public static int GetPortraitId(uint player) => 10;
                public static string GetPortraitResRef(uint player) => "baseportrait";
                public static int GetSoundset(uint player) => 10;
                public static string GetDescription(uint player, bool original = false) => "Original biography";
                public static void SetPortraitId(uint player, int portrait) { }
                public static void SetPortraitResRef(uint player, string portrait) { }
                public static void SetSoundset(uint player, int sound) { }
                public static void SetDescription(uint player, string description) { }
            }
            public sealed class DBQuery<T>
            {
                public string OwnerId;
                public int Offset, Count = 50;
                public DBQuery<T> AddFieldSearch(string field, object value, bool fuzzy)
                { if (field == "PlayerId") OwnerId = (string)value; return this; }
                public DBQuery<T> AddPaging(int count, int offset) { Count = count; Offset = offset; return this; }
            }
            public static class DB
            {
                public static readonly Dictionary<(Type, string), EntityBase> Records = new();
                public static T Get<T>(string id) where T : EntityBase => Records.GetValueOrDefault((typeof(T), id)) as T;
                public static void Set<T>(T value) where T : EntityBase => Records[(typeof(T), value.Id)] = value;
                public static void Delete<T>(string id) => Records.Remove((typeof(T), id));
                public static IEnumerable<T> Search<T>(DBQuery<T> query) where T : EntityBase
                    => Records.Values.OfType<T>()
                        .Where(value => value is not PlayerDisguise disguise || disguise.PlayerId == query.OwnerId)
                        .Skip(query.Offset).Take(query.Count).ToArray();
            }
            public enum LogGroup { PlayerName, PublicDescriptors }
            public static class Log
            {
                public static readonly List<object[]> Records = new();
                public static readonly Dictionary<object[], string> Groups = new();
                public static void WriteStructured(LogGroup group, string template, params object[] values)
                {
                    var record = new object[] { template }.Concat(values).ToArray();
                    Records.Add(record);
                    Groups[record] = group.ToString();
                }
            }
            public static class UtilPlugin { public static string StripColors(string value) => value; }
            public static class Gui { public static void PublishRefreshEvent(uint player, object refresh) { } }
            public sealed class RPXPRefreshEvent { }
            public static class DMPresence { {{getDm}} }
            public static class PlayerName
            {
                public static string SanitizeKnownName(string name) => name.Trim();
                public static string ValidateKnownNameInput(string name) => string.IsNullOrWhiteSpace(name) ? "Required" : "";
                public static void RefreshNameOverridesForPlayer(uint player) { }
                public static int DeleteKnownNameReferences(string identityKey) => 3;
            }
            public static class PlayerDescriptor
            {
                public static string GetUnknownDisplayName(uint player) => DB.Get<Player>(GetObjectUUID(player)).UnknownDisplayName;
                public static string GenerateUnknownDisplayName(Player player) => "A Generated Human";
                {{string.Join("\n", descriptorMethods)}}
            }
            public static class Disguise
            {
                {{string.Join("\n", constants)}}
                {{string.Join("\n", disguiseMethods)}}
                private static int CountUsedSlots(string playerId) => DB.Records.Values.OfType<PlayerDisguise>().Count(row => row.PlayerId == playerId);
                private static int GetDisguiseSlotLimit(uint player, Player dbPlayer) => 3;
                private static int ResolvePortraitInternalId(uint player) => 10;
                private static int GetMaxPortraitCount() => 100;
                private static int ResolveSoundSetId(int id) => id;
                private static string ValidateActivationDelay(uint player, string playerId) => "";
                private static void ApplyAppearance(uint player, PlayerDisguise disguise) { }
                private static void RefreshDisguiseDisplay(uint player) { }
            }
            public static class Harness
            {
                public const string OldBiography = "Old\n\"bio\" {text}";
                public const string NewBiography = "New\n\"bio\" {text}";
                private static void Reset()
                {
                    DB.Records.Clear();
                    Log.Records.Clear();
                    Log.Groups.Clear();
                    DB.Set(new Player { Id = "owner-id" });
                }
                public static object[][] Lifecycle()
                {
                    Reset();
                    var disguise = Disguise.CreateDisguise(1);
                    if (!Disguise.SaveDisguise(1, disguise.Id, "Old mask", "Old descriptor", OldBiography, 20, 21, true).IsSuccessful)
                        throw new Exception("Initial save failed");
                    if (!Disguise.Activate(1, disguise.Id).IsSuccessful) throw new Exception("Activation failed");
                    if (!Disguise.SaveDisguise(1, disguise.Id, "New mask", "New descriptor", NewBiography, 30, 31, false).IsSuccessful)
                        throw new Exception("Active save failed");
                    Disguise.ApplyActiveDisguiseOnEnter();
                    Disguise.ResetActivationCooldowns(1, 2);
                    Disguise.Deactivate(1);
                    Disguise.Retire(1, disguise.Id);
                    Disguise.Unretire(1, disguise.Id);
                    Disguise.Retire(1, disguise.Id);
                    if (Disguise.DeleteRetiredDisguise(1, disguise.Id, DisguisePaymentMethod.Credits) != DeleteRetiredDisguiseResult.Success)
                        throw new Exception("Delete failed");
                    if (DB.Get<PlayerDisguise>(disguise.Id) != null) throw new Exception("Disguise was not deleted");
                    return Log.Records.ToArray();
                }
                public static object[][] DescriptorChanges()
                {
                    Reset();
                    var player = DB.Get<Player>("owner-id");
                    player.UnknownDisplayName = "";
                    if (!PlayerDescriptor.EnsureUnknownDisplayName(1)) throw new Exception("Generation failed");
                    if (PlayerDescriptor.EnsureUnknownDisplayName(1)) throw new Exception("Descriptor regenerated");
                    var disguise = Disguise.CreateDisguise(1);
                    Disguise.Activate(1, disguise.Id);
                    Log.Records.Clear();
                    PlayerDescriptor.SetUnknownDisplayName(1, "New base descriptor");
                    if (Disguise.GetDisplayDescriptor(1) != "A Generated Human") throw new Exception("Active descriptor changed");
                    return Log.Records.ToArray();
                }
                public static object[][] Generation()
                {
                    Reset();
                    DB.Get<Player>("owner-id").UnknownDisplayName = "";
                    PlayerDescriptor.EnsureUnknownDisplayName(1);
                    PlayerDescriptor.EnsureUnknownDisplayName(1);
                    return Log.Records.ToArray();
                }
                public static object[][] UnchangedDisguiseDescriptor()
                {
                    Reset();
                    var disguise = Disguise.CreateDisguise(1);
                    Log.Records.Clear();
                    Disguise.SaveDisguise(1, disguise.Id, "Changed private name", disguise.Descriptor, "Changed biography", 20, 21, true);
                    return Log.Records.ToArray();
                }
                public static string GroupOf(object[] record) => Log.Groups[record];
                public static object[][] RejectedSave()
                {
                    Reset();
                    var disguise = Disguise.CreateDisguise(1);
                    Log.Records.Clear();
                    if (Disguise.SaveDisguise(1, disguise.Id, "", "Descriptor", "Biography", 20, 21, true).IsSuccessful)
                        throw new Exception("Invalid save accepted");
                    return Log.Records.ToArray();
                }
                public static object[][] MultilineRejectedSave()
                {
                    Reset();
                    var disguise = Disguise.CreateDisguise(1);
                    Log.Records.Clear();
                    Disguise.SaveDisguise(1, disguise.Id, "Invalid\n" + new string('x', 40),
                        "Descriptor\r\n2026-01-01 forged event", "Biography\r\nForged event", 20, 21, true);
                    return Log.Records.ToArray();
                }
                public static object[][] PossessedReset()
                {
                    Reset();
                    var disguise = Disguise.CreateDisguise(1);
                    Disguise.Activate(1, disguise.Id);
                    Log.Records.Clear();
                    Disguise.ResetActivationCooldowns(1, 3);
                    return Log.Records.ToArray();
                }
                public static object[][] RejectedLifecycle()
                {
                    Reset();
                    Disguise.Activate(1, "missing-id");
                    Disguise.Deactivate(1);
                    Disguise.Retire(1, "missing-id");
                    Disguise.Unretire(1, "missing-id");
                    Disguise.DeleteRetiredDisguise(1, "missing-id", DisguisePaymentMethod.Credits);
                    return Log.Records.ToArray();
                }
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Append(typeof(JsonConvert).Assembly.Location).Distinct()
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("DisguiseAuditHarness", [CSharpSyntaxTree.ParseText(source)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _harness = Assembly.Load(stream.ToArray()).GetType("Harness")!;
    }

    [Test]
    public void Lifecycle_RecordsEveryMutationWithCanonicalOwnerAndStableDisguiseId()
    {
        var records = Run("Lifecycle");
        records.Select(record => ((string)record[0]).Split(':')[0]).Should().Equal(
            "Disguise created", "Disguise saved", "Disguise public descriptor changed", "Disguise activated", "Disguise saved",
            "Disguise public descriptor changed",
            "Disguise reapplied on login", "Disguise activation cooldown reset", "Disguise deactivated",
            "Disguise retired", "Disguise restored", "Disguise retired", "Retired disguise deleted");
        var disguiseId = Field(records[0], "DisguiseId");
        foreach (var record in records)
        {
            Field(record, "PlayerId").Should().Be("owner-id");
            Field(record, "PlayerName").Should().Be("Canonical Player");
            Field(record, "DisguiseId").Should().Be(disguiseId);
            GroupOf(record).Should().Be(((string)record[0]).StartsWith("Disguise public descriptor changed:")
                ? "PublicDescriptors" : "PlayerName");
        }
        var reapplied = records.Single(record => ((string)record[0]).StartsWith("Disguise reapplied on login:"));
        Field(reapplied, "Descriptor").Should().Be("New descriptor");
        var reset = records.Single(record => ((string)record[0]).StartsWith("Disguise activation cooldown reset:"));
        Field(reset, "PreviousDateLastActivated").Should().BeOfType<DateTime>();
        Field(reset, "ActorPlayerId").Should().Be("staff-id");
        Field(reset, "ActorName").Should().Be("Staff Actor");
    }

    [Test]
    public void Customization_RetainsEveryPreviousAndNewValueIncludingSameLengthBiographyEdits()
    {
        var save = Run("Lifecycle").Last(record => ((string)record[0]).StartsWith("Disguise saved:"));
        Field(save, "PreviousPrivateName").Should().Be("Old mask");
        Field(save, "PrivateName").Should().Be("New mask");
        Field(save, "PreviousDescriptor").Should().Be("Old descriptor");
        Field(save, "Descriptor").Should().Be("New descriptor");
        JsonConvert.DeserializeObject<string>((string)Field(save, "PreviousBiography")).Should().Be("Old\n\"bio\" {text}");
        JsonConvert.DeserializeObject<string>((string)Field(save, "Biography")).Should().Be("New\n\"bio\" {text}");
        Field(save, "PreviousBiographyLength").Should().Be(Field(save, "BiographyLength"));
        Field(save, "PreviousPortraitInternalId").Should().Be(20);
        Field(save, "PortraitInternalId").Should().Be(30);
        Field(save, "PreviousSoundSetId").Should().Be(21);
        Field(save, "SoundSetId").Should().Be(31);
        Field(save, "PreviousScrambleAccountId").Should().Be(true);
        Field(save, "ScrambleAccountId").Should().Be(false);
    }

    [Test]
    public void Deletion_RetainsCustomizationAndPaymentAfterTheDisguiseRecordIsGone()
    {
        var deleted = Run("Lifecycle").Last();
        JsonConvert.DeserializeObject<string>((string)Field(deleted, "Biography")).Should().Be("New\n\"bio\" {text}");
        Field(deleted, "Descriptor").Should().Be("New descriptor");
        Field(deleted, "PrivateName").Should().Be("New mask");
        Field(deleted, "PortraitInternalId").Should().Be(30);
        Field(deleted, "SoundSetId").Should().Be(31);
        Field(deleted, "ScrambleAccountId").Should().Be(false);
        Field(deleted, "PaymentMethod").Should().Be("Credits");
        Field(deleted, "PaymentAmount").Should().Be(100000);
        Field(deleted, "RemovedKnownNameReferences").Should().Be(3);
    }

    [Test]
    public void BaseDescriptorChange_RecordsOldAndNewValuesWhileKeepingActiveDisguiseTraceable()
    {
        var records = Run("DescriptorChanges");
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PublicDescriptors");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        Field(records[0], "PlayerName").Should().Be("Canonical Player");
        Field(records[0], "IdentityKey").Should().Be("owner-id");
        Field(records[0], "ActiveIdentityKey").Should().BeOfType<string>().Which.Should().StartWith("disguise:");
        Field(records[0], "PreviousDescriptor").Should().Be("A Generated Human");
        Field(records[0], "Descriptor").Should().Be("New base descriptor");
    }

    [Test]
    public void DescriptorGeneration_IsLoggedOnlyWhenANewDescriptorIsPersisted()
    {
        var records = Run("Generation");
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PublicDescriptors");
        ((string)records[0][0]).Should().StartWith("Base descriptor generated:");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        Field(records[0], "IdentityKey").Should().Be("owner-id");
        Field(records[0], "ActiveIdentityKey").Should().Be("owner-id");
        Field(records[0], "PreviousDescriptor").Should().Be("");
        Field(records[0], "Descriptor").Should().Be("A Generated Human");
    }

    [Test]
    public void RejectedCustomization_RecordsAttemptedValuesAndReason()
    {
        var records = Run("RejectedSave");
        records.Should().ContainSingle();
        ((string)records[0][0]).Should().StartWith("Disguise customization rejected:");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        JsonConvert.DeserializeObject<string>((string)Field(records[0], "Descriptor")).Should().Be("Descriptor");
        JsonConvert.DeserializeObject<string>((string)Field(records[0], "Biography")).Should().Be("Biography");
        Field(records[0], "Reason").Should().Be("Please enter a private disguise name.");
    }

    [Test]
    public void RejectedLifecycle_RecordsAttemptsWithoutSuccessfulStateChanges()
    {
        var records = Run("RejectedLifecycle");
        records.Select(record => ((string)record[0]).Split(':')[0]).Should().Equal(
            "Disguise activation rejected", "Disguise deactivation rejected", "Disguise retirement rejected",
            "Disguise restoration rejected", "Disguise deletion rejected");
        foreach (var record in records)
        {
            Field(record, "PlayerId").Should().Be("owner-id");
            Field(record, "Reason").Should().BeOfType<string>().Which.Should().NotBeEmpty();
        }
    }

    [Test]
    public void MultilineCustomization_FormatsAsOnePhysicalAuditLineWithoutLosingText()
    {
        using var logger = new LoggerConfiguration().CreateLogger();
        var formatter = new MessageTemplateTextFormatter("{Timestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null);
        foreach (var record in Run("Lifecycle").Concat(Run("MultilineRejectedSave")))
        {
            logger.BindMessageTemplate((string)record[0], record.Skip(1).ToArray(), out var template, out var properties)
                .Should().BeTrue();
            var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template!, properties!);
            using var writer = new StringWriter();
            formatter.Format(logEvent, writer);
            writer.ToString().TrimEnd('\r', '\n').Should().NotContain("\r").And.NotContain("\n");
        }
        var rejected = Run("MultilineRejectedSave").Single();
        JsonConvert.DeserializeObject<string>((string)Field(rejected, "Biography")).Should().Be("Biography\r\nForged event");
        JsonConvert.DeserializeObject<string>((string)Field(rejected, "Descriptor")).Should().Be("Descriptor\r\n2026-01-01 forged event");
    }

    [Test]
    public void CooldownReset_AttributesPossessedActorsToTheStaffCharacter()
    {
        var records = Run("PossessedReset");
        records.Should().ContainSingle();
        Field(records[0], "ActorPlayerId").Should().Be("staff-id");
        Field(records[0], "ActorName").Should().Be("Staff Actor");
    }

    [Test]
    public void DisguiseDescriptorEdits_WritePublicHistoryWithTheIdentityAndActiveState()
    {
        var records = Run("Lifecycle").Where(record => GroupOf(record) == "PublicDescriptors").ToArray();
        records.Should().HaveCount(2);
        Field(records[0], "PreviousDescriptor").Should().Be("Base descriptor");
        Field(records[0], "Descriptor").Should().Be("Old descriptor");
        Field(records[0], "IsActive").Should().Be(false);
        Field(records[1], "PreviousDescriptor").Should().Be("Old descriptor");
        Field(records[1], "Descriptor").Should().Be("New descriptor");
        Field(records[1], "IsActive").Should().Be(true);
        foreach (var record in records)
            Field(record, "IdentityKey").Should().Be($"disguise:{Field(record, "DisguiseId")}");
    }

    [Test]
    public void OtherDisguiseCustomization_DoesNotWriteAPublicDescriptorChange()
    {
        var records = Run("UnchangedDisguiseDescriptor");
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PlayerName");
    }

    private object[][] Run(string method) => (object[][])_harness.GetMethod(method)!.Invoke(null, null)!;

    private string GroupOf(object[] record) => (string)_harness.GetMethod("GroupOf")!.Invoke(null, [record])!;

    private static object Field(object[] record, string name)
    {
        var names = Regex.Matches((string)record[0], @"\{(?<name>[^}]+)\}")
            .Select(match => match.Groups["name"].Value).ToArray();
        names.Length.Should().Be(record.Length - 1, "every audit property must have a value");
        var index = Array.IndexOf(names, name);
        index.Should().BeGreaterThanOrEqualTo(0, $"the audit must retain {name}");
        return record[index + 1];
    }

    private static SyntaxNode ReadSource(string path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", path))).GetRoot();
    }
}
