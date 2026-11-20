using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Service.Search;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the login fix for the craft file sequence: on a relog the engine copies the locals of the earlier login over the new creature,
/// so the craft locals are only right once <c>NwnCraftSessionWorld.ApplyLoadedSequence</c> has put the file's value back. Every read
/// therefore goes through <c>NwnCraftSessionWorld</c>, whose accessors apply it first.
/// </summary>
public class CraftFileSequenceReaderTests
{
    [Test]
    public void OnlyTheSequenceOwners_TouchTheCraftLocalsDirectly()
    {
        // A new reader must call NwnCraftSessionWorld (FileSequence, LoadedSequence, ExportedSequence, AdvanceFileSequence, FlushCharacter).
        var root = Path.Combine(TestRepository.FindRoot(), "SWLOR.Game.Server");
        var owners = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"CraftSessionFileSequence\.(Variable|ExportedVariable|LoadedVariable)\b"))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        owners.Should().BeEquivalentTo(
            "Native/PlayerFileCheckpoint.cs",
            "Domain/Crafting/Minigame/LoadedCraftFileSequences.cs",
            "Domain/Crafting/Minigame/NwnCraftSessionWorld.cs");
    }

    [TestCase("public int FileSequence(uint player)")]
    [TestCase("public int AdvanceFileSequence(uint player)")]
    [TestCase("public int LoadedSequence(uint player)")]
    [TestCase("public int ExportedSequence(uint player)")]
    [TestCase("private static void ExportAndRecord(uint player)")]
    public void EveryCraftLocalAccessor_AppliesTheFilesSequenceFirst(string signature)
    {
        var accessor = Method(Source("Domain", "Crafting", "Minigame", "NwnCraftSessionWorld.cs"), signature);

        var apply = accessor.IndexOf("ApplyLoadedSequence(player);", StringComparison.Ordinal);
        apply.Should().BeGreaterThan(0, "the accessor applies the loaded file's sequence");
        apply.Should().BeLessThan(accessor.IndexOf("GetLocalInt(", StringComparison.Ordinal), "it applies before it reads");
    }

    [TestCase("Native", "PlayerFileCheckpoint.cs", "public static void ApplyLoadedCraftSequence()")]
    public void TheModuleEnterHandler_AppliesTheFilesSequence(string folder, string file, string signature)
    {
        Method(Source(folder, file), signature).Should().Contain("NwnCraftSessionWorld.Instance.ApplyLoadedSequence(GetEnteringObject());");
    }

    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { TestRepository.FindRoot(), "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException(signature);
    }
}
