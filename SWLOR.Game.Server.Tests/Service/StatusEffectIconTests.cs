using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectIconTests
{
    [Test]
    public void PainSuppressant_UsesGeneratedCustomStatusIcons()
    {
        var rank1 = new PainSuppressant1StatusEffect();
        var rank2 = new PainSuppressant2StatusEffect();

        rank1.Icon.Should().Be(EffectIconType.PainSuppressant1StatusEffect);
        rank2.Icon.Should().Be(EffectIconType.PainSuppressant2StatusEffect);
        ((int)rank1.Icon).Should().Be(315);
        ((int)rank2.Icon).Should().Be(316);
    }

    [Test]
    public void PainSuppressantCustomIconRows_DoNotReplaceFoodOrDash()
    {
        var root = FindRepositoryRoot();
        var rows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "effecticons.2da")));

        rows[130]["Label"].Should().Be("FOOD");
        rows[130]["Icon"].Should().Be("ife_aether_curre");
        rows[131]["Label"].Should().Be("DASH");
        rows[131]["Icon"].Should().Be("ife_sprint");
        rows[(int)EffectIconType.PainSuppressant1StatusEffect]["Label"].Should().Be("PainSuppressant1");
        rows[(int)EffectIconType.PainSuppressant1StatusEffect]["Icon"].Should().Be("ief_painsup1");
        rows[(int)EffectIconType.PainSuppressant2StatusEffect]["Label"].Should().Be("PainSuppressant2");
        rows[(int)EffectIconType.PainSuppressant2StatusEffect]["Icon"].Should().Be("ief_painsup2");
    }

    [Test]
    public void GameplayIconManifest_StatusEffectsMatchEffectIconsAndGeneratedFiles()
    {
        var root = FindRepositoryRoot();
        var manifestRows = ReadGameplayIconManifest(root)
            .Where(row => row.Type == "StatusEffect")
            .ToArray();
        var effectIconRows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "effecticons.2da")));
        var iconHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in manifestRows)
        {
            Enum.TryParse<EffectIconType>(row.Key, out var enumValue)
                .Should().BeTrue($"{row.Key} should exist in EffectIconType");

            var rowNumber = (int)enumValue;
            effectIconRows.Should().ContainKey(rowNumber, $"{row.Key} should be in effecticons.2da row {rowNumber}");
            var effectIconRow = effectIconRows[rowNumber];
            effectIconRow["Label"].Should().NotBeNullOrWhiteSpace();
            effectIconRow["Icon"].Should().Be(row.IconResRef);
            effectIconRow["StrRef"].Should().MatchRegex(@"^\d+$");

            var iconPath = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga", $"{row.IconResRef}.tga");
            AssertGameplayIconTga(iconPath, $"{row.Type} {row.Key}");
            AssertUniqueIconPixels(iconPath, $"{row.Type} {row.Key}", iconHashes);
        }
    }

    [Test]
    public void GameplayIconManifest_AbilitiesMatchFeatSpellIconsAndGeneratedFiles()
    {
        var root = FindRepositoryRoot();
        var manifestRows = ReadGameplayIconManifest(root)
            .Where(row => row.Type == "Ability")
            .ToArray();
        var featRows = Read2daByLabel(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "feat.2da")));
        var spellRows = Read2daByLabel(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "spells.2da")));
        var iconHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        manifestRows
            .GroupBy(row => row.IconResRef, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Should()
            .BeEmpty("ability icon resrefs should not be reused after a gameplay rename");

        foreach (var row in manifestRows)
        {
            featRows.Should().ContainKey(row.Key, $"{row.Key} should exist in feat.2da");
            featRows[row.Key]["ICON"].Should().Be(row.IconResRef);

            spellRows.Should().ContainKey(row.Key, $"{row.Key} should exist in spells.2da");
            spellRows[row.Key]["IconResRef"].Should().Be(row.IconResRef);

            var iconPath = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga", $"{row.IconResRef}.tga");
            AssertGameplayIconTga(iconPath, $"{row.Type} {row.Key}");
            AssertUniqueIconPixels(iconPath, $"{row.Type} {row.Key}", iconHashes);

            if (!row.IconResRef.StartsWith("ife_", StringComparison.OrdinalIgnoreCase))
                continue;

            var suffix = row.IconResRef[4..];
            for (var stage = 0; stage <= 5; stage++)
            {
                var cooldownPath = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga", $"pr{stage}_{suffix}.tga");
                AssertGameplayIconTga(cooldownPath, $"{row.Type} {row.Key} cooldown pr{stage}");
            }
        }
    }

    [Test]
    public void Gameplay2daLabels_DoNotExposeObsoleteRows()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            "feat.2da",
            "spells.2da",
            "effecticons.2da"
        };
        var failures = new List<string>();

        foreach (var file in files)
        {
            var rows = Read2da(new FileInfo(Path.Combine(
                root.FullName,
                "SWLOR_Haks",
                "swlor2_2da",
                file)));

            foreach (var (rowNumber, row) in rows)
            {
                if (!HasLabel(row))
                    continue;

                var label = GetLabel(row);
                if (label.StartsWith("Obsolete", StringComparison.Ordinal))
                    failures.Add($"{file}:{rowNumber}:{label}");
            }
        }

        failures.Should().BeEmpty("retired 2DA rows should be fully blank placeholders");
    }

    [Test]
    public void CurrentCustomFeatRows_RetainFeat2daLabels()
    {
        var root = FindRepositoryRoot();
        var rows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "feat.2da")));
        var feats = new[]
        {
            FeatType.WeaponBlueprints1,
            FeatType.WeaponBlueprints2,
            FeatType.WeaponBlueprints3,
            FeatType.WeaponBlueprints4,
            FeatType.WeaponBlueprints5,
            FeatType.ArmorBlueprints1,
            FeatType.ArmorBlueprints2,
            FeatType.ArmorBlueprints3,
            FeatType.ArmorBlueprints4,
            FeatType.ArmorBlueprints5,
            FeatType.AccessoryBlueprints1,
            FeatType.AccessoryBlueprints2,
            FeatType.AccessoryBlueprints3,
            FeatType.AccessoryBlueprints4,
            FeatType.AccessoryBlueprints5,
            FeatType.FurnitureBlueprints1,
            FeatType.FurnitureBlueprints2,
            FeatType.FurnitureBlueprints3,
            FeatType.FurnitureBlueprints4,
            FeatType.FurnitureBlueprints5,
            FeatType.StructureBlueprints1,
            FeatType.StructureBlueprints2,
            FeatType.StructureBlueprints3,
            FeatType.StructureBlueprints4,
            FeatType.StructureBlueprints5,
            FeatType.StarshipBlueprints1,
            FeatType.StarshipBlueprints2,
            FeatType.StarshipBlueprints3,
            FeatType.StarshipBlueprints4,
            FeatType.StarshipBlueprints5,
            FeatType.CookingRecipes1,
            FeatType.CookingRecipes2,
            FeatType.CookingRecipes3,
            FeatType.CookingRecipes4,
            FeatType.CookingRecipes5,
            FeatType.Provoke1,
            FeatType.EnhancementBlueprints1,
            FeatType.EnhancementBlueprints2,
            FeatType.EnhancementBlueprints3,
            FeatType.EnhancementBlueprints4,
            FeatType.EnhancementBlueprints5,
            FeatType.DroidEquipmentBlueprints1,
            FeatType.DroidEquipmentBlueprints2,
            FeatType.DroidEquipmentBlueprints3,
            FeatType.DroidEquipmentBlueprints4,
            FeatType.DroidEquipmentBlueprints5
        };
        var failures = new List<string>();

        foreach (var feat in feats)
        {
            var rowNumber = (int)feat;
            if (!rows.TryGetValue(rowNumber, out var row))
            {
                failures.Add($"{feat} row {rowNumber} is missing from feat.2da.");
                continue;
            }

            var label = GetLabel(row);
            if (!label.Equals(feat.ToString(), StringComparison.Ordinal))
                failures.Add($"{feat} row {rowNumber} has label '{label}'.");
        }

        failures.Should().BeEmpty("current custom feats outside the combat-upgrade generated range must not be blanked or renamed");
    }

    [Test]
    public void RenamedGeneratedIcons_DoNotLeaveOldResourceFiles()
    {
        var root = FindRepositoryRoot();
        var iconRoot = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga");
        var obsoleteFiles = new[]
        {
            "ief_forcerage1.tga",
            "ief_forcerage2.tga",
            "ife_forcerg1.tga",
            "ife_forcerg2.tga",
            "ife_grn_ion1.tga",
            "ife_grn_ion2.tga",
            "ife_grn_ion3.tga",
            "ife_cast_light.tga",
            "pr0_forcerg1.tga",
            "pr5_forcerg2.tga",
            "pr0_grn_ion1.tga",
            "pr5_grn_ion3.tga",
            "pr0_cast_light.tga",
            "pr5_cast_light.tga"
        };

        foreach (var file in obsoleteFiles)
        {
            File.Exists(Path.Combine(iconRoot, file)).Should().BeFalse($"{file} was replaced by a gameplay-name-matching resref");
        }
    }

    [Test]
    public void EffectIconDocumentation_AllowsEffectIconRowsPast255()
    {
        var root = FindRepositoryRoot();
        var effectFunctions = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.NWN.API",
            "NWScript",
            "EffectFunctions.cs"));
        var nwscript = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.NWN.API",
            "NWN",
            "nwscript-8193.37.nss"));

        effectFunctions.Should().Contain("support effecticons.2da rows past 255");
        effectFunctions.Should().Contain("Older clients are simply not sent icons past row 255");
        effectFunctions.Should().NotContain("nIconID is < 1 or > 255");

        nwscript.Should().Contain("effecticons.2da rows past 255 are supported");
        nwscript.Should().Contain("Older clients are simply not sent icons past row 255");
        nwscript.Should().NotContain("nIconID is < 1 or > 255");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }

    private static IReadOnlyList<IconManifestRow> ReadGameplayIconManifest(DirectoryInfo root)
    {
        var file = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Readmes",
            "GameplayIconManifest.csv");
        using var parser = new TextFieldParser(file);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields();
        headers.Should().NotBeNull();
        var headerIndex = headers!
            .Select((header, index) => (header, index))
            .ToDictionary(x => x.header, x => x.index, StringComparer.OrdinalIgnoreCase);
        var rows = new List<IconManifestRow>();

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null || fields.Length == 0)
                continue;

            rows.Add(new IconManifestRow(
                fields[headerIndex["Type"]],
                fields[headerIndex["Key"]],
                fields[headerIndex["DisplayName"]],
                fields[headerIndex["IconResRef"]]));
        }

        return rows;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(FileInfo file)
    {
        var lines = File.ReadAllLines(file.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var headers = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var rows = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < headers.Length + 1 || !int.TryParse(parts[0], out var rowNumber))
                continue;

            var row = new Dictionary<string, string>();
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = parts[index + 1];
            }

            rows[rowNumber] = row;
        }

        return rows;
    }

    private static Dictionary<string, Dictionary<string, string>> Read2daByLabel(FileInfo file)
    {
        var rows = Read2da(file);
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows.Values.Where(HasLabel))
        {
            var label = GetLabel(row);
            if (label is "****" or "DELETED")
                continue;

            result.TryAdd(label, row);
        }

        return result;
    }

    private static bool HasLabel(IReadOnlyDictionary<string, string> row)
    {
        return row.ContainsKey("LABEL") || row.ContainsKey("Label");
    }

    private static string GetLabel(IReadOnlyDictionary<string, string> row)
    {
        return row.TryGetValue("LABEL", out var upperLabel)
            ? upperLabel
            : row["Label"];
    }

    private static void AssertGameplayIconTga(string path, string label)
    {
        File.Exists(path).Should().BeTrue($"{label} should have a generated TGA");
        var bytes = File.ReadAllBytes(path);
        bytes.Length.Should().BeGreaterThanOrEqualTo(18, $"{label} TGA should have a header");
        var width = bytes[12] + (bytes[13] << 8);
        var height = bytes[14] + (bytes[15] << 8);

        width.Should().Be(32, $"{label} TGA width should match NWN gameplay icon size");
        height.Should().Be(32, $"{label} TGA height should match NWN gameplay icon size");
        (bytes[17] & 32).Should().Be(0, $"{label} TGA should use bottom-left origin");

        if (bytes[16] != 32)
            return;

        for (var offset = 18; offset < bytes.Length; offset += 4)
        {
            bytes[offset + 3].Should().Be(255, $"{label} TGA should be fully opaque");
        }
    }

    private static void AssertUniqueIconPixels(
        string path,
        string label,
        IDictionary<string, string> iconHashes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        iconHashes.Should().NotContainKey(hash, $"{label} should not reuse another gameplay icon's pixels");
        iconHashes[hash] = label;
    }

    private sealed record IconManifestRow(
        string Type,
        string Key,
        string DisplayName,
        string IconResRef);
}
