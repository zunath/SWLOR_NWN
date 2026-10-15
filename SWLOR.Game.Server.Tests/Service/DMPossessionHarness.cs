using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Compiles production code against a model of how nwserver reports players while a DM possesses a creature.
/// The model follows the disassembled engine: GetFirstPC/GetNextPC return each player's own character, and
/// possession only changes the object the player controls. GetIsPC, GetIsDM, and message delivery all resolve the
/// player from the controlled object, so a possessing DM's avatar is not a PC, not a DM, and receives no messages.
/// </summary>
internal sealed class DMPossessionHarness
{
    public const uint Invalid = 0x7F000000;
    public const int Familiar = 3;
    public const int DMPossess = 7;
    public const int DMPossessFullPower = 8;

    private const string WorldSource = """
        using System.Collections.Generic;
        public static class World
        {
            public const uint OBJECT_INVALID = 0x7F000000;
            public static readonly List<(uint Character, uint Controlled, bool HasDMPrivileges)> Players = new();
            public static readonly Dictionary<uint, int> AssociateTypes = new();
            public static readonly Dictionary<uint, uint> Masters = new();
            public static readonly Dictionary<uint, uint> Areas = new();
            public static readonly List<(uint Character, string Message)> Messages = new();
            private static int _nextPC;

            public static void Reset()
            {
                Players.Clear();
                AssociateTypes.Clear();
                Masters.Clear();
                Areas.Clear();
                Messages.Clear();
            }

            public static void AddPlayer(uint character, bool hasDMPrivileges) =>
                Players.Add((character, character, hasDMPrivileges));

            public static void Possess(uint character, uint creature, int associateType)
            {
                var index = Players.FindIndex(player => player.Character == character);
                Players[index] = (character, creature, Players[index].HasDMPrivileges);
                AssociateTypes[creature] = associateType;
                Masters[creature] = character;
            }

            public static uint ReceivingPlayer(uint obj)
            {
                var index = FindController(obj);
                return index >= 0 ? Players[index].Character : OBJECT_INVALID;
            }

            public static uint GetFirstPC()
            {
                _nextPC = 0;
                return GetNextPC();
            }

            public static uint GetNextPC() => _nextPC < Players.Count ? Players[_nextPC++].Character : OBJECT_INVALID;
            public static bool GetIsObjectValid(uint obj) => obj != OBJECT_INVALID;
            public static bool GetIsPC(uint obj) => FindController(obj) >= 0;

            public static bool GetIsDM(uint obj)
            {
                var index = FindController(obj);
                return index >= 0 && Players[index].HasDMPrivileges && AssociateTypes.GetValueOrDefault(obj) == 0;
            }

            public static bool GetIsDMPossessed(uint obj) => AssociateTypes.GetValueOrDefault(obj) is 7 or 8;
            public static uint GetMaster(uint obj) => Masters.TryGetValue(obj, out var master) ? master : OBJECT_INVALID;
            public static uint GetArea(uint obj) => Areas.TryGetValue(obj, out var area) ? area : OBJECT_INVALID;
            public static float GetDistanceBetween(uint first, uint second) => 0f;
            public static string GetObjectUUID(uint obj) => obj.ToString();

            public static void SendMessageToPC(uint obj, string message)
            {
                var receiver = ReceivingPlayer(obj);
                if (receiver != OBJECT_INVALID)
                    Messages.Add((receiver, message));
            }

            private static int FindController(uint obj) => Players.FindIndex(player => player.Controlled == obj);
        }

        public static class PlayerPlugin
        {
            public static uint GetGameObject(uint player)
            {
                foreach (var entry in World.Players)
                {
                    if (entry.Character == player)
                        return entry.Controlled;
                }

                return World.OBJECT_INVALID;
            }
        }
        """;

    private readonly Assembly _assembly;
    private readonly Type _world;

    private DMPossessionHarness(Assembly assembly)
    {
        _assembly = assembly;
        _world = assembly.GetType("World")!;
    }

    /// <summary>
    /// Compiles the given classes, which see the engine model through <c>using static World</c>.
    /// </summary>
    public static DMPossessionHarness Compile(string source)
    {
        var harness = CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using static World;
            {{source}}
            """);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("DMPossessionHarness",
            [CSharpSyntaxTree.ParseText(WorldSource), harness], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        return new DMPossessionHarness(Assembly.Load(stream.ToArray()));
    }

    /// <summary>
    /// Returns the source of a production method, by name, from a file under SWLOR.Game.Server.
    /// </summary>
    public static string Method(string relativePath, string name) =>
        ParseServerFile(relativePath).DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name)
            .ToFullString();

    /// <summary>
    /// Returns the source of a production type declaration, such as an enum, by name.
    /// </summary>
    public static string Type(string relativePath, string name) =>
        ParseServerFile(relativePath).DescendantNodes().OfType<BaseTypeDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == name)
            .ToFullString();

    /// <summary>
    /// Returns the source of the GetFirstPC/GetNextPC loop in a production file that contains the marker text.
    /// </summary>
    public static string PlayerLoop(string relativePath, string marker) =>
        ParseServerFile(relativePath).DescendantNodes().OfType<ForStatementSyntax>()
            .Single(loop => loop.Declaration?.ToString().Contains("GetFirstPC()") == true &&
                            loop.Statement.ToString().Contains(marker))
            .ToFullString();

    public static string ReadServerFile(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", relativePath));

    public void Reset() => CallWorld("Reset");

    /// <summary>
    /// Adds a player who controls their own character.
    /// </summary>
    public void AddPlayer(uint character, bool isDM = false, uint area = Invalid)
    {
        CallWorld("AddPlayer", character, isDM);
        Place(character, area);
    }

    /// <summary>
    /// Moves a player's control to a creature, as DM possession or familiar possession does.
    /// </summary>
    public void Possess(uint character, uint creature, int associateType, uint creatureArea = Invalid)
    {
        CallWorld("Possess", character, creature, associateType);
        Place(creature, creatureArea);
    }

    public void Place(uint obj, uint area)
    {
        var areas = (Dictionary<uint, uint>)_world.GetField("Areas")!.GetValue(null)!;
        if (area == Invalid)
            areas.Remove(obj);
        else
            areas[obj] = area;
    }

    /// <summary>
    /// Returns the player whose client receives a message or chat sent to the object, or <see cref="Invalid"/>.
    /// </summary>
    public uint ReceivingPlayer(uint obj) => (uint)CallWorld("ReceivingPlayer", obj)!;

    public IReadOnlyList<(uint Character, string Message)> Messages =>
        (List<(uint Character, string Message)>)_world.GetField("Messages")!.GetValue(null)!;

    public T Invoke<T>(string type, string method, params object[] arguments) => (T)Invoke(type, method, arguments)!;

    public object Invoke(string type, string method, params object[] arguments) =>
        _assembly.GetType(type)!.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, arguments);

    public object GetField(string type, string field) =>
        _assembly.GetType(type)!.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    public void SetField(string type, string field, object value) =>
        _assembly.GetType(type)!.GetField(field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!
            .SetValue(null, value);

    private object CallWorld(string method, params object[] arguments) =>
        _world.GetMethod(method)!.Invoke(null, arguments);

    private static SyntaxNode ParseServerFile(string relativePath) =>
        CSharpSyntaxTree.ParseText(ReadServerFile(relativePath)).GetRoot();

    private static DirectoryInfo FindRepositoryRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        return root!;
    }
}
