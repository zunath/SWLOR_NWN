using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatUpgradeBibleWorkbookFormattingTests
{
    private const decimal MinimumNotesColumnWidth = 45m;
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    [Test]
    public void CombatUpgradeBibleWorkbook_UsesCompactReadableFormatting()
    {
        var root = FindRepositoryRoot();
        var workbookPath = Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx");
        var layoutPath = Path.Combine(root.FullName, "tools", "CombatUpgradeBibleWorkbookLayout.json");

        using var archive = ZipFile.OpenRead(workbookPath);
        var worksheetsByName = ReadWorksheetsByName(archive);
        var sharedStrings = ReadSharedStrings(archive);
        var expectedColumnsBySheet = ReadLayoutColumns(layoutPath);
        var failures = new List<string>();

        foreach (var (sheetName, entryName) in worksheetsByName)
        {
            var worksheet = ReadWorkbookXml(archive, entryName);
            AssertNoCustomRowHeights(sheetName, worksheet, failures);

            if (!expectedColumnsBySheet.TryGetValue(sheetName, out var expectedColumns))
            {
                failures.Add($"{sheetName}: missing column-width layout entry in {layoutPath}.");
                continue;
            }

            AssertColumns(sheetName, worksheet, expectedColumns, failures);
            AssertNotesColumnsReadable(sheetName, worksheet, expectedColumns, sharedStrings, failures);
            AssertBeastCalcsColumnsCondensed(sheetName, expectedColumns, failures);
            AssertForceColumnsAligned(sheetName, expectedColumns, failures);
        }

        expectedColumnsBySheet.Keys
            .Except(worksheetsByName.Keys, StringComparer.Ordinal)
            .Should()
            .BeEmpty("the layout manifest should only reference workbook tabs");
        expectedColumnsBySheet.Should().HaveCount(worksheetsByName.Count, "every workbook tab should have explicit layout columns");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void ForceAndDevices_StoreSkillPointPricesAsNumericCells()
    {
        var root = FindRepositoryRoot();
        var workbookPath = Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx");

        using var archive = ZipFile.OpenRead(workbookPath);
        var worksheetsByName = ReadWorksheetsByName(archive);
        var sharedStrings = ReadSharedStrings(archive);
        var failures = new List<string>();

        foreach (var sheetName in new[] { "Force", "Devices" })
        {
            var worksheet = ReadWorkbookXml(archive, worksheetsByName[sheetName]);
            var rows = worksheet.Descendants(SpreadsheetNs + "row").ToArray();
            var headerRow = rows.Single(row => row
                .Elements(SpreadsheetNs + "c")
                .Any(cell => GetCellText(cell, sharedStrings) == "Perk Name"));
            var headers = headerRow
                .Elements(SpreadsheetNs + "c")
                .Where(cell => !string.IsNullOrWhiteSpace(GetCellText(cell, sharedStrings)))
                .ToDictionary(
                    cell => GetCellText(cell, sharedStrings),
                    cell => new string(((string)cell.Attribute("r")!).TakeWhile(char.IsLetter).ToArray()));

            foreach (var row in rows.Where(row => row != headerRow))
            {
                var rowNumber = (string)row.Attribute("r")!;
                var nameCell = row.Elements(SpreadsheetNs + "c")
                    .SingleOrDefault(cell => (string)cell.Attribute("r")! == $"{headers["Perk Name"]}{rowNumber}");
                var perkName = nameCell == null ? string.Empty : GetCellText(nameCell, sharedStrings);
                if (string.IsNullOrWhiteSpace(perkName))
                    continue;

                var priceCell = row.Elements(SpreadsheetNs + "c")
                    .SingleOrDefault(cell => (string)cell.Attribute("r")! == $"{headers["SP Price"]}{rowNumber}");
                if (priceCell == null || priceCell.Attribute("t") != null ||
                    !decimal.TryParse(
                        GetCellText(priceCell, sharedStrings),
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out _))
                {
                    failures.Add($"{sheetName} row {rowNumber} ({perkName}): SP Price must be a numeric cell.");
                }
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void CharacterStats_DocumentsRuntimeCombatLimits()
    {
        var root = FindRepositoryRoot();
        var workbookPath = Path.Combine(
            root.FullName,
            "design",
            "bible",
            "SWLOR Design Bible - Combat Upgrade.xlsx");

        using var archive = ZipFile.OpenRead(workbookPath);
        var worksheetEntry = ReadWorksheetsByName(archive)["Character Stats"];
        var worksheet = ReadWorkbookXml(archive, worksheetEntry);
        var sharedStrings = ReadSharedStrings(archive);
        var stats = worksheet
            .Descendants(SpreadsheetNs + "row")
            .Select(row => row
                .Elements(SpreadsheetNs + "c")
                .ToDictionary(
                    cell => new string(((string)cell.Attribute("r") ?? string.Empty).TakeWhile(char.IsLetter).ToArray()),
                    cell => GetCellText(cell, sharedStrings)))
            .Where(row => row.TryGetValue("A", out var name) && !string.IsNullOrWhiteSpace(name))
            .ToDictionary(row => row["A"], StringComparer.Ordinal);

        AssertStatRange(stats, "Combat Readiness", 0, Stat.MaximumCombatReadinessPercent);
        AssertStatRange(stats, "Shield Deflection", 0, Stat.MaximumShieldDeflectionChance);
        AssertStatRange(stats, "Melee / Ranged Deflection", 0, Stat.MaximumDeflectionChanceCap);
        AssertStatRange(stats, "Guard", 0, Stat.MaximumGuardChance);
        AssertStatRange(stats, "Critical Rate", Combat.MinimumCriticalRate, Combat.MaximumCriticalRate);
        AssertStatRange(stats, "Critical Damage", 0, Combat.MaximumCriticalDamagePercentAdjustment);
        AssertStatRange(
            stats,
            "Enmity",
            Enmity.MinimumEnmityPercentAdjustment,
            Enmity.MaximumEnmityPercentAdjustment);
        AssertStatRange(stats, "Haste", 0, Combat.MaximumAttackDelayAdjustmentPercent);
        AssertStatRange(stats, "Slow", 0, Combat.MaximumAttackDelayAdjustmentPercent);
        AssertStatRange(
            stats,
            "Movement Speed",
            (decimal)Stat.MinimumMovementSpeedMultiplier,
            (decimal)Stat.MaximumMovementSpeedMultiplier);
        AssertStatRange(
            stats,
            "Damage-Derived Healing per Hit",
            0,
            Combat.MaximumDamageDerivedHealingPercentPerHit);
        AssertStatRange(stats, "Hit Rate", Combat.MinimumHitRate, Combat.MaximumHitRate);
        AssertStatRange(stats, "Damage Bonus per Hit", 0, Combat.MaximumDamageBonusPercent);
        AssertStatRange(
            stats,
            "Single Damage Reduction Modifier",
            0,
            Combat.MaximumNormalDamageReductionPercent);
        AssertStatRange(
            stats,
            "Combined Damage Reduction per Hit",
            0,
            Combat.MaximumCombinedDamageReductionPercent);

        stats["Melee / Ranged Deflection"]["K"].Should().Contain($"independent default chance cap of {Stat.DefaultMeleeDeflectionChanceCap}%");
        stats["Melee / Ranged Deflection"]["K"].Should().Contain("activated combat abilities or Force powers");
        stats["Shield Deflection"]["K"].Should().Contain("completely replaces Melee and Ranged Deflection");
        stats["Guard"]["K"].Should().Contain($"reduces damage by {Combat.BaseGuardDamageReductionPercent}% by default");
        stats["Guard"]["K"].Should().Contain($"{Combat.MaximumGuardDamageReductionPercent}% hard limit");
        stats["Damage-Derived Healing per Hit"]["K"].Should().Contain("after Combat Readiness and healing-received modifiers");
    }

    private static void AssertStatRange(
        IReadOnlyDictionary<string, Dictionary<string, string>> stats,
        string name,
        decimal expectedMinimum,
        decimal expectedMaximum)
    {
        stats.Should().ContainKey(name);
        var row = stats[name];
        decimal.Parse(row["I"], System.Globalization.CultureInfo.InvariantCulture).Should().Be(expectedMinimum);
        decimal.Parse(row["J"], System.Globalization.CultureInfo.InvariantCulture).Should().Be(expectedMaximum);
    }

    private static void AssertColumns(
        string sheetName,
        XDocument worksheet,
        IReadOnlyList<(int Min, int Max, decimal Width)> expectedColumns,
        List<string> failures)
    {
        var actualColumns = worksheet
            .Descendants(SpreadsheetNs + "cols")
            .Elements(SpreadsheetNs + "col")
            .Select(column => (
                Min: (int?)column.Attribute("min") ?? 0,
                Max: (int?)column.Attribute("max") ?? 0,
                Width: decimal.Parse((string)column.Attribute("width") ?? "0", System.Globalization.CultureInfo.InvariantCulture)))
            .ToArray();

        if (!actualColumns.SequenceEqual(expectedColumns))
        {
            failures.Add($"{sheetName}: expected compact column widths [{FormatColumns(expectedColumns)}] but found [{FormatColumns(actualColumns)}].");
        }
    }

    private static void AssertNoCustomRowHeights(string sheetName, XDocument worksheet, List<string> failures)
    {
        var customRows = worksheet
            .Descendants(SpreadsheetNs + "row")
            .Where(row => row.Attribute("ht") != null || row.Attribute("customHeight") != null)
            .Select(row => (string)row.Attribute("r") ?? "?")
            .Take(10)
            .ToArray();

        if (customRows.Length != 0)
        {
            failures.Add($"{sheetName}: fixed row heights should be removed so Google Sheets can auto-fit visible text. First rows: {string.Join(", ", customRows)}.");
        }
    }

    private static void AssertNotesColumnsReadable(
        string sheetName,
        XDocument worksheet,
        IReadOnlyList<(int Min, int Max, decimal Width)> expectedColumns,
        IReadOnlyList<string> sharedStrings,
        List<string> failures)
    {
        var notesColumns = worksheet
            .Descendants(SpreadsheetNs + "c")
            .Where(cell => GetCellText(cell, sharedStrings).Trim() == "Notes")
            .Select(cell => GetColumnIndex((string)cell.Attribute("r") ?? string.Empty))
            .Where(column => column > 0)
            .Distinct()
            .OrderBy(column => column)
            .ToArray();

        foreach (var notesColumn in notesColumns)
        {
            var matchingColumn = expectedColumns
                .Where(column => column.Min <= notesColumn && column.Max >= notesColumn)
                .Select(column => (Found: true, Width: column.Width))
                .FirstOrDefault();

            if (!matchingColumn.Found)
            {
                failures.Add($"{sheetName}: Notes column {notesColumn} has no width entry in the layout manifest.");
                continue;
            }

            if (matchingColumn.Width < MinimumNotesColumnWidth)
            {
                failures.Add($"{sheetName}: Notes column {notesColumn} width is {matchingColumn.Width}, expected at least {MinimumNotesColumnWidth}.");
            }
        }
    }

    private static void AssertBeastCalcsColumnsCondensed(
        string sheetName,
        IReadOnlyList<(int Min, int Max, decimal Width)> expectedColumns,
        List<string> failures)
    {
        if (sheetName != "Beast Calcs")
            return;

        var expectedWidthsByColumn = new Dictionary<int, decimal>();
        AddExpectedColumns(expectedWidthsByColumn, 1, 1, 18.0m);
        AddExpectedColumns(expectedWidthsByColumn, 2, 2, 9.0m);
        AddExpectedColumns(expectedWidthsByColumn, 3, 10, 4.5m);
        AddExpectedColumns(expectedWidthsByColumn, 11, 16, 6.0m);
        AddExpectedColumns(expectedWidthsByColumn, 17, 17, 10.0m);
        AddExpectedColumns(expectedWidthsByColumn, 18, 18, 15.0m);
        AddExpectedColumns(expectedWidthsByColumn, 19, 19, 6.0m);
        AddExpectedColumns(expectedWidthsByColumn, 20, 25, 6.0m);
        AddExpectedColumns(expectedWidthsByColumn, 26, 27, 7.0m);
        AddExpectedColumns(expectedWidthsByColumn, 28, 28, 26.0m);
        AddExpectedColumns(expectedWidthsByColumn, 29, 35, 8.0m);

        foreach (var (column, expectedWidth) in expectedWidthsByColumn)
        {
            var matchingColumn = expectedColumns
                .Where(width => width.Min <= column && width.Max >= column)
                .Select(width => (Found: true, Width: width.Width))
                .FirstOrDefault();

            if (!matchingColumn.Found)
            {
                failures.Add($"Beast Calcs: condensed column {column} has no width entry in the layout manifest.");
                continue;
            }

            if (matchingColumn.Width != expectedWidth)
            {
                failures.Add($"Beast Calcs: condensed column {column} width is {matchingColumn.Width}, expected {expectedWidth}.");
            }
        }
    }

    private static void AssertForceColumnsAligned(
        string sheetName,
        IReadOnlyList<(int Min, int Max, decimal Width)> expectedColumns,
        List<string> failures)
    {
        if (sheetName != "Force")
            return;

        var expectedWidthsByColumn = new Dictionary<int, decimal>();
        AddExpectedColumns(expectedWidthsByColumn, 7, 7, 12.0m);
        AddExpectedColumns(expectedWidthsByColumn, 8, 8, 12.5m);
        AddExpectedColumns(expectedWidthsByColumn, 9, 9, 92.25m);
        AddExpectedColumns(expectedWidthsByColumn, 18, 18, 20.75m);
        AddExpectedColumns(expectedWidthsByColumn, 19, 19, 45.0m);

        foreach (var (column, expectedWidth) in expectedWidthsByColumn)
        {
            var matchingColumn = expectedColumns
                .Where(width => width.Min <= column && width.Max >= column)
                .Select(width => (Found: true, Width: width.Width))
                .FirstOrDefault();

            if (!matchingColumn.Found)
            {
                failures.Add($"Force: column {column} has no width entry in the layout manifest.");
                continue;
            }

            if (matchingColumn.Width != expectedWidth)
            {
                failures.Add($"Force: column {column} width is {matchingColumn.Width}, expected {expectedWidth}.");
            }
        }
    }

    private static void AddExpectedColumns(Dictionary<int, decimal> expectedWidthsByColumn, int min, int max, decimal width)
    {
        for (var column = min; column <= max; column++)
        {
            expectedWidthsByColumn[column] = width;
        }
    }

    private static Dictionary<string, string> ReadWorksheetsByName(ZipArchive archive)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        var relationshipTargets = relationships
            .Root!
            .Elements()
            .ToDictionary(
                relationship => (string)relationship.Attribute("Id")!,
                relationship => GetWorkbookEntryPath((string)relationship.Attribute("Target")!));

        return workbook
            .Descendants(SpreadsheetNs + "sheet")
            .ToDictionary(
                sheet => (string)sheet.Attribute("name")!,
                sheet =>
                {
                    var relationshipId = (string)sheet.Attribute(RelationshipNs + "id")!;
                    return relationshipTargets[relationshipId];
                });
    }

    private static string GetWorkbookEntryPath(string target)
    {
        if (target.StartsWith('/'))
            return target.TrimStart('/');

        return target.StartsWith("worksheets/", StringComparison.Ordinal)
            ? $"xl/{target}"
            : $"xl/{target}";
    }

    private static string[] ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        using var stream = entry.Open();
        var sharedStrings = XDocument.Load(stream);
        return sharedStrings
            .Descendants(SpreadsheetNs + "si")
            .Select(sharedString => string.Concat(sharedString.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static Dictionary<string, (int Min, int Max, decimal Width)[]> ReadLayoutColumns(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var columnsBySheet = document.RootElement.GetProperty("columnsBySheet");
        return columnsBySheet
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => property.Value
                    .EnumerateArray()
                    .Select(column => (
                        Min: column.GetProperty("min").GetInt32(),
                        Max: column.GetProperty("max").GetInt32(),
                        Width: column.GetProperty("width").GetDecimal()))
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"workbook entry {entryName} should exist");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static string GetCellText(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = (string)cell.Attribute("t") ?? string.Empty;
        if (cellType == "inlineStr")
            return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));

        var rawValue = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
        if (cellType == "s" && int.TryParse(rawValue, out var sharedStringIndex) &&
            sharedStringIndex >= 0 && sharedStringIndex < sharedStrings.Count)
        {
            return sharedStrings[sharedStringIndex];
        }

        return rawValue;
    }

    private static int GetColumnIndex(string cellReference)
    {
        var column = 0;
        foreach (var character in cellReference.TakeWhile(char.IsLetter))
        {
            column = (column * 26) + char.ToUpperInvariant(character) - 'A' + 1;
        }

        return column;
    }

    private static string FormatColumns(IEnumerable<(int Min, int Max, decimal Width)> columns)
    {
        return string.Join(", ", columns.Select(column => $"{column.Min}:{column.Max}:{column.Width}"));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
