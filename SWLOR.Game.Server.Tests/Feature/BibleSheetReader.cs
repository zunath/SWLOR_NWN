using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Reads the cached cell values of one sheet of the Design Bible workbook without Excel.
/// Rows are keyed by row number; each row maps column letters (A, B, ...) to the cell text.
/// </summary>
public static class BibleSheetReader
{
    public static SortedDictionary<int, Dictionary<string, string>> ReadRows(string sheetName)
    {
        var workbookPath = Path.Combine(FindRepositoryRoot().FullName, "design", "bible", "SWLOR Design Bible.xlsx");
        using var stream = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sharedStrings = ReadSharedStrings(archive, ns);
        var worksheet = ReadWorksheetByName(archive, sheetName);

        var rows = new SortedDictionary<int, Dictionary<string, string>>();
        foreach (var cell in worksheet.Descendants(ns + "c"))
        {
            var address = cell.Attribute("r")!.Value;
            var column = new string(address.TakeWhile(char.IsLetter).ToArray());
            var rowNumber = int.Parse(address[column.Length..], CultureInfo.InvariantCulture);
            var type = cell.Attribute("t")?.Value;
            var value = type == "inlineStr"
                ? string.Concat(cell.Descendants(ns + "t").Select(node => node.Value))
                : cell.Element(ns + "v")?.Value ?? string.Empty;
            if (type == "s")
                value = sharedStrings[int.Parse(value, CultureInfo.InvariantCulture)];

            if (!rows.TryGetValue(rowNumber, out var cells))
                rows[rowNumber] = cells = new Dictionary<string, string>();
            cells[column] = value;
        }

        return rows;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive, XNamespace ns)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        using var stream = entry.Open();
        return XDocument.Load(stream)
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        XDocument Load(string entryName)
        {
            using var stream = archive.GetEntry(entryName)!.Open();
            return XDocument.Load(stream);
        }

        var relationshipId = Load("xl/workbook.xml")
            .Descendants(workbookNs + "sheet")
            .Single(sheet => sheet.Attribute("name")?.Value == sheetName)
            .Attribute(relationshipNs + "id")!
            .Value;
        var target = Load("xl/_rels/workbook.xml.rels")
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(relationship => relationship.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")!
            .Value
            .Replace('\\', '/');

        return Load(target.StartsWith("/", StringComparison.Ordinal) ? target.TrimStart('/') : $"xl/{target}");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
