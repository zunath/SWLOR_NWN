using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class BeastSoundsetTests
{
    private const int Bear = 7;
    private const uint NoText = uint.MaxValue;

    // soundset.2da rows that talk: spoken words, or humanoid speech with chat text.
    // Rows whose RESREF starts with "vs_" are NPC voice sets and are added from the 2DA.
    private static readonly Dictionary<int, string> SpeakingSoundsets = new()
    {
        [12] = "Bugbear", // gibberish battle cries with chat text
        [13] = "BugbrChf",
        [14] = "BugbrWiz",
        [23] = "Curst", // SWLOR_Haks replaces its c_vampire_* wavs with voice lines
        [25] = "Demon", // English battle cries ("I will rend your soul!")
        [60] = "LizMale",
        [83] = "SlaadPow",
        [84] = "SlaadWek",
        [255] = "Devil",
        [902] = "Aqualish",
    };

    [Test]
    public void GranitebackUrsavar_UsesBearSoundset()
    {
        var soundsets = ReadHak2DA("soundset.2da");
        soundsets.GetValue(Bear, "LABEL").Should().Be("Bear");

        BeastDefinitionCatalog.All[BeastType.GranitebackUrsavar].SoundSetId.Should().Be(Bear);
    }

    [Test]
    public void CreatureBeasts_DoNotUseSpeakingSoundsets()
    {
        var soundsets = ReadHak2DA("soundset.2da");
        var appearances = ReadHak2DA("appearance.2da");

        foreach (var (row, label) in SpeakingSoundsets)
            soundsets.GetValue(row, "LABEL").Should().Be(label, $"soundset.2da row {row} is listed as {label}");

        var speaking = SpeakingSoundsets.Keys.ToHashSet();
        for (var row = 0; row < soundsets.RowCount; row++)
        {
            if (soundsets.GetValue(row, "RESREF")?.StartsWith("vs_", StringComparison.OrdinalIgnoreCase) == true)
                speaking.Add(row);
        }

        var failures = BeastDefinitionCatalog.All
            .Where(beast => !string.Equals(appearances.GetValue((int)beast.Value.Appearance, "MODELTYPE"), "P",
                StringComparison.OrdinalIgnoreCase))
            .Where(beast => speaking.Contains(beast.Value.SoundSetId))
            .Select(beast => $"{beast.Key}: soundset {beast.Value.SoundSetId} " +
                             $"({soundsets.GetValue(beast.Value.SoundSetId, "LABEL")}) on appearance {beast.Value.Appearance}")
            .ToList();

        failures.Should().BeEmpty();
    }

    // A soundset entry's string shows as chat text when the sound plays. The c_treant set once
    // pointed its battle cries at class names in the custom TLK ("Charismatic Heroes").
    [Test]
    public void BeastSoundsetsFromTheHaks_ShowNoChatText()
    {
        var soundsets = ReadHak2DA("soundset.2da");
        var soundFolder = Path.Combine(HaksRoot(), "sw_sound");
        var failures = new List<string>();

        foreach (var soundsetId in BeastDefinitionCatalog.All.Values.Select(beast => beast.SoundSetId).Distinct())
        {
            var resref = soundsets.GetValue(soundsetId, "RESREF");
            if (string.IsNullOrWhiteSpace(resref) || resref == "****")
                continue;

            var path = Directory.EnumerateFiles(soundFolder, "*.ssf")
                .FirstOrDefault(file => string.Equals(Path.GetFileNameWithoutExtension(file), resref,
                    StringComparison.OrdinalIgnoreCase));
            if (path == null)
                continue;

            foreach (var (index, sound, strRef) in ReadSsf(path))
            {
                if (strRef != NoText)
                    failures.Add($"{Path.GetFileName(path)} entry {index} ({sound}): string {strRef}");
            }
        }

        failures.Should().BeEmpty();
    }

    private static IEnumerable<(int Index, string Sound, uint StrRef)> ReadSsf(string path)
    {
        var bytes = File.ReadAllBytes(path);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).Should().Be("SSF ", path);
        var count = BitConverter.ToInt32(bytes, 8);
        var table = BitConverter.ToInt32(bytes, 12);

        for (var index = 0; index < count; index++)
        {
            var entry = BitConverter.ToInt32(bytes, table + index * 4);
            var sound = System.Text.Encoding.ASCII.GetString(bytes, entry, 16).TrimEnd('\0');
            yield return (index, sound, BitConverter.ToUInt32(bytes, entry + 16));
        }
    }

    private static TwoDAFile ReadHak2DA(string fileName)
    {
        return TwoDAReader.Read(Path.Combine(HaksRoot(), "sw_2da", fileName));
    }

    private static string HaksRoot()
    {
        var configuredHaks = Environment.GetEnvironmentVariable("SWLOR_HAKS_CORPUS");
        return string.IsNullOrWhiteSpace(configuredHaks)
            ? Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks")
            : Path.GetFullPath(configuredHaks);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
