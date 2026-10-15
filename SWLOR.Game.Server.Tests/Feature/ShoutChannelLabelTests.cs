using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class ShoutChannelLabelTests
{
    private MethodInfo _login = null!;

    [OneTimeSetUp]
    public void CompileLoginHandler()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server")))
            root = root.Parent;
        root.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(root!.FullName, "SWLOR.Game.Server", "Service", "Communication.cs"));
        var handler = CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "ApplyShoutChannelName");
        handler.AttributeLists.ToString().Should().Be("[NWNEventHandler(ScriptName.OnModuleEnter)]");

        // Run the actual login handler with a recording NWNX boundary, rather than assert its text.
        var harness = $$"""
            using System.Collections.Generic;
            public static class LoginHarness
            {
                private static uint _entering;
                private static bool _isPC, _isDM, _isPossessed;
                private static uint GetEnteringObject() => _entering;
                private static bool GetIsPC(uint player) => _isPC;
                private static bool GetIsDM(uint player) => _isDM;
                private static bool GetIsDMPossessed(uint player) => _isPossessed;
                private static readonly List<(uint Player, int StrRef, string Label)> Overrides = new();
                private static class PlayerPlugin
                {
                    public static void SetTlkOverride(uint player, int strRef, string label) =>
                        Overrides.Add((player, strRef, label));
                }
                public static (uint, int, string)[] Login(uint player, bool isPC, bool isDM, bool isPossessed)
                {
                    _entering = player;
                    _isPC = isPC;
                    _isDM = isDM;
                    _isPossessed = isPossessed;
                    {{handler.Identifier.ValueText}}();
                    return Overrides.ToArray();
                }
                {{handler.WithAttributeLists(default)}}
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ShoutChannelLabelHarness", [CSharpSyntaxTree.ParseText(harness)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        _login = Assembly.Load(stream.ToArray()).GetType("LoginHarness")!.GetMethod("Login")!;
    }

    [Test]
    public void Logins_SendOnePersonalOverrideEachWithoutChangingOtherClients()
    {
        Login(10, true, false, false).Should().Equal((10u, 66751, "Disabled"));
        // A DM avatar may not report as a PC; either way it must receive Shout.
        Login(20, false, true, false).Should().Equal((10u, 66751, "Disabled"), (20u, 66751, "Shout"));
        Login(30, true, false, false).Should().Equal(
            (10u, 66751, "Disabled"), (20u, 66751, "Shout"), (30u, 66751, "Disabled"));
        Login(40, true, true, false).Should().HaveCount(4).And.EndWith((40u, 66751, "Shout"));
        Login(50, false, false, true).Should().HaveCount(5).And.EndWith((50u, 66751, "Shout"));
        Login(60, false, false, false).Should().HaveCount(5, "non-player objects have no client");
    }

    private (uint Player, int StrRef, string Label)[] Login(uint player, bool isPC, bool isDM, bool isPossessed) =>
        ((uint, int, string)[])_login.Invoke(null, [player, isPC, isDM, isPossessed])!;
}
