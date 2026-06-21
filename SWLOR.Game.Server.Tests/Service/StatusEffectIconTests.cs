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
        ((int)rank1.Icon).Should().BeGreaterThan(131);
        ((int)rank2.Icon).Should().Be((int)rank1.Icon + 1);
    }

    [Test]
    public void PainSuppressantCustomIconRows_DoNotReplaceFoodOrDash()
    {
        var root = FindRepositoryRoot();
        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
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
        var effectIconRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
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

            if (spellRows.TryGetValue(row.Key, out var spellRow))
                spellRow["IconResRef"].Should().Be(row.IconResRef);

            var iconPath = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga", $"{row.IconResRef}.tga");
            AssertGameplayIconTga(iconPath, $"{row.Type} {row.Key}");
            AssertSemanticFrame(iconPath, row.SemanticCategory, $"{row.Type} {row.Key}");
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
    public void GameplayIconManifest_CombatAbilityCategoriesMatchGameplayIntent()
    {
        var root = FindRepositoryRoot();
        var abilityRows = ReadGameplayIconManifest(root)
            .Where(row => row.Type == "Ability")
            .ToDictionary(row => row.Key, StringComparer.OrdinalIgnoreCase);

        abilityRows["DuelistsChallenge1"].SemanticCategory.Should().Be("Self");
        abilityRows["ForceCapacitor1"].SemanticCategory.Should().Be("Self");
        abilityRows["ShelterCircle1"].SemanticCategory.Should().Be("Beneficial");
        abilityRows["SweepingGuard1"].SemanticCategory.Should().Be("Harmful");
    }

    [Test]
    public void GameplayIconManifest_CoversCustomFeatSpellIcons()
    {
        var root = FindRepositoryRoot();
        var manifestRows = ReadGameplayIconManifest(root)
            .Where(row => row.Type is "Ability" or "Feat" or "Spell")
            .ToArray();
        var manifestIcons = manifestRows
            .Select(row => row.IconResRef)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var customIconRefs = ReadCustomFeatSpellIconRefs(root);

        customIconRefs
            .Where(icon => !manifestIcons.Contains(icon))
            .Should()
            .BeEmpty("every custom feat/spell icon with a SWLOR TGA should be covered by the gameplay icon manifest");
        manifestIcons
            .Should()
            .NotContain(icon => IsDynamicShipModulePlaceholderIcon(icon), "ship module feat icons are runtime texture override anchors");

        manifestRows
            .Should()
            .Contain(row => row.IconResRef == "ife_tame" && row.SemanticCategory == "Utility");
        manifestRows
            .Should()
            .Contain(row => row.IconResRef == "ife_callbeast" && row.SemanticCategory == "Utility");
        manifestRows
            .Should()
            .Contain(row => row.IconResRef == "ife_harm_rest" && row.SemanticCategory == "Passive");

        foreach (var row in manifestRows)
        {
            var iconPath = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga", $"{row.IconResRef}.tga");
            AssertGameplayIconTga(iconPath, $"{row.Type} {row.Key}");
            AssertSemanticFrame(iconPath, row.SemanticCategory, $"{row.Type} {row.Key}");
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
            var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
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
        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
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
                fields[headerIndex["SemanticCategory"]],
                fields[headerIndex["IconResRef"]]));
        }

        return rows;
    }

    private static Dictionary<string, Dictionary<string, string>> Read2daByLabel(FileInfo file)
    {
        var rows = Test2daHelper.Read2da(file);
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

    private static IReadOnlySet<string> ReadCustomFeatSpellIconRefs(DirectoryInfo root)
    {
        const int customTlkOffset = 16777216;
        const int customFeatStart = 1116;
        const int customSpellStart = 1000;
        var iconRoot = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga");
        var icons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var featRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "feat.2da")));
        var spellRows = Test2daHelper.Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "spells.2da")));

        foreach (var (rowNumber, row) in featRows)
        {
            if (!HasLabel(row) ||
                GetLabel(row) is "****" or "DELETED" ||
                !row.TryGetValue("ICON", out var icon) ||
                icon == "****" ||
                !File.Exists(Path.Combine(iconRoot, $"{icon}.tga")))
                continue;

            var customRow =
                rowNumber >= customFeatStart ||
                IsCustomStrRef(row.GetValueOrDefault("FEAT"), customTlkOffset) ||
                IsCustomStrRef(row.GetValueOrDefault("DESCRIPTION"), customTlkOffset);

            if (customRow)
            {
                if (IsDynamicShipModulePlaceholderIcon(icon))
                    continue;

                icons.Add(icon);
            }
        }

        foreach (var (rowNumber, row) in spellRows)
        {
            if (!HasLabel(row) ||
                GetLabel(row) is "****" or "DELETED" ||
                !row.TryGetValue("IconResRef", out var icon) ||
                icon == "****" ||
                !File.Exists(Path.Combine(iconRoot, $"{icon}.tga")))
                continue;

            var customRow =
                rowNumber >= customSpellStart ||
                IsCustomStrRef(row.GetValueOrDefault("Name"), customTlkOffset) ||
                IsCustomStrRef(row.GetValueOrDefault("SpellDesc"), customTlkOffset);

            if (customRow)
                icons.Add(icon);
        }

        return icons;
    }

    private static bool IsCustomStrRef(string value, int customTlkOffset)
    {
        return int.TryParse(value, out var strRef) && strRef >= customTlkOffset;
    }

    private static bool IsDynamicShipModulePlaceholderIcon(string icon)
    {
        if (!icon.StartsWith("ife_sm", StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(icon["ife_sm".Length..], out var number) &&
               number is >= 1 and <= 30;
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

    private static void AssertSemanticFrame(string path, string category, string label)
    {
        var bytes = File.ReadAllBytes(path);
        var width = bytes[12] + (bytes[13] << 8);
        var height = bytes[14] + (bytes[15] << 8);
        var bits = bytes[16];
        var type = bytes[2];

        type.Should().Be(2, $"{label} TGA should be an uncompressed final gameplay icon");
        (bits is 24 or 32).Should().BeTrue($"{label} TGA should be 24-bit or 32-bit");

        var expected = GetSemanticColor(category);
        var bytesPerPixel = bits / 8;
        var offset = 18 + bytes[0];
        var matches = 0;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var isFramePixel =
                    x is >= 1 and <= 30 && (y is 1 or 30) ||
                    y is >= 1 and <= 30 && (x is 1 or 30) ||
                    x is >= 3 and <= 28 && (y is 3 or 28) ||
                    y is >= 3 and <= 28 && (x is 3 or 28);

                if (isFramePixel)
                {
                    var blue = bytes[offset];
                    var green = bytes[offset + 1];
                    var red = bytes[offset + 2];
                    if (Math.Abs(red - expected.Red) <= 55 &&
                        Math.Abs(green - expected.Green) <= 55 &&
                        Math.Abs(blue - expected.Blue) <= 55)
                    {
                        matches++;
                    }
                }

                offset += bytesPerPixel;
            }
        }

        matches.Should().BeGreaterThanOrEqualTo(16, $"{label} should have the {category} semantic frame color");
    }

    private static (int Red, int Green, int Blue) GetSemanticColor(string category)
    {
        return category switch
        {
            "Beneficial" => (84, 246, 122),
            "Harmful" => (240, 84, 84),
            "Self" => (79, 195, 255),
            "Control" => (181, 108, 255),
            "Deployable" => (255, 184, 77),
            "Passive" => (245, 215, 110),
            "Utility" => (221, 230, 240),
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };
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
        string SemanticCategory,
        string IconResRef);
}
