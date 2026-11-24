using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class BeastSoundsetTests
{
    private const int Bear = 7;

    // soundset.2da rows whose lines are spoken words rather than creature sounds.
    // 23 Curst: SWLOR_Haks replaces the c_vampire_* wavs it plays with voice lines.
    // 25 Demon and 255 Devil: English battle cries ("I will rend your soul!").
    // Rows whose RESREF starts with "vs_" are NPC voice sets and are added from the 2DA.
    private static readonly Dictionary<int, string> SpokenSoundsets = new()
    {
        [23] = "Curst",
        [25] = "Demon",
        [255] = "Devil",
    };

    // Creature beasts that already use a spoken soundset and wait on an owner decision.
    // Remove an entry once its beast gets a creature soundset.
    private static readonly HashSet<BeastType> PendingSoundsetReview = new()
    {
        BeastType.DeepstoneGraxal,
        BeastType.VoidmireEcho,
        BeastType.BloodtuskRavor,
    };

    [Test]
    public void GranitebackUrsavar_UsesBearSoundset()
    {
        var soundsets = ReadHak2DA("soundset.2da");
        soundsets.GetValue(Bear, "LABEL").Should().Be("Bear");

        BeastDefinitionCatalog.All[BeastType.GranitebackUrsavar].SoundSetId.Should().Be(Bear);
    }

    [Test]
    public void CreatureBeasts_DoNotUseSpokenSoundsets()
    {
        var soundsets = ReadHak2DA("soundset.2da");
        var appearances = ReadHak2DA("appearance.2da");

        foreach (var (row, label) in SpokenSoundsets)
            soundsets.GetValue(row, "LABEL").Should().Be(label, $"soundset.2da row {row} is listed as {label}");

        var spoken = GetSpokenSoundsets(soundsets);
        var failures = BeastDefinitionCatalog.All
            .Where(beast => !PendingSoundsetReview.Contains(beast.Key))
            .Where(beast => !string.Equals(appearances.GetValue((int)beast.Value.Appearance, "MODELTYPE"), "P",
                StringComparison.OrdinalIgnoreCase))
            .Where(beast => spoken.Contains(beast.Value.SoundSetId))
            .Select(beast => $"{beast.Key}: soundset {beast.Value.SoundSetId} " +
                             $"({soundsets.GetValue(beast.Value.SoundSetId, "LABEL")}) on appearance {beast.Value.Appearance}")
            .ToList();

        failures.Should().BeEmpty();
    }

    [Test]
    public void PendingSoundsetReview_ListsOnlyBeastsThatStillUseSpokenSoundsets()
    {
        var spoken = GetSpokenSoundsets(ReadHak2DA("soundset.2da"));

        foreach (var beast in PendingSoundsetReview)
            spoken.Should().Contain(BeastDefinitionCatalog.All[beast].SoundSetId, $"{beast} no longer needs an exception");
    }

    private static HashSet<int> GetSpokenSoundsets(TwoDAFile soundsets)
    {
        var spoken = SpokenSoundsets.Keys.ToHashSet();
        for (var row = 0; row < soundsets.RowCount; row++)
        {
            if (soundsets.GetValue(row, "RESREF")?.StartsWith("vs_", StringComparison.OrdinalIgnoreCase) == true)
                spoken.Add(row);
        }

        return spoken;
    }

    private static TwoDAFile ReadHak2DA(string fileName)
    {
        var configuredHaks = Environment.GetEnvironmentVariable("SWLOR_HAKS_CORPUS");
        var haks = string.IsNullOrWhiteSpace(configuredHaks)
            ? Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks")
            : Path.GetFullPath(configuredHaks);
        return TwoDAReader.Read(Path.Combine(haks, "sw_2da", fileName));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
