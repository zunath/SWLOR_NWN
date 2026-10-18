#nullable enable
using System.Reflection;
using System.Text;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Reaches the boot-time character migrations, which are internal to the game assembly, and builds the minimal
/// saved character files the character upgrade can run over without the game engine.
/// </summary>
internal static class CharacterMigrations
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string ServiceNamespace = "SWLOR.Game.Server.Service.MigrationService.";
    private const string MigrationNamespace = "SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration.";

    private static readonly Assembly Game = typeof(Migration).Assembly;

    private static Type GameType(string name) => Game.GetType(name) ?? throw new InvalidOperationException($"No game type '{name}'.");

    /// <summary>The <c>ICharacterMigration</c> interface.</summary>
    public static Type Interface => GameType(ServiceNamespace + "ICharacterMigration");

    /// <summary>Every <c>ICharacterMigration</c> implementation, found by scanning the assembly as the upgrade does.</summary>
    public static IReadOnlyList<Type> ImplementationTypes => Game.GetTypes()
        .Where(type => !type.IsAbstract && !type.IsInterface && Interface.IsAssignableFrom(type)).ToList();

    /// <summary><c>CharacterFileUpgrade.Migrations</c>: the migrations the upgrade runs, in version order.</summary>
    public static IReadOnlyList<object> Registered =>
        ((IEnumerable<object>)GameType(ServiceNamespace + "CharacterFileUpgrade").GetProperty("Migrations", Members)!.GetValue(null)!).ToList();

    public static int LatestVersion =>
        (int)GameType(ServiceNamespace + "CharacterFileUpgrade").GetProperty("LatestVersion", Members)!.GetValue(null)!;

    public static int VersionOf(object migration) => (int)migration.GetType().GetProperty("Version")!.GetValue(migration)!;

    /// <summary>A migration class by name, for example <c>_22_ReviseLootStats</c>.</summary>
    public static Type Named(string name) => GameType(MigrationNamespace + name);

    public static Type? FindNamed(string name) => Game.GetType(MigrationNamespace + name);

    public static int VersionOf(string className) => VersionOf(Activator.CreateInstance(Named(className))!);

    /// <summary>The result of <c>CharacterFileUpgrade.Upgrade</c>, or null when the character was current.</summary>
    public sealed record Upgraded(byte[] File, Player Record);

    /// <summary>Runs the server's character upgrade over a saved file and a record, which it changes in place.</summary>
    public static Upgraded? Upgrade(byte[] file, Player record)
    {
        var result = GameType(ServiceNamespace + "CharacterFileUpgrade").GetMethod("Upgrade", Members)!
            .Invoke(null, new object?[] { file, record, null });
        return result == null
            ? null
            : new Upgraded((byte[])result.GetType().GetProperty("File")!.GetValue(result)!, (Player)result.GetType().GetProperty("Record")!.GetValue(result)!);
    }

    /// <summary>The migration checkpoint a saved character file holds.</summary>
    public static int ReadFileVersion(byte[] file)
    {
        var characterType = GameType(ServiceNamespace + "SavedCharacter");
        var character = Activator.CreateInstance(characterType, Members, null, new object?[] { file, null }, null)!;
        return (int)characterType.GetMethod("ReadInteger", Members)!.Invoke(character, new object[] { "PlayerFileVer" })!;
    }
}

/// <summary>Builds minimal saved character files (BIC) with the structures the character upgrade reads.</summary>
internal static class SavedCharacterFile
{
    private static readonly Type Document = typeof(Migration).Assembly
        .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    private static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;

    /// <summary>A character with no class, items or quickbar, holding the given migration checkpoint (0 for none).</summary>
    public static byte[] Create(int fileVersion, string id = "unit-test-character")
    {
        var root = Activator.CreateInstance(NodeType, true)!;
        var fields = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(root)!;
        fields.Add(Text("UUID", id));
        fields.Add(Field(0, "IsDM", BitConverter.GetBytes(0)));
        fields.Add(Field(15, "ClassList"));
        fields.Add(Field(15, "Equip_ItemList"));
        fields.Add(Field(15, "ItemList"));
        if (fileVersion != 0)
            fields.Add(Field(5, "PlayerFileVer", BitConverter.GetBytes(fileVersion)));
        return Encode(root);
    }

    private static object Field(uint type, string name, byte[]? data = null)
    {
        object? list = null;
        if (type == 15)
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType));
        return Document.GetMethod("MakeField", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new[] { (object)type, name, data, list })!;
    }

    private static object Text(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray());
    }

    private static byte[] Encode(object root)
    {
        NodeType.GetField("Type")!.SetValue(root, uint.MaxValue);
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, Encoding.ASCII.GetBytes("BIC V3.2"));
        Document.GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, root);
        return (byte[])Document.GetMethod("SerializeBytes", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(document, null)!;
    }
}
