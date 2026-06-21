using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class PassivePerkIconTests
{
    private const int PassiveTraitFeatStart = 1171;
    private const int PassiveTraitFeatEnd = 1400;
    private const int CustomTlkOffset = 16777216;

    [Test]
    public void ActivePerks_ResolveAnIconFromGrantedFeats()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "feat.2da")));
        var iconRoot = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga");
        var failures = new List<string>();

        foreach (var perk in BuildPerksWithout2daLookup().OrderBy(x => x.Detail.Name).ThenBy(x => x.Type))
        {
            if (!perk.Detail.IsActive)
                continue;

            var iconFeat = perk.Detail.PerkLevels
                .OrderBy(x => x.Key)
                .SelectMany(x => x.Value.GrantedFeats)
                .Select(feat => (Feat: feat, RowNumber: (int)feat, Row: featRows.GetValueOrDefault((int)feat)))
                .FirstOrDefault(x => HasIcon(x.Row));

            if (iconFeat.Row == null)
            {
                failures.Add($"{perk.Type} ({perk.Detail.Name}) does not grant any feat with a feat.2da ICON.");
                continue;
            }

            var icon = iconFeat.Row["ICON"];
            if (icon.Equals("default_perk", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add($"{perk.Type} ({perk.Detail.Name}) resolves to default_perk from {iconFeat.Feat}.");
                continue;
            }

            var iconPath = Path.Combine(iconRoot, $"{icon}.tga");
            if (!File.Exists(iconPath))
                failures.Add($"{perk.Type} icon feat {iconFeat.Feat} should have a TGA at {iconPath}.");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(200)));
    }

    [Test]
    public void PassiveTraitFeatRows_AreNonUsableAndHaveGeneratedIcons()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(new FileInfo(Path.Combine(
            root.FullName,
            "SWLOR_Haks",
            "swlor2_2da",
            "feat.2da")));
        var iconRoot = Path.Combine(root.FullName, "SWLOR_Haks", "swlor2_tga");
        var passiveRows = featRows
            .Where(x => x.Key is >= PassiveTraitFeatStart and <= PassiveTraitFeatEnd)
            .Where(x => HasLabel(x.Value))
            .ToArray();
        var failures = new List<string>();

        passiveRows.Should().HaveCount(230);

        foreach (var (rowNumber, row) in passiveRows.OrderBy(x => x.Key))
        {
            var label = row["LABEL"];
            if (!label.EndsWith("Trait", StringComparison.Ordinal))
                failures.Add($"feat.2da row {rowNumber} label '{label}' should end with Trait.");

            if (!Enum.TryParse<FeatType>(label, out var feat) || (int)feat != rowNumber)
                failures.Add($"feat.2da row {rowNumber} label '{label}' should match FeatType.{label}.");

            AssertCustomStrRef(row["FEAT"], $"{label} FEAT", failures);
            AssertCustomStrRef(row["DESCRIPTION"], $"{label} DESCRIPTION", failures);
            AssertColumn(row, "CATEGORY", "****", label, failures);
            AssertColumn(row, "SPELLID", "****", label, failures);
            AssertColumn(row, "TARGETSELF", "****", label, failures);
            AssertColumn(row, "HostileFeat", "****", label, failures);
            AssertColumn(row, "USESPERDAY", "****", label, failures);
            AssertColumn(row, "MinLevel", "99", label, failures);
            AssertColumn(row, "ReqAction", "0", label, failures);

            var icon = row["ICON"];
            if (!icon.StartsWith("ife_", StringComparison.OrdinalIgnoreCase))
                failures.Add($"{label} icon '{icon}' should start with ife_.");
            if (icon.Length > 16)
                failures.Add($"{label} icon '{icon}' exceeds NWN's 16-character resref limit.");

            AssertGameplayIconTga(Path.Combine(iconRoot, $"{icon}.tga"), $"{label} icon", failures);
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures.Take(200)));
    }

    private static IReadOnlyCollection<PerkRecord> BuildPerksWithout2daLookup()
    {
        var result = new List<PerkRecord>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(x))
            .OrderBy(x => x.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(x => x.ReturnType == typeof(void) && x.GetParameters().Length == 0 && !x.Name.Contains('<'))
                         .OrderBy(x => x.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;

            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;

            result.AddRange(perks.Select(x => new PerkRecord(x.Key, x.Value)));
        }

        return result;
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
            if (parts.Length == 0 || !int.TryParse(parts[0], out var rowNumber))
                continue;

            var row = new Dictionary<string, string>();
            for (var index = 0; index < headers.Length && index + 1 < parts.Length; index++)
            {
                row[headers[index]] = parts[index + 1];
            }

            rows[rowNumber] = row;
        }

        return rows;
    }

    private static bool HasIcon(IReadOnlyDictionary<string, string> row)
    {
        return row != null &&
               row.TryGetValue("ICON", out var icon) &&
               !string.IsNullOrWhiteSpace(icon) &&
               icon != "****";
    }

    private static bool HasLabel(IReadOnlyDictionary<string, string> row)
    {
        return row.TryGetValue("LABEL", out var label) &&
               !string.IsNullOrWhiteSpace(label) &&
               label != "****";
    }

    private static void AssertColumn(
        IReadOnlyDictionary<string, string> row,
        string column,
        string expected,
        string label,
        ICollection<string> failures)
    {
        if (!row.TryGetValue(column, out var actual) || actual != expected)
            failures.Add($"{label} {column} is '{actual}', expected '{expected}'.");
    }

    private static void AssertCustomStrRef(string value, string label, ICollection<string> failures)
    {
        if (!int.TryParse(value, out var strRef) || strRef < CustomTlkOffset)
            failures.Add($"{label} should be a custom TLK strref, found '{value}'.");
    }

    private static void AssertGameplayIconTga(string path, string label, ICollection<string> failures)
    {
        if (!File.Exists(path))
        {
            failures.Add($"{label} should have a generated TGA at {path}.");
            return;
        }

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 18)
        {
            failures.Add($"{label} TGA should have a header.");
            return;
        }

        var width = bytes[12] + (bytes[13] << 8);
        var height = bytes[14] + (bytes[15] << 8);
        if (width != 32)
            failures.Add($"{label} TGA width is {width}, expected 32.");
        if (height != 32)
            failures.Add($"{label} TGA height is {height}, expected 32.");
        if ((bytes[17] & 32) != 0)
            failures.Add($"{label} TGA should use bottom-left origin.");

        if (bytes[16] != 32)
            return;

        for (var offset = 18; offset < bytes.Length; offset += 4)
        {
            if (bytes[offset + 3] == 255)
                continue;

            failures.Add($"{label} TGA should be fully opaque.");
            return;
        }
    }

    private sealed record PerkRecord(PerkType Type, PerkDetail Detail);
}
