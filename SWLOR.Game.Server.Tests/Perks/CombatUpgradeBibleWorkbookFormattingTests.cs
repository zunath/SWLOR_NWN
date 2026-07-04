using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

public class CombatUpgradeBibleWorkbookFormattingTests
{
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
