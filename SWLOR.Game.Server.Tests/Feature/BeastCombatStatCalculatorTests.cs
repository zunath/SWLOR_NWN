using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Feature;

public class BeastCombatStatCalculatorTests
{
    private const int BeastLevelsHeaderRow = 4;

    [Test]
    public void StonecladBehemoth_StableTotalsMatchTheSummonedBeast()
    {
        var detail = BuildAllBeasts()[BeastType.StonecladBehemoth];
        var level = detail.Levels[50];
        var beast = new Beast
        {
            Level = 50,
            AttackPurity = 99,
            AccuracyPurity = 58,
            EvasionPurity = 68,
        };
        beast.DefensePurities[CombatDamageType.Physical] = 100;
        beast.DefensePurities[CombatDamageType.Force] = 74;

        BeastCombatStatCalculator.CalculateAttack(detail, level, beast).Should().Be(137);
        BeastCombatStatCalculator.CalculateAccuracy(detail, level, beast).Should().Be(136);
        BeastCombatStatCalculator.CalculateEvasion(level, beast).Should().Be(144);
        BeastCombatStatCalculator.CalculateDefense(level, beast, CombatDamageType.Physical).Should().Be(154);
        BeastCombatStatCalculator.CalculateDefense(level, beast, CombatDamageType.Force).Should().Be(133);
    }

    [Test]
    public void BeastLevelsBible_TotalsMatchRuntimeFormulasAtFullPurity()
    {
        var beasts = BuildAllBeasts();
        var failures = new List<string>();
        var checkedRows = 0;

        foreach (var (rowNumber, row) in ReadBeastLevelsRows())
        {
            if (!Enum.TryParse<BeastType>(row["Enum"], out var beastType) ||
                !beasts.TryGetValue(beastType, out var detail))
            {
                failures.Add($"row {rowNumber}: unknown beast enum '{row["Enum"]}'");
                continue;
            }

            var beastLevel = ParseNumber(row["Level"]);
            var level = detail.Levels[beastLevel];
            var beast = new Beast
            {
                Level = beastLevel,
                AttackPurity = 100,
                AccuracyPurity = 100,
                EvasionPurity = 100,
            };
            beast.DefensePurities[CombatDamageType.Physical] = 100;
            beast.DefensePurities[CombatDamageType.Force] = 100;

            var expected = new Dictionary<string, int>
            {
                ["Attack"] = BeastCombatStatCalculator.CalculateAttack(detail, level, beast),
                ["Physical DEF"] = BeastCombatStatCalculator.CalculateDefense(level, beast, CombatDamageType.Physical),
                ["Force DEF"] = BeastCombatStatCalculator.CalculateDefense(level, beast, CombatDamageType.Force),
                ["Accuracy"] = BeastCombatStatCalculator.CalculateAccuracy(detail, level, beast),
                ["Evasion"] = BeastCombatStatCalculator.CalculateEvasion(level, beast),
            };

            foreach (var (header, value) in expected)
            {
                var bible = ParseNumber(row[header]);
                if (bible != value)
                    failures.Add($"row {rowNumber} {detail.Name} L{beastLevel} {header}: Bible {bible}, game {value}");
            }

            checkedRows++;
        }

        checkedRows.Should().Be(beasts.Count * 50, "every beast level should have a Beast Levels row");
        failures.Should().BeEmpty("Beast Levels totals must match the stats a summoned beast has at 100% purity");
    }

    private static Dictionary<BeastType, BeastDetail> BuildAllBeasts()
    {
        var beasts = new Dictionary<BeastType, BeastDetail>();
        var definitionTypes = typeof(IBeastListDefinition).Assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
            {
                beasts.Add(beastType, detail);
            }
        }

        return beasts;
    }

    private static List<(int RowNumber, Dictionary<string, string> Values)> ReadBeastLevelsRows()
    {
        var workbookPath = Path.Combine(FindRepositoryRoot().FullName, "design", "bible", "SWLOR Design Bible.xlsx");
        using var stream = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sharedStrings = ReadSharedStrings(archive, ns);
        var worksheet = ReadWorksheetByName(archive, "Beast Levels");

        var cellsByRow = new Dictionary<int, Dictionary<string, string>>();
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

            if (!cellsByRow.TryGetValue(rowNumber, out var cells))
                cellsByRow[rowNumber] = cells = new Dictionary<string, string>();
            cells[column] = value;
        }

        var headersByColumn = cellsByRow[BeastLevelsHeaderRow]
            .ToDictionary(header => header.Key, header => header.Value.Trim());

        var rows = new List<(int RowNumber, Dictionary<string, string> Values)>();
        foreach (var (rowNumber, cells) in cellsByRow.Where(row => row.Key > BeastLevelsHeaderRow).OrderBy(row => row.Key))
        {
            var values = headersByColumn
                .GroupBy(header => header.Value)
                .ToDictionary(
                    group => group.Key,
                    group => cells.TryGetValue(group.First().Key, out var value) ? value : string.Empty);
            if (!string.IsNullOrWhiteSpace(values["NPC Name"]))
                rows.Add((rowNumber, values));
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

    private static int ParseNumber(string text)
    {
        return (int)decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
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
