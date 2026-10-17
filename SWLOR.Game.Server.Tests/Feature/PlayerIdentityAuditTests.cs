using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerIdentityAuditTests
{
    private MethodInfo _run = null!;
    private MethodInfo _getAuditName = null!;
    private MethodInfo _runConnection = null!;
    private MethodInfo _runNameAction = null!;
    private MethodInfo _runHoloNet = null!;
    private MethodInfo _runIntroduction = null!;

    private static IEnumerable<ChatChannel> RegularChatChannels => Enum.GetValues<ChatChannel>()
        .Where(channel => channel != ChatChannel.ServerMessage &&
                          channel != ChatChannel.PlayerTell && channel != ChatChannel.DMTell);

    [OneTimeSetUp]
    public void CompileAuditHarness()
    {
        var getAuditName = ExtractMethod("Service", "PlayerName.cs", "GetAuditName");
        var getAuditIdentity = ExtractMethod("Service", "PlayerName.cs", "GetAuditIdentity");
        var connect = ExtractMethod("Feature", "Auditing.cs", "AuditClientConnection");
        var disconnect = ExtractMethod("Feature", "Auditing.cs", "AuditClientDisconnection");
        var chat = ExtractMethod("Feature", "Auditing.cs", "AuditChatMessages");
        var holoNet = CSharpSyntaxTree.ParseText(ExtractMethod("Feature/GuiDefinition/ViewModel",
            "HoloNetViewModel.cs", "SubmitBroadcastAsync")).GetRoot();
        var holoNetAuthor = holoNet.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .Single(statement => statement.Declaration.Variables.Any(variable => variable.Identifier.ValueText == "auditAuthorName"));
        var holoNetLog = holoNet.DescendantNodes().OfType<ExpressionStatementSyntax>()
            .Single(statement => statement.Expression is InvocationExpressionSyntax call &&
                                 call.Expression.ToString() == "Log.Write" &&
                                 call.ArgumentList.Arguments[0].ToString() == "LogGroup.Chat");
        var setNameAction = ExtractAction("SetKnownName");
        var forgetNameAction = ExtractAction("ForgetKnownName");
        var acceptIntroduction = ExtractMethod("Service", "PlayerIntroduction.cs", "Accept");
        var setDescriptor = ExtractMethod("Service", "PlayerDescriptor.cs", "SetUnknownDisplayName");
        var getIdentityKey = ExtractMethod("Service", "Disguise.cs", "GetIdentityKey");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using static AuditWorld;

            public static class AuditWorld
            {
                public static readonly Dictionary<uint, (string Name, string Descriptor, bool IsPc, bool IsDm, bool Valid)> Players = new();
                public static readonly Dictionary<(uint Observer, uint Target), string> KnownNames = new();
                public static readonly HashSet<uint> DmPossessed = new();
                public static string ValidationError = "";
                public static uint EnteringObject, ExitingObject;
                public static string GetName(uint player) => Players.TryGetValue(player, out var row) ? row.Name : "invalid";
                public static bool GetIsObjectValid(uint player) => Players.TryGetValue(player, out var row) && row.Valid;
                public static bool GetIsPC(uint player) => Players.TryGetValue(player, out var row) && row.IsPc;
                public static bool GetIsDM(uint player) => Players.TryGetValue(player, out var row) && row.IsDm;
                public static bool GetIsDMPossessed(uint player) => DmPossessed.Contains(player);
                public static uint GetEnteringObject() => EnteringObject;
                public static uint GetExitingObject() => ExitingObject;
                public static string GetPCIPAddress(uint player) => $"ip-{player}";
                public static string GetPCPublicCDKey(uint player) => $"cdk-{player}";
                public static string GetPCPlayerName(uint player) => $"account-{player}";
                public static string GetObjectUUID(uint player) => player.ToString();
                public static int GetLocalInt(uint player, string variable) => 0;
                public static void SendMessageToPC(uint player, string message) { }
            }

            public static class PlayerName
            {
                {{getAuditName}}
                {{getAuditIdentity}}
                public static string InputError => AuditWorld.ValidationError;
                public static string ValidateKnownNameInput(string name) => InputError;
                public static string ValidateKnownNameAssignment(uint observer, uint target, string name) => InputError;
                public static string SanitizeKnownName(string name) => name.Trim();
                public static bool TryGetKnownName(uint observer, uint target, out string name)
                    => AuditWorld.KnownNames.TryGetValue((observer, target), out name);
                public static void SetKnownName(uint observer, uint target, string name)
                    => AuditWorld.KnownNames[(observer, target)] = name;
                public static void ForgetKnownName(uint observer, uint target)
                    => AuditWorld.KnownNames.Remove((observer, target));
                public static void RefreshNameOverridesForPlayer(uint player) { }
            }
            public sealed class Player
            {
                public string Id, UnknownDisplayName;
            }
            public static class DB
            {
                public static readonly Dictionary<string, Player> Players = new();
                public static T Get<T>(string id) where T : class => Players.GetValueOrDefault(id) as T;
                public static void Set(Player player) => Players[player.Id] = player;
            }
            public static class PlayerDescriptor
            {
                {{setDescriptor}}
                public static string GetUnknownDisplayName(uint player) => DB.Get<Player>(GetObjectUUID(player)).UnknownDisplayName;
            }
            public static class Disguise
            {
                public static readonly Dictionary<uint, PlayerDisguise> Active = new();
                {{getIdentityKey}}
                public static string GetDisplayDescriptor(uint player) => AuditWorld.Players[player].Descriptor;
                public static PlayerDisguise GetActiveDisguise(uint player) => Active.GetValueOrDefault(player);
                public static string BuildIdentityKey(string disguiseId) => $"disguise:{disguiseId}";
            }
            public sealed class PlayerDisguise { public string Id, PrivateName; }

            public enum ChatChannel { {{string.Join(", ", Enum.GetNames<ChatChannel>())}} }
            [AttributeUsage(AttributeTargets.Method)] public sealed class NWNEventHandlerAttribute : Attribute
            {
                public NWNEventHandlerAttribute(string eventName) { }
            }
            public static class ScriptName
            {
                public const string OnModuleEnter = "enter";
                public const string OnModuleExit = "exit";
                public const string OnNWNXChat = "chat";
            }
            public enum LogGroup { Connection, Chat, PlayerName, PrivateNames, PublicDescriptors }
            public static class Log
            {
                public static readonly List<string> Messages = new();
                public static readonly List<object[]> StructuredMessages = new();
                public static void Write(LogGroup group, string message, bool immediate = false) => Messages.Add($"{group}:{message}");
                public static void WriteStructured(LogGroup group, string message, params object[] values)
                {
                    var record = new List<object> { group, message };
                    record.AddRange(values);
                    StructuredMessages.Add(record.ToArray());
                }
            }
            public static class ColorToken
            {
                public static string Red(string value) => value;
                public static string Green(string value) => value;
            }
            public static class ChatPlugin
            {
                public static uint Sender, Target;
                public static ChatChannel Channel;
                public static string Message = "";
                public static uint GetSender() => Sender;
                public static uint GetTarget() => Target;
                public static ChatChannel GetChannel() => Channel;
                public static string GetMessage() => Message;
            }
            public static class Communication { public const string SuppressChatAuditVariable = "suppress"; }
            public static class DMPresence
            {
                public static readonly Dictionary<uint, uint> Masters = new();
                public static uint GetDM(uint actor)
                    => AuditWorld.GetIsDMPossessed(actor) && Masters.TryGetValue(actor, out var dm) ? dm : actor;
            }

            public static class Auditing
            {
                {{connect}}
                {{disconnect}}
                {{chat}}
            }

            public sealed class IntroductionOffer
            {
                public uint Presenter = 2;
                public string PresenterPlayerId = "2", IdentityKey = "2", Name = "Introduced label";
                public string ValidateIdentityAndConsent(string playerId, string identityKey, string currentName, string approvedName) => "";
            }
            public sealed class IntroductionInbox
            {
                public string Accept(string observerId, Guid offerId, DateTime now,
                    Func<IntroductionOffer, string> validate, Action<IntroductionOffer> remember)
                {
                    var offer = new IntroductionOffer();
                    var error = validate(offer);
                    if (string.IsNullOrEmpty(error)) remember(offer);
                    return error;
                }
            }
            public static class PlayerIntroduction
            {
                private static readonly IntroductionInbox Inbox = new();
                private static bool IsCurrentIdentity(IntroductionOffer offer) => true;
                {{acceptIntroduction}}
            }

            public static class NameActionHarness
            {
                public static readonly Action<uint, uint, object, string[]> SetName = {{setNameAction}};
                public static readonly Action<uint, uint, object, string[]> ForgetName = {{forgetNameAction}};
                public static object[][] Run(Action<uint, uint, object, string[]> action, uint user, uint target, string[] args)
                {
                    Log.StructuredMessages.Clear();
                    action(user, target, null, args);
                    return Log.StructuredMessages.ToArray();
                }
            }

            public static class AuditHarness
            {
                public static string GetAuditName(uint player) => PlayerName.GetAuditName(player);
                public static string[] RunChat(uint sender, uint target, ChatChannel channel, string message)
                {
                    Log.Messages.Clear();
                    ChatPlugin.Sender = sender;
                    ChatPlugin.Target = target;
                    ChatPlugin.Channel = channel;
                    ChatPlugin.Message = message;
                    Auditing.AuditChatMessages();
                    return Log.Messages.ToArray();
                }
                public static string[] RunConnection(uint player, bool connected)
                {
                    Log.Messages.Clear();
                    AuditWorld.EnteringObject = player;
                    AuditWorld.ExitingObject = player;
                    if (connected) Auditing.AuditClientConnection();
                    else Auditing.AuditClientDisconnection();
                    return Log.Messages.ToArray();
                }
                public static string[] RunHoloNet(uint player, string message)
                {
                    Log.Messages.Clear();
                    {{holoNetAuthor}}
                    {{holoNetLog}}
                    return Log.Messages.ToArray();
                }
                public static object[][] RunNameAction(bool forget, uint user, uint target, string[] args)
                    => NameActionHarness.Run(forget ? NameActionHarness.ForgetName : NameActionHarness.SetName, user, target, args);
                public static object[][] RunIntroduction()
                {
                    Log.StructuredMessages.Clear();
                    PlayerIntroduction.Accept(1, Guid.NewGuid(), "Introduced label");
                    return Log.StructuredMessages.ToArray();
                }
                public static void SetKnownName(uint observer, uint target, string name)
                    => AuditWorld.KnownNames[(observer, target)] = name;
                public static void SetValidationError(string error) => AuditWorld.ValidationError = error;
                public static void ResetNameState()
                {
                    AuditWorld.KnownNames.Clear();
                    AuditWorld.ValidationError = "";
                    AuditWorld.DmPossessed.Clear();
                    DMPresence.Masters.Clear();
                    Disguise.Active.Clear();
                    DB.Players.Clear();
                    DB.Set(new Player { Id = "1", UnknownDisplayName = "Old Base Descriptor" });
                    AuditWorld.Players[1] = ("Canonical Speaker", "A Seedy Individual", true, false, true);
                    AuditWorld.Players[2] = ("Canonical Listener", "A Quiet Twi'lek", true, false, true);
                }
                public static void SetPlayer(uint id, string name, string descriptor, bool isPc, bool isDm, bool valid = true)
                    => AuditWorld.Players[id] = (name, descriptor, isPc, isDm, valid);
                public static void SetDescriptor(uint id, string descriptor)
                {
                    var row = AuditWorld.Players[id];
                    AuditWorld.Players[id] = (row.Name, descriptor, row.IsPc, row.IsDm, row.Valid);
                }
                public static void SetDMPossessed(uint id) => AuditWorld.DmPossessed.Add(id);
                public static void SetPossessedBy(uint creature, uint dm)
                {
                    AuditWorld.DmPossessed.Add(creature);
                    DMPresence.Masters[creature] = dm;
                }
                public static void SetDisguise(uint player, string id, string name)
                    => Disguise.Active[player] = new PlayerDisguise { Id = id, PrivateName = name };
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("PlayerIdentityAuditHarness",
            [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _run = assembly.GetType("AuditHarness")!.GetMethod("RunChat")!;
        _getAuditName = assembly.GetType("AuditHarness")!.GetMethod("GetAuditName")!;
        _runConnection = assembly.GetType("AuditHarness")!.GetMethod("RunConnection")!;
        _runNameAction = assembly.GetType("AuditHarness")!.GetMethod("RunNameAction")!;
        _runHoloNet = assembly.GetType("AuditHarness")!.GetMethod("RunHoloNet")!;
        _runIntroduction = assembly.GetType("AuditHarness")!.GetMethod("RunIntroduction")!;
        assembly.GetType("AuditHarness")!.GetMethod("SetPlayer")!.Invoke(null,
            [1u, "Canonical Speaker", "A Seedy Individual", true, false, true]);
        assembly.GetType("AuditHarness")!.GetMethod("SetPlayer")!.Invoke(null,
            [2u, "Canonical Listener", "A Quiet Twi'lek", true, false, true]);
        assembly.GetType("AuditHarness")!.GetMethod("SetPlayer")!.Invoke(null,
            [3u, "Ordinary NPC", "", false, false, true]);
        assembly.GetType("AuditHarness")!.GetMethod("SetPlayer")!.Invoke(null,
            [4u, "Staff Character", "", true, true, true]);
    }

    [Test]
    public void AuditName_UsesCanonicalNameAndCurrentDisguiseDescriptorForPlayers()
    {
        ((string)_getAuditName.Invoke(null, [1u])!).Should().Be("Canonical Speaker [A Seedy Individual]");
        ((string)_getAuditName.Invoke(null, [3u])!).Should().Be("Ordinary NPC");
        ((string)_getAuditName.Invoke(null, [4u])!).Should().Be("Staff Character");
        ((string)_getAuditName.Invoke(null, [99u])!).Should().Be("invalid");
    }

    [SetUp]
    public void ResetMutableHarnessState()
    {
        _runNameAction.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("ResetNameState")!
            .Invoke(null, null);
    }

    [TestCaseSource(nameof(RegularChatChannels))]
    public void ChatAudit_RetainsCanonicalCredentialsAndAddsSpeakerDescriptor(ChatChannel channel)
    {
        SetDisguise(1, "speaker-mask", "Smuggler");
        var output = RunChat(1, 0, channel.ToString(), "I saw A Seedy Individual near the docks.");

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("account-1")
            .And.Contain("cdk-1")
            .And.Contain("ip-1")
            .And.Contain("PlayerId=1 IdentityKey=disguise:speaker-mask Disguised=True DisguiseId=speaker-mask DisguiseName=Smuggler")
            .And.Contain($" - {channel}:")
            .And.Contain("I saw A Seedy Individual near the docks.");
    }

    [TestCase(true, "Connected to server")]
    [TestCase(false, "Disconnected from server")]
    public void ConnectionAudits_RetainCanonicalNameAndAddDescriptor(bool connected, string expectedText)
    {
        var output = (string[])_runConnection.Invoke(null, [1u, connected])!;

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("account-1")
            .And.Contain("cdk-1")
            .And.Contain("ip-1")
            .And.Contain(expectedText);
    }

    [TestCase(ChatChannel.PlayerTell)]
    [TestCase(ChatChannel.DMTell)]
    public void TellAudit_RecordsCurrentDescriptorsForBothCanonicalParticipants(ChatChannel channel)
    {
        SetDisguise(1, "speaker-mask", "Smuggler");
        SetDisguise(2, "listener-mask", "Merchant");
        var output = RunChat(1, 2, channel.ToString(), "Are you the one I met?");

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("Canonical Listener [A Quiet Twi'lek]")
            .And.Contain("account-1")
            .And.Contain("account-2")
            .And.Contain("cdk-1")
            .And.Contain("cdk-2")
            .And.Contain("ip-1")
            .And.Contain("ip-2")
            .And.Contain("PlayerId=1 IdentityKey=disguise:speaker-mask Disguised=True DisguiseId=speaker-mask DisguiseName=Smuggler")
            .And.Contain("PlayerId=2 IdentityKey=disguise:listener-mask Disguised=True DisguiseId=listener-mask DisguiseName=Merchant")
            .And.Contain($" - {channel} (SENT TO ")
            .And.Contain("Are you the one I met?");
    }

    [Test]
    public void TellAudit_RecordsThePossessingDMRatherThanTheCreature()
    {
        var harness = _run.DeclaringType!.Assembly.GetType("AuditHarness")!;
        harness.GetMethod("SetPlayer")!.Invoke(null, [6u, "Staff Listener", "", false, true, true]);
        harness.GetMethod("SetPlayer")!.Invoke(null, [7u, "Possessed NPC", "", true, false, true]);
        harness.GetMethod("SetPossessedBy")!.Invoke(null, [7u, 6u]);

        var output = RunChat(1, 7, "DMTell", "Staff check in");

        output.Should().ContainSingle();
        output[0].Should().Contain("(SENT TO Staff Listener")
            .And.NotContain("Possessed NPC")
            .And.Contain("account-7")
            .And.Contain("cdk-7")
            .And.Contain("ip-7");
    }

    [Test]
    public void ChatAudit_UsesCurrentDescriptorsAfterIdentityChangesInsteadOfPrivateLabels()
    {
        SetKnownName(1, 2, "Private listener label");
        SetKnownName(2, 1, "Private speaker label");
        RunChat(1, 2, "PlayerTell", "Before changing disguises").Single()
            .Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("Canonical Listener [A Quiet Twi'lek]");

        SetDescriptor(1, "A Masked Human");
        SetDescriptor(2, "A Hooded Traveler");
        var output = RunChat(1, 2, "PlayerTell", "After changing disguises");

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Masked Human]")
            .And.Contain("Canonical Listener [A Hooded Traveler]")
            .And.NotContain("A Seedy Individual")
            .And.NotContain("A Quiet Twi'lek")
            .And.NotContain("Private listener label")
            .And.NotContain("Private speaker label");
    }

    [Test]
    public void HoloNetAudit_RetainsCanonicalAuthorAndCurrentDescriptor()
    {
        SetDescriptor(1, "A Masked Human");
        SetDisguise(1, "broadcast-mask", "Broadcaster");
        var output = (string[])_runHoloNet.Invoke(null, [1u, "Looking for a mechanic."])!;

        output.Should().ContainSingle();
        output[0].Should().Contain("Chat:Canonical Speaker [A Masked Human]")
            .And.Contain("PlayerId=1 IdentityKey=disguise:broadcast-mask Disguised=True DisguiseId=broadcast-mask DisguiseName=Broadcaster")
            .And.EndWith("(account-1) [cdk-1] submitted HoloNet broadcast: Looking for a mechanic.");
    }

    [Test]
    public void ChatAudit_DistinguishesDisguisesThatShareADescriptor()
    {
        SetDisguise(1, "first-mask", "First disguise");
        var first = RunChat(1, 0, "PlayerTalk", "First message").Single();
        SetDisguise(1, "second-mask", "Second disguise");
        var second = RunChat(1, 0, "PlayerTalk", "Second message").Single();

        first.Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("IdentityKey=disguise:first-mask");
        second.Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("IdentityKey=disguise:second-mask").And.NotContain("first-mask");
    }

    [Test]
    public void ChatAudit_RecordsCommandsWithTheCurrentDisguiseIdentity()
    {
        SetDisguise(1, "command-mask", "Command disguise");
        RunChat(1, 0, "PlayerTalk", "/name A hooded traveler").Single().Should()
            .Contain("DisguiseId=command-mask").And.Contain("PlayerId=1")
            .And.EndWith("PlayerTalk: /name A hooded traveler");
    }

    [Test]
    public void ChatAudit_IdentifiesUndisguisedPlayersExplicitly()
    {
        RunChat(1, 0, "PlayerTalk", "No disguise").Single().Should()
            .Contain("PlayerId=1 IdentityKey=1 Disguised=False DisguiseId=none DisguiseName=none");
    }

    [Test]
    public void ChatAudit_PreservesPossessedNpcNameWithoutAddingPlayerDescriptor()
    {
        _run.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetPlayer")!
            .Invoke(null, [5u, "Renamed NPC", "Not a player descriptor", true, false, true]);
        _run.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetDMPossessed")!
            .Invoke(null, [5u]);

        RunChat(5, 0, "DMTalk", "NPC dialogue").Single().Should().Contain("Chat:Renamed NPC - ")
            .And.NotContain("Not a player descriptor");
    }

    [Test]
    public void ChatAudit_DoesNotLogServerMessages()
    {
        RunChat(1, 0, "ServerMessage", "Server notification").Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SelfNameChange_WritesOneBaseDescriptorEventWithSeparateActiveIdentity(bool disguised)
    {
        ResetNameState();
        if (disguised)
            SetDisguise(1, "self-mask", "Self disguise");
        var logs = RunNameAction(forget: false, user: 1, target: 1, args: ["New description"]);

        logs.Should().ContainSingle();
        logs[0][0].ToString().Should().Be(nameof(LogGroup.PublicDescriptors));
        logs[0][1].ToString().Should().StartWith("Base descriptor changed:");
        AssertStructuredField(logs[0], "PlayerId", "1");
        AssertStructuredField(logs[0], "PlayerName", "Canonical Speaker");
        AssertStructuredField(logs[0], "IdentityKey", "1");
        AssertStructuredField(logs[0], "ActiveIdentityKey", disguised ? "disguise:self-mask" : "1");
        AssertStructuredField(logs[0], "PreviousDescriptor", "Old Base Descriptor");
        AssertStructuredField(logs[0], "Descriptor", "New description");
    }

    [Test]
    public void PrivateNameActions_LogPreviousAndNewLabelsAgainstCurrentIdentity()
    {
        ResetNameState();
        SetKnownName(1, 2, "Old private label");
        var setLogs = RunNameAction(forget: false, user: 1, target: 2, args: ["New private label"]);
        setLogs.Should().ContainSingle();
        setLogs[0][0].ToString().Should().Be(nameof(LogGroup.PrivateNames));
        AssertStructuredField(setLogs[0], "Action", "name-set");
        AssertStructuredField(setLogs[0], "ObserverName", "Canonical Speaker [A Seedy Individual]");
        AssertStructuredField(setLogs[0], "TargetName", "Canonical Listener [A Quiet Twi'lek]");
        AssertStructuredField(setLogs[0], "IdentityKey", "2");
        AssertStructuredField(setLogs[0], "PreviousName", "Old private label");
        AssertStructuredField(setLogs[0], "Name", "New private label");

        var forgetLogs = RunNameAction(forget: true, user: 1, target: 2, args: []);
        forgetLogs.Should().ContainSingle();
        forgetLogs[0][0].ToString().Should().Be(nameof(LogGroup.PrivateNames));
        AssertStructuredField(forgetLogs[0], "Action", "name-forget");
        AssertStructuredField(forgetLogs[0], "IdentityKey", "2");
        AssertStructuredField(forgetLogs[0], "PreviousName", "New private label");
        AssertStructuredField(forgetLogs[0], "Name", string.Empty);
    }

    [Test]
    public void InvalidNameInput_DoesNotCreateAuditRecord()
    {
        ResetNameState();
        SetValidationError("Invalid");

        RunNameAction(forget: false, user: 1, target: 2, args: ["bad"]).Should().BeEmpty();
    }

    [Test]
    public void RememberedIntroduction_WritesOnlyThePrivateNameLog()
    {
        SetKnownName(1, 2, "Previous label");
        var logs = (object[][])_runIntroduction.Invoke(null, null)!;

        logs.Should().ContainSingle();
        logs[0][0].ToString().Should().Be(nameof(LogGroup.PrivateNames));
        AssertStructuredField(logs[0], "Action", "introduction-accepted");
        AssertStructuredField(logs[0], "ObserverPlayerId", "1");
        AssertStructuredField(logs[0], "TargetPlayerId", "2");
        AssertStructuredField(logs[0], "IdentityKey", "2");
        AssertStructuredField(logs[0], "PreviousName", "Previous label");
        AssertStructuredField(logs[0], "Name", "Introduced label");
    }

    [TestCase(LogGroup.PrivateNames, "PrivateNames")]
    [TestCase(LogGroup.PublicDescriptors, "PublicDescriptors")]
    public void DedicatedIdentityLogs_HaveSeparateDestinationsInEveryEnvironment(LogGroup group, string loggerName)
    {
        var settings = typeof(LogGroup).GetField(group.ToString())!.GetCustomAttribute<LogGroupAttribute>()!;
        settings.LoggerName.Should().Be(loggerName).And.NotBe(nameof(LogGroup.PlayerName));
        settings.Environment.Should().Be(ServerEnvironmentType.All);
    }

    private string[] RunChat(uint sender, uint target, string channelName, string message)
    {
        var channelType = _run.DeclaringType!.Assembly.GetType("ChatChannel")!;
        var parsedChannel = Enum.Parse(channelType, channelName);
        return (string[])_run.Invoke(null, [sender, target, parsedChannel, message])!;
    }

    private object[][] RunNameAction(bool forget, uint user, uint target, string[] args)
        => (object[][])_runNameAction.Invoke(null, [forget, user, target, args])!;

    private void SetKnownName(uint observer, uint target, string name)
        => _runNameAction.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetKnownName")!
            .Invoke(null, [observer, target, name]);

    private void SetDescriptor(uint player, string descriptor)
        => _run.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetDescriptor")!
            .Invoke(null, [player, descriptor]);

    private void SetDisguise(uint player, string disguiseId, string privateName)
        => _run.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetDisguise")!
            .Invoke(null, [player, disguiseId, privateName]);

    private void ResetNameState()
        => _runNameAction.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("ResetNameState")!.Invoke(null, null);

    private void SetValidationError(string error)
        => _runNameAction.DeclaringType!.Assembly.GetType("AuditHarness")!.GetMethod("SetValidationError")!
            .Invoke(null, [error]);

    private static string ExtractMethod(string area, string fileName, string methodName)
    {
        var root = FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", area, fileName));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(node => node.Identifier.ValueText == methodName);
        return method.WithAttributeLists(default).WithoutTrivia().ToFullString();
    }

    private static string ExtractAction(string methodName)
    {
        var root = FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server", "Feature",
            "ChatCommandDefinition", "CharacterChatCommand.cs"));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.ValueText == methodName);
        var action = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(invocation => invocation.Expression is MemberAccessExpressionSyntax member &&
                                  member.Name.Identifier.ValueText == "Action")
            .ArgumentList.Arguments.Single().Expression;
        return action.WithoutTrivia().ToFullString();
    }

    private static void AssertStructuredField(object[] record, string fieldName, string expectedValue)
    {
        var template = (string)record[1]!;
        var fields = Regex.Matches(template, @"\{(?<name>[^}]+)\}")
            .Cast<Match>()
            .Select(match => match.Groups["name"].Value)
            .ToArray();
        var fieldIndex = Array.IndexOf(fields, fieldName);
        fieldIndex.Should().BeGreaterThanOrEqualTo(0, $"the structured template should contain {fieldName}");
        record[fieldIndex + 2].Should().Be(expectedValue, $"the {fieldName} field should contain its expected value");
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
