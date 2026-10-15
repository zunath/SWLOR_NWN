using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

public class HoloComCallTargetTests
{
    private const uint Character = 1;
    private const uint Controlled = 2;
    private const int NoAssociate = 0;
    private const int Familiar = 3;
    private const int DMPossess = 7;
    private const int DMPossessFullPower = 8;

    private Type _world = null!;
    private MethodInfo _isControlledByDM = null!;
    private MethodInfo _getIsDM = null!;
    private MethodInfo _getIsDMPossessed = null!;
    private MethodInfo _addPlayer = null!;

    [OneTimeSetUp]
    public void CompileProductionDMCheck()
    {
        var root = FindRepositoryRoot();
        var holoCom = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root.FullName,
            "SWLOR.Game.Server", "Service", "HoloCom.cs"))).GetRoot();
        var method = holoCom.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(declaration => declaration.Identifier.Text == "IsControlledByDM");

        // The world mirrors the engine: GetFirstPC returns each player's own character, possession only changes
        // the object they control, GetIsDM matches the controlled object of a player with DM privileges, and
        // GetIsDMPossessed reads the DM possession associate types.
        var source = $$"""
            using System.Collections.Generic;
            using static World;
            public static class HoloCom
            {
                {{method.ToFullString()}}
            }
            public static class PlayerPlugin
            {
                public static uint GetGameObject(uint player)
                {
                    foreach (var entry in Players)
                    {
                        if (entry.Character == player)
                            return entry.Controlled;
                    }

                    return OBJECT_INVALID;
                }
            }
            public static class World
            {
                public const uint OBJECT_INVALID = 0x7F000000;
                public static readonly List<(uint Character, uint Controlled, bool HasDMPrivileges)> Players = new();
                public static readonly Dictionary<uint, int> AssociateTypes = new();
                public static void Reset()
                {
                    Players.Clear();
                    AssociateTypes.Clear();
                }
                public static void AddPlayer(uint character, uint controlled, bool hasDMPrivileges, int controlledAssociateType)
                {
                    Players.Add((character, controlled, hasDMPrivileges));
                    AssociateTypes[controlled] = controlledAssociateType;
                }
                public static bool GetIsDM(uint creature)
                {
                    if (AssociateTypes.GetValueOrDefault(creature) != 0)
                        return false;

                    foreach (var entry in Players)
                    {
                        if (entry.Controlled == creature)
                            return entry.HasDMPrivileges;
                    }

                    return false;
                }
                public static bool GetIsDMPossessed(uint creature) => AssociateTypes.GetValueOrDefault(creature) is 7 or 8;
            }
            """;
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("HoloComCallTargetHarness", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        result.Success.Should().BeTrue(string.Join(Environment.NewLine, result.Diagnostics));
        var assembly = Assembly.Load(stream.ToArray());
        _world = assembly.GetType("World")!;
        _isControlledByDM = assembly.GetType("HoloCom")!.GetMethod("IsControlledByDM")!;
        _getIsDM = _world.GetMethod("GetIsDM")!;
        _getIsDMPossessed = _world.GetMethod("GetIsDMPossessed")!;
        _addPlayer = _world.GetMethod("AddPlayer")!;
    }

    [SetUp]
    public void Reset() => _world.GetMethod("Reset")!.Invoke(null, null);

    [Test]
    public void DMAvatarOrPlayerDM_IsNotCallable()
    {
        AddPlayer(Character, Character, hasDMPrivileges: true, NoAssociate);

        IsControlledByDM(Character).Should().BeTrue();
    }

    [TestCase(DMPossess)]
    [TestCase(DMPossessFullPower)]
    public void DMAvatar_IsNotCallableWhilePossessingACreature(int possessionType)
    {
        AddPlayer(Character, Controlled, hasDMPrivileges: true, possessionType);

        Invoke<bool>(_getIsDM, Character).Should().BeFalse("the engine only reports the controlled object as the DM");
        Invoke<bool>(_getIsDMPossessed, Character).Should().BeFalse("the avatar itself is not possessed");
        IsControlledByDM(Character).Should().BeTrue();
    }

    [Test]
    public void Player_IsCallable()
    {
        AddPlayer(Character, Character, hasDMPrivileges: false, NoAssociate);

        IsControlledByDM(Character).Should().BeFalse();
    }

    [Test]
    public void PlayerPossessingAFamiliar_IsCallable()
    {
        AddPlayer(Character, Controlled, hasDMPrivileges: false, Familiar);

        IsControlledByDM(Character).Should().BeFalse();
    }

    [Test]
    public void HoloComMenu_FiltersCallTargetsThroughTheDMCheck()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot().FullName,
            "SWLOR.Game.Server", "Feature", "DialogDefinition", "HoloComDialog.cs"));

        source.Should().Contain("HoloCom.IsControlledByDM(pc)");
    }

    private void AddPlayer(uint character, uint controlled, bool hasDMPrivileges, int controlledAssociateType) =>
        _addPlayer.Invoke(null, [character, controlled, hasDMPrivileges, controlledAssociateType]);

    private bool IsControlledByDM(uint player) => Invoke<bool>(_isControlledByDM, player);

    private static T Invoke<T>(MethodInfo method, params object[] arguments) => (T)method.Invoke(null, arguments)!;

    private static DirectoryInfo FindRepositoryRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;
        root.Should().NotBeNull();
        return root!;
    }
}
