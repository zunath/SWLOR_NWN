using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerIdentityAuditTests
{
    private MethodInfo _run = null!;
    private MethodInfo _getAuditName = null!;
    private MethodInfo _runConnection = null!;
    private MethodInfo _runNameAction = null!;

    [OneTimeSetUp]
    public void CompileAuditHarness()
    {
        var getAuditName = ExtractMethod("Service", "PlayerName.cs", "GetAuditName");
        var connect = ExtractMethod("Feature", "Auditing.cs", "AuditClientConnection");
        var disconnect = ExtractMethod("Feature", "Auditing.cs", "AuditClientDisconnection");
        var chat = ExtractMethod("Feature", "Auditing.cs", "AuditChatMessages");
        var setNameAction = ExtractAction("SetKnownName");
        var forgetNameAction = ExtractAction("ForgetKnownName");
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using static AuditWorld;

            public static class AuditWorld
            {
                public static readonly Dictionary<uint, (string Name, string Descriptor, bool IsPc, bool IsDm, bool Valid)> Players = new();
                public static readonly Dictionary<(uint Observer, uint Target), string> KnownNames = new();
                public static string ValidationError = "";
                public static uint EnteringObject, ExitingObject;
                public static string GetName(uint player) => Players.TryGetValue(player, out var row) ? row.Name : "invalid";
                public static bool GetIsObjectValid(uint player) => Players.TryGetValue(player, out var row) && row.Valid;
                public static bool GetIsPC(uint player) => Players.TryGetValue(player, out var row) && row.IsPc;
                public static bool GetIsDM(uint player) => Players.TryGetValue(player, out var row) && row.IsDm;
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
            }
            public static class PlayerDescriptor
            {
                public static string BaseDescriptor = "Old Base Descriptor";
                public static void SetUnknownDisplayName(uint player, string name)
                {
                    BaseDescriptor = name;
                    var row = AuditWorld.Players[player];
                    AuditWorld.Players[player] = (row.Name, "Current Disguise Descriptor", row.IsPc, row.IsDm, row.Valid);
                }
                public static string GetUnknownDisplayName(uint player) => BaseDescriptor;
            }
            public static class Disguise
            {
                public static string GetIdentityKey(uint player) => $"identity-{player}";
                public static string GetDisplayDescriptor(uint player) => AuditWorld.Players[player].Descriptor;
            }

            public enum ChatChannel { ServerMessage, DMTell, PlayerTell, Talk, Party }
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
            public enum LogGroup { Connection, Chat, PlayerName }
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

            public static class Auditing
            {
                {{connect}}
                {{disconnect}}
                {{chat}}
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
                public static object[][] RunNameAction(bool forget, uint user, uint target, string[] args)
                    => NameActionHarness.Run(forget ? NameActionHarness.ForgetName : NameActionHarness.SetName, user, target, args);
                public static void SetKnownName(uint observer, uint target, string name)
                    => AuditWorld.KnownNames[(observer, target)] = name;
                public static void SetValidationError(string error) => AuditWorld.ValidationError = error;
                public static void ResetNameState()
                {
                    AuditWorld.KnownNames.Clear();
                    AuditWorld.ValidationError = "";
                    PlayerDescriptor.BaseDescriptor = "Old Base Descriptor";
                    AuditWorld.Players[1] = ("Canonical Speaker", "A Seedy Individual", true, false, true);
                    AuditWorld.Players[2] = ("Canonical Listener", "A Quiet Twi'lek", true, false, true);
                }
                public static void SetPlayer(uint id, string name, string descriptor, bool isPc, bool isDm, bool valid = true)
                    => AuditWorld.Players[id] = (name, descriptor, isPc, isDm, valid);
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

    [Test]
    public void ChatAudit_RetainsCanonicalCredentialsAndAddsSpeakerDescriptor()
    {
        var output = RunChat(1, 0, "Talk", "I saw A Seedy Individual near the docks.");

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("account-1")
            .And.Contain("cdk-1")
            .And.Contain("ip-1")
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

    [Test]
    public void TellAudit_RecordsCurrentDescriptorsForBothCanonicalParticipants()
    {
        var output = RunChat(1, 2, "PlayerTell", "Are you the one I met?");

        output.Should().ContainSingle();
        output[0].Should().Contain("Canonical Speaker [A Seedy Individual]")
            .And.Contain("Canonical Listener [A Quiet Twi'lek]")
            .And.Contain("account-1")
            .And.Contain("account-2")
            .And.Contain("cdk-1")
            .And.Contain("cdk-2");
    }

    [Test]
    public void SelfNameChange_LogsPreviousBaseDescriptorAndNewValueAfterMutation()
    {
        ResetNameState();
        var logs = RunNameAction(forget: false, user: 1, target: 1, args: ["New description"]);

        logs.Should().ContainSingle();
        AssertStructuredField(logs[0], "Action", "unknown-name-set");
        AssertStructuredField(logs[0], "ObserverPlayerId", "1");
        AssertStructuredField(logs[0], "ObserverName", "Canonical Speaker [Current Disguise Descriptor]");
        AssertStructuredField(logs[0], "TargetPlayerId", "1");
        AssertStructuredField(logs[0], "TargetName", "Canonical Speaker [Current Disguise Descriptor]");
        AssertStructuredField(logs[0], "IdentityKey", "1");
        AssertStructuredField(logs[0], "PreviousName", "Old Base Descriptor");
        AssertStructuredField(logs[0], "Name", "New description");
    }

    [Test]
    public void PrivateNameActions_LogPreviousAndNewLabelsAgainstCurrentIdentity()
    {
        ResetNameState();
        SetKnownName(1, 2, "Old private label");
        var setLogs = RunNameAction(forget: false, user: 1, target: 2, args: ["New private label"]);
        setLogs.Should().ContainSingle();
        AssertStructuredField(setLogs[0], "Action", "name-set");
        AssertStructuredField(setLogs[0], "ObserverName", "Canonical Speaker [A Seedy Individual]");
        AssertStructuredField(setLogs[0], "TargetName", "Canonical Listener [A Quiet Twi'lek]");
        AssertStructuredField(setLogs[0], "IdentityKey", "identity-2");
        AssertStructuredField(setLogs[0], "PreviousName", "Old private label");
        AssertStructuredField(setLogs[0], "Name", "New private label");

        var forgetLogs = RunNameAction(forget: true, user: 1, target: 2, args: []);
        forgetLogs.Should().ContainSingle();
        AssertStructuredField(forgetLogs[0], "Action", "name-forget");
        AssertStructuredField(forgetLogs[0], "IdentityKey", "identity-2");
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
