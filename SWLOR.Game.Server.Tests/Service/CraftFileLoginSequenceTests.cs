using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.ProspectingService;
using SWLOR.Game.Server.Tests.Service.Search;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards the login checks that compare the player record with the character file. The engine keeps a player's locals across relogs
/// until a restart, so the sequence the file loaded with must come from the file itself (<see cref="CraftFileLoginSequences"/>), applied
/// once at login before any check reads it. With the locals of the last session, a relog gave back every harvest, craft output and
/// Search find of that session, even ones the player had since trashed or stored.
/// </summary>
public class CraftFileLoginSequenceTests
{
    [SetUp]
    public void Clear()
    {
        foreach (var id in new[] { "a", "b" })
            CraftFileLoginSequences.TryTake(id, out _);
    }

    [Test]
    public void TheSequenceAFileLoadedWith_IsTakenOnce()
    {
        CraftFileLoginSequences.Record("a", 12);

        CraftFileLoginSequences.TryTake("a", out var sequence).Should().BeTrue();
        sequence.Should().Be(12);
        CraftFileLoginSequences.TryTake("a", out _).Should().BeFalse(
            "a second login check must not reset the sequence the first one advanced");
    }

    [Test]
    public void ALaterLoadOfTheSameCharacter_ReplacesTheEarlierOne_AndOthersAreKeptApart()
    {
        CraftFileLoginSequences.Record("a", 4);
        CraftFileLoginSequences.Record("b", 30);
        CraftFileLoginSequences.Record("a", 9);

        CraftFileLoginSequences.TryTake("a", out var a).Should().BeTrue();
        CraftFileLoginSequences.TryTake("b", out var b).Should().BeTrue();
        a.Should().Be(9);
        b.Should().Be(30);
    }

    [Test]
    public void AFileWithNoSequence_LoadsAsZero_AndACharacterWithNoIdIsIgnored()
    {
        CraftFileLoginSequences.Record("a", -3);
        CraftFileLoginSequences.Record(" ", 5);

        CraftFileLoginSequences.TryTake("a", out var sequence).Should().BeTrue();
        sequence.Should().Be(0);
        CraftFileLoginSequences.TryTake(" ", out _).Should().BeFalse();
    }

    [Test]
    public void ARelog_WhoseFileHoldsTheHarvest_GivesNothingAgain()
    {
        // First session: a harvest is journaled at sequence 1, and the logout writes the file at sequence 1. The player then trashed
        // or stored the units. The relog reads the file's sequence, not the 0 its locals kept from the first login of the server run.
        var player = new Player();
        HarvestReturnJournal.Record(player, exported: 0, sequence: 1, new[] { "raw_veldite", "raw_veldite", "jade" });
        CraftFileLoginSequences.Record("a", 1);

        CraftFileLoginSequences.TryTake("a", out var loaded).Should().BeTrue();
        HarvestReturnJournal.TakeAtLogin(player, loaded).Should().BeEmpty();
        player.PendingHarvestReturns.Should().BeEmpty();
    }

    [TestCase("Feature", "CraftingInterruptions.cs", "public static void SettleOnLogin()", "CraftSessionService.RecoverAtLogin(player)")]
    [TestCase("Feature", "ResearchInterruptions.cs", "public static void SettleOnLogin()", "ResearchExperimentService.RecoverAtLogin(player)")]
    [TestCase("Service", "Performance.cs", "public static void RecoverOnLogin()", "RecoverInterruptedPerformance(player, atLogin: true)")]
    public void EveryLoginCheck_AppliesTheFilesSequence_BeforeItReadsIt(string folder, string file, string signature, string firstCheck)
    {
        var handler = Method(Source(folder, file), signature);

        handler.Should().Contain("PlayerFileCheckpoint.ApplyLoginCraftSequence(player);");
        handler.IndexOf("ApplyLoginCraftSequence(player)", StringComparison.Ordinal)
            .Should().BeLessThan(handler.IndexOf(firstCheck, StringComparison.Ordinal));
    }

    [Test]
    public void OnlyTheKnownLoginChecks_ReadTheSequenceTheFileLoadedWith()
    {
        // A new reader of the loaded sequence must be called after ApplyLoginCraftSequence; add its handler to the test above.
        var root = Path.Combine(TestRepository.FindRoot(), "SWLOR.Game.Server");
        var readers = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"CraftSessionFileSequence\.LoadedVariable|\.LoadedSequence\("))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        readers.Should().BeEquivalentTo(
            "Native/PlayerFileCheckpoint.cs",
            "Service/CraftService/Minigame/CraftSessionLifecycle.cs",
            "Service/CraftService/Minigame/NwnCraftSessionWorld.cs",
            "Service/CraftService/Research/ResearchExperimentService.cs",
            "Service/Performance.cs",
            "Service/ProspectingService/NwnProspectingWorld.cs",
            "Service/ScavengeService/NwnScavengeWorld.cs");
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
