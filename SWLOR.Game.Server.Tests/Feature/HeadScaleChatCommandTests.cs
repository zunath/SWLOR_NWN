using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class HeadScaleChatCommandTests
{
    private MethodInfo _execute = null!;

    [OneTimeSetUp]
    public void CompileCommandWithRecordingEngineAndDatabase()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName,
            "SWLOR.Game.Server", "Feature", "ChatCommandDefinition", "CharacterChatCommand.cs"));
        var method = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.Text == "HeadScale");
        var harness = $$"""
            using System;
            using System.Globalization;
            using SWLOR.Game.Server.Enumeration;
            using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
            using SWLOR.Game.Server.Service.ChatCommandService;
            using SWLOR.NWN.API.NWScript.Enum;
            using Player = SWLOR.Game.Server.Entity.Player;
            using static World;
            public static class World
            {
                public static bool DM, Possessed, Supported;
                public static float Scale;
                public static string Message;
                public static bool GetIsDM(uint user) => DM;
                public static bool GetIsDMPossessed(uint user) => Possessed;
                public static AppearanceType GetAppearanceType(uint user) => Supported
                    ? AppearanceType.Human : (AppearanceType)(-1);
                public static string GetObjectUUID(uint user) => "test-player";
                public static void SendMessageToPC(uint user, string message) => Message = message;
            }
            public static class DB
            {
                public static Player Record;
                public static int Reads, Writes;
                public static T Get<T>(string id) where T : class
                {
                    if (DM || Possessed) throw new Exception("DM command accessed player data");
                    Reads++;
                    return Record as T;
                }
                public static void Set(Player player)
                {
                    if (DM || Possessed) throw new Exception("DM command saved player data");
                    Writes++;
                }
            }
            public static class HelmetModelRenderer
            {
                public static float GetHeadScale(uint user) => Scale;
                public static void SetHeadScale(uint user, float scale) => Scale = scale;
            }
            public class Harness
            {
                private readonly ChatCommandBuilder _builder = new();
                {{method.ToFullString()}}
                public static (string Error, float Head, float Saved, int Reads, int Writes, string Message)
                    Execute(bool dm, bool possessed, bool supported, string[] args, float live, float saved)
                {
                    DM = dm; Possessed = possessed; Supported = supported; Scale = live; Message = "";
                    DB.Record = new Player { HeadAppearanceScale = saved };
                    DB.Reads = 0; DB.Writes = 0;
                    var harness = new Harness();
                    harness.HeadScale();
                    var command = harness._builder.Build()["headscale"];
                    var error = command.ValidateArguments(1, args);
                    if (string.IsNullOrEmpty(error)) command.DoAction(1, 0, null, args);
                    return (error, Scale, DB.Record.HeadAppearanceScale, DB.Reads, DB.Writes, Message);
                }
            }
            """;
        // The real command builder and racial definitions remain in use; only native calls
        // and Redis are replaced, allowing DM and possession paths to run without a client.
        var referencePaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Concat(new[] { typeof(CharacterChatCommand).Assembly.Location,
                typeof(SWLOR.NWN.API.NWScript.Enum.AppearanceType).Assembly.Location });
        var references = referencePaths.Distinct().Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("HeadScaleChatCommandHarness",
            [CSharpSyntaxTree.ParseText(harness)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _execute = Assembly.Load(stream.ToArray()).GetType("Harness")!.GetMethod("Execute")!;
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void DMAndPossessedCreature_UseCurrentHeadScaleWithoutPlayerData(bool dm, bool possessed)
    {
        var result = Execute(dm, possessed, ["+"], live: 1.07f);
        result.Error.Should().BeEmpty();
        result.Head.Should().BeApproximately(1.08f, 0.0001f);
        result.Reads.Should().Be(0);
        result.Writes.Should().Be(0);
        result.Saved.Should().Be(1f);
    }

    [Test]
    public void DM_QueryReportsCurrentCreatureScale()
    {
        var result = Execute(true, false, [], live: 1.07f);
        result.Message.Should().Contain("1.07");
        result.Head.Should().Be(1.07f);
        result.Reads.Should().Be(0);
        result.Writes.Should().Be(0);
    }

    [TestCase("NaN")]
    [TestCase("Infinity")]
    [TestCase("invalid")]
    [TestCase("0.84")]
    [TestCase("1.16")]
    public void PossessedCreature_StillRejectsInvalidOrOutOfRangeValues(string value)
    {
        var result = Execute(false, true, [value]);
        result.Error.Should().NotBeEmpty();
        result.Head.Should().Be(1f);
        result.Writes.Should().Be(0);
    }

    [Test]
    public void PossessedCreature_UnsupportedAppearanceRemainsRejected()
    {
        Execute(false, true, ["1.04"], supported: false).Error
            .Should().Be("Your appearance type does not support head scaling.");
    }

    [TestCase("+", 1.15f)]
    [TestCase("-", 0.85f)]
    public void PossessedCreature_IncrementStopsAtRacialLimit(string argument, float live)
    {
        var result = Execute(false, true, [argument], live);
        result.Error.Should().BeEmpty();
        result.Head.Should().Be(live);
        result.Message.Should().Contain("any further");
    }

    [Test]
    public void Player_StillIncrementsAndPersistsSavedHeadScale()
    {
        var result = Execute(false, false, ["+"], live: 1f, saved: 1.07f);
        result.Error.Should().BeEmpty();
        result.Head.Should().BeApproximately(1.08f, 0.0001f);
        result.Saved.Should().BeApproximately(1.08f, 0.0001f);
        result.Reads.Should().Be(1);
        result.Writes.Should().Be(1);
    }

    private (string Error, float Head, float Saved, int Reads, int Writes, string Message) Execute(
        bool dm, bool possessed, string[] args, float live = 1f, float saved = 1f, bool supported = true)
    {
        return ((string, float, float, int, int, string))_execute.Invoke(null,
            [dm, possessed, supported, args, live, saved])!;
    }
}
