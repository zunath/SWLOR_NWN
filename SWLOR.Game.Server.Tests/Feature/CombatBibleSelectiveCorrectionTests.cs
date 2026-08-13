using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class CombatBibleSelectiveCorrectionTests
{
    private static readonly XNamespace SpreadsheetNs =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace DocumentRelationshipNs =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNs =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    [Test]
    public void SelectivePerkMode_ChangesOnlyOneSelectedPerk()
    {
        var workbook = CopyBibleToTemporaryFile();
        try
        {
            SetPerkDescription(workbook, "Lightsaber", "Force Sheath II", "single selection sentinel");
            SetPerkDescription(workbook, "Lightsaber", "Force Sheath III", "unselected sentinel");
            var characterStatsBefore = ReadSheetXml(workbook, "Character Stats");
            var auditBefore = ReadSheetXml(workbook, "Combat Balance Findings");

            var result = RunCorrection(workbook, new[] { "Force Sheath II" });

            result.ExitCode.Should().Be(0, result.Error);
            ReadPerkDescription(workbook, "Lightsaber", "Force Sheath II")
                .Should().Be("On your next hit, deal + 17 Force DMG.");
            ReadPerkDescription(workbook, "Lightsaber", "Force Sheath III")
                .Should().Be("unselected sentinel");
            ReadSheetXml(workbook, "Character Stats").Should().Be(characterStatsBefore);
            ReadSheetXml(workbook, "Combat Balance Findings").Should().Be(auditBefore);
        }
        finally
        {
            File.Delete(workbook);
        }
    }

    [Test]
    public void SelectivePerkMode_ChangesOnlyMultipleSelectedPerks()
    {
        var workbook = CopyBibleToTemporaryFile();
        try
        {
            SetPerkDescription(workbook, "Lightsaber", "Force Sheath II", "selected two sentinel");
            SetPerkDescription(workbook, "Lightsaber", "Force Sheath III", "selected three sentinel");
            SetPerkDescription(workbook, "Lightsaber", "Force Sheath IV", "unselected four sentinel");
            var characterStatsBefore = ReadSheetXml(workbook, "Character Stats");
            var auditBefore = ReadSheetXml(workbook, "Combat Balance Findings");

            var result = RunCorrection(workbook, new[] { "Force Sheath II", "Force Sheath III" });

            result.ExitCode.Should().Be(0, result.Error);
            ReadPerkDescription(workbook, "Lightsaber", "Force Sheath II")
                .Should().Be("On your next hit, deal + 17 Force DMG.");
            ReadPerkDescription(workbook, "Lightsaber", "Force Sheath III")
                .Should().Be("On your next hit, deal + 23 Force DMG.");
            ReadPerkDescription(workbook, "Lightsaber", "Force Sheath IV")
                .Should().Be("unselected four sentinel");
            ReadSheetXml(workbook, "Character Stats").Should().Be(characterStatsBefore);
            ReadSheetXml(workbook, "Combat Balance Findings").Should().Be(auditBefore);
        }
        finally
        {
            File.Delete(workbook);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SelectivePerkMode_InvalidOrConflictingArgumentsDoNotModifyWorkbook(bool conflictingModes)
    {
        var workbook = CopyBibleToTemporaryFile();
        try
        {
            var before = File.ReadAllBytes(workbook);
            var result = conflictingModes
                ? RunCorrection(workbook, new[] { "Force Sheath II" }, espionageStealthOnly: true)
                : RunCorrection(workbook, new[] { "Not A Real Perk" });

            result.ExitCode.Should().NotBe(0);
            File.ReadAllBytes(workbook).Should().Equal(before,
                "argument validation must fail before the original workbook is replaced");
        }
        finally
        {
            File.Delete(workbook);
        }
    }

    private static string CopyBibleToTemporaryFile()
    {
        var root = FindRepositoryRoot();
        var source = Path.Combine(root.FullName, "design", "bible", "SWLOR Design Bible - Combat Upgrade.xlsx");
        var destination = Path.Combine(Path.GetTempPath(), $"swlor-selective-bible-{Guid.NewGuid():N}.xlsx");
        File.Copy(source, destination);
        return destination;
    }

    private static (int ExitCode, string Output, string Error) RunCorrection(
        string workbook,
        IReadOnlyCollection<string> perkNames,
        bool espionageStealthOnly = false)
    {
        var root = FindRepositoryRoot();
        var script = Path.Combine(root.FullName, "tools", "ApplyCombatBibleReviewFixes.ps1");
        static string Quote(string value) => $"'{value.Replace("'", "''")}'";

        var selected = string.Join(",", perkNames.Select(Quote));
        var command = $"& {Quote(script)} -WorkbookPath {Quote(workbook)}";
        if (espionageStealthOnly)
            command += " -EspionageStealthOnly";
        if (perkNames.Count > 0)
            command += $" -OnlyPerkName @({selected})";

        var startInfo = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return (process.ExitCode, output, error);
    }

    private static string ReadPerkDescription(string workbook, string sheetName, string perkName)
    {
        using var zip = ZipFile.OpenRead(workbook);
        var sheet = ReadSheet(zip, sheetName);
        var sharedStrings = ReadSharedStrings(zip);
        var (_, columns) = FindHeader(sheet, sharedStrings);
        var perkRow = FindPerkRow(sheet, sharedStrings, columns["Perk Name"], perkName);
        return ReadCellText(FindCell(perkRow, columns["Description"]), sharedStrings);
    }

    private static void SetPerkDescription(string workbook, string sheetName, string perkName, string description)
    {
        using var zip = ZipFile.Open(workbook, ZipArchiveMode.Update);
        var entryPath = GetSheetEntryPath(zip, sheetName);
        var sheet = ReadXml(zip, entryPath);
        var sharedStrings = ReadSharedStrings(zip);
        var (_, columns) = FindHeader(sheet, sharedStrings);
        var perkRow = FindPerkRow(sheet, sharedStrings, columns["Perk Name"], perkName);
        var cell = FindCell(perkRow, columns["Description"]);

        cell.RemoveNodes();
        cell.SetAttributeValue("t", "inlineStr");
        cell.Add(new XElement(
            SpreadsheetNs + "is",
            new XElement(SpreadsheetNs + "t", description)));

        ReplaceEntry(zip, entryPath, sheet.ToString(SaveOptions.DisableFormatting));
    }

    private static string ReadSheetXml(string workbook, string sheetName)
    {
        using var zip = ZipFile.OpenRead(workbook);
        return ReadEntry(zip, GetSheetEntryPath(zip, sheetName));
    }

    private static XDocument ReadSheet(ZipArchive zip, string sheetName)
    {
        return ReadXml(zip, GetSheetEntryPath(zip, sheetName));
    }

    private static string GetSheetEntryPath(ZipArchive zip, string sheetName)
    {
        var workbook = ReadXml(zip, "xl/workbook.xml");
        var relationships = ReadXml(zip, "xl/_rels/workbook.xml.rels")
            .Root!
            .Elements(PackageRelationshipNs + "Relationship")
            .ToDictionary(
                relationship => (string)relationship.Attribute("Id")!,
                relationship => NormalizeWorkbookTarget((string)relationship.Attribute("Target")!));

        var sheet = workbook
            .Descendants(SpreadsheetNs + "sheet")
            .Single(element => (string)element.Attribute("name")! == sheetName);
        return relationships[(string)sheet.Attribute(DocumentRelationshipNs + "id")!];
    }

    private static string NormalizeWorkbookTarget(string target)
    {
        var normalized = target.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("xl/", StringComparison.Ordinal)
            ? normalized
            : $"xl/{normalized}";
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return new List<string>();

        var document = ReadXml(zip, "xl/sharedStrings.xml");
        return document
            .Descendants(SpreadsheetNs + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNs + "t").Select(text => text.Value)))
            .ToList();
    }

    private static (XElement Row, Dictionary<string, string> Columns) FindHeader(
        XDocument sheet,
        IReadOnlyList<string> sharedStrings)
    {
        foreach (var row in sheet.Descendants(SpreadsheetNs + "row"))
        {
            var columns = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var cell in row.Elements(SpreadsheetNs + "c"))
            {
                var header = ReadCellText(cell, sharedStrings);
                if (!string.IsNullOrWhiteSpace(header))
                    columns[header] = GetColumn((string)cell.Attribute("r")!);
            }

            if (columns.ContainsKey("Perk Name"))
                return (row, columns);
        }

        throw new InvalidOperationException("Perk Name header was not found.");
    }

    private static XElement FindPerkRow(
        XDocument sheet,
        IReadOnlyList<string> sharedStrings,
        string perkColumn,
        string perkName)
    {
        return sheet
            .Descendants(SpreadsheetNs + "row")
            .Single(row => row
                .Elements(SpreadsheetNs + "c")
                .Where(cell => GetColumn((string)cell.Attribute("r")!) == perkColumn)
                .Any(cell => ReadCellText(cell, sharedStrings) == perkName));
    }

    private static XElement FindCell(XElement row, string column)
    {
        return row
            .Elements(SpreadsheetNs + "c")
            .Single(cell => GetColumn((string)cell.Attribute("r")!) == column);
    }

    private static string ReadCellText(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string)cell.Attribute("t");
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(text => text.Value));

        var raw = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;
        return type == "s" && int.TryParse(raw, out var index)
            ? sharedStrings[index]
            : raw;
    }

    private static string GetColumn(string reference)
    {
        return new string(reference.TakeWhile(char.IsLetter).ToArray());
    }

    private static XDocument ReadXml(ZipArchive zip, string entryPath)
    {
        return XDocument.Parse(ReadEntry(zip, entryPath), LoadOptions.PreserveWhitespace);
    }

    private static string ReadEntry(ZipArchive zip, string entryPath)
    {
        using var reader = new StreamReader(zip.GetEntry(entryPath)!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static void ReplaceEntry(ZipArchive zip, string entryPath, string content)
    {
        zip.GetEntry(entryPath)!.Delete();
        var replacement = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
