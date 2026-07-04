using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

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
        }

        expectedColumnsBySheet.Keys
            .Except(worksheetsByName.Keys, StringComparer.Ordinal)
            .Should()
            .BeEmpty("the layout manifest should only reference workbook tabs");
        expectedColumnsBySheet.Should().HaveCount(worksheetsByName.Count, "every workbook tab should have explicit layout columns");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
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
