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
    private MethodInfo _confirmLogin = null!;

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
        var nativeSource = File.ReadAllText(Path.Combine(root.FullName, "SWLOR.Game.Server", "Native", "ChatChannelLabels.cs"));
        var nativeMethods = CSharpSyntaxTree.ParseText(nativeSource).GetRoot().DescendantNodes()
            .OfType<MethodDeclarationSyntax>().ToDictionary(method => method.Identifier.ValueText);
        nativeMethods["RegisterHook"].ToString().Should().Contain("_ZN11CNWSMessage31SendServerToPlayerLogin_ConfirmEj");
        // Replace only the unmanaged trampoline boundary with a recording client. Execute
        // the production role lookup, personal override, exception handling, and send order.
        var confirm = nativeMethods["ConfirmLogin"].WithAttributeLists(default).ToString()
            .Replace("var original = (delegate* unmanaged<void*, uint, int>)_loginConfirmHook->m_trampoline;", "")
            .Replace("original(message, playerId)", "RecordConfirmation(message, playerId)");

        // Run the actual login handler with a recording NWNX boundary, rather than assert its text.
        var harness = $$"""
            using System;
            using System.Collections.Generic;
            public static unsafe class LoginHarness
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
                private static bool _missingInfo;
                private static int _failures;
                private static readonly Dictionary<uint, string> ClientLabels = new();
                private static string _cachedLabel = "";
                private sealed class PlayerInfo { public int m_bGameMasterPrivileges => _isDM ? 1 : 0; }
                private sealed class Server
                {
                    public Server m_pServerExoApp => this;
                    public Server GetNetLayer() => this;
                    public PlayerInfo GetPlayerInfo(uint id) => _missingInfo ? null : new();
                }
                private static class NWNXLib { public static Server g_pAppManager = new(); }
                private sealed class CExoString : IDisposable
                {
                    public string Text;
                    public CExoString(string text) => Text = text;
                    public void Dispose() { }
                }
                private sealed class CNWSMessage
                {
                    public static CNWSMessage FromPointer(void* message) => new();
                    public int SendServerToPlayerSetTlkOverride(uint id, int strRef, CExoString text)
                    {
                        if (strRef != 66751) throw new Exception("Incorrect chat label strref");
                        ClientLabels[id] = text.Text;
                        return 1;
                    }
                }
                private static class Log { public static void WriteError(Exception ex, string text) => _failures++; }
                private static int RecordConfirmation(void* message, uint id)
                {
                    // Login/Confirm constructs the dropdown from the client's current TLK table.
                    _cachedLabel = ClientLabels.GetValueOrDefault(id, "Shout");
                    return 73;
                }
                public static (int Result, string CachedLabel, int Failures) Confirm(uint id, bool isDM, bool missingInfo)
                {
                    _isDM = isDM;
                    _missingInfo = missingInfo;
                    _failures = 0;
                    var result = ConfirmLogin(null, id);
                    return (result, _cachedLabel, _failures);
                }
                {{confirm}}
                {{nativeMethods["SendLabel"].WithAttributeLists(default)}}
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ShoutChannelLabelHarness", [CSharpSyntaxTree.ParseText(harness)],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var type = Assembly.Load(stream.ToArray()).GetType("LoginHarness")!;
        _login = type.GetMethod("Login")!;
        _confirmLogin = type.GetMethod("Confirm")!;
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

    [Test]
    public void LoginConfirmation_CachesPersonalLabelBeforeAnyCharacterOrAreaExists()
    {
        Confirm(100, false).Should().Be((73, "Disabled", 0));
        Confirm(200, true).Should().Be((73, "Shout", 0));
        Confirm(100, false).Should().Be((73, "Disabled", 0), "DM logins must not change another client's TLK");
    }

    [Test]
    public void LoginConfirmation_StillSendsConfirmationWhenPlayerInfoIsMissing()
    {
        Confirm(300, false, true).Should().Be((73, "Shout", 1));
    }

    private (int Result, string CachedLabel, int Failures) Confirm(uint player, bool isDM, bool missingInfo = false) =>
        ((int, string, int))_confirmLogin.Invoke(null, [player, isDM, missingInfo])!;
}
