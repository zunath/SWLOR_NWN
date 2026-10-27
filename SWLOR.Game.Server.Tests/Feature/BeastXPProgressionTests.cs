using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class BeastXPProgressionTests
{
    private const string XPChartSheet = "XP Chart";
    private const string BeastXPRequiredHeader = "Beast XP Required";
    private const string BeastCumulativeHeader = "Beast Cumulative";

    [Test]
    public void BeastXPRequirement_IsTwiceTheSkillRequirementAtEveryLevel()
    {
        for (var level = 0; level < BeastMastery.MaxLevel; level++)
        {
            BeastMastery.GetRequiredXP(level, 0).Should().Be(Skill.GetRequiredXP(level) * 2, $"level {level}");
        }
    }

    [Test]
    public void BeastXPRequirement_AddsTheXPPenaltyPercent()
    {
        BeastMastery.GetRequiredXP(10, 50).Should().Be((int)(Skill.GetRequiredXP(10) * 2 * 1.5f));
    }

    [TestCase(0, 0)]
    [TestCase(1, 10)]
    [TestCase(3, 30)]
    [TestCase(5, 50)]
    [TestCase(6, 50)]
    public void BeastLevelCap_IsTenLevelsPerTameRankUpToMaxLevel(int tamePerkLevel, int expectedCap)
    {
        BeastMastery.GetBeastLevelCap(tamePerkLevel).Should().Be(expectedCap);
    }

    [Test]
    public void MaxDistributableXP_FillsEveryLevelUpToTheCap()
    {
        var beast = new Beast { Level = 5, XP = 300, XPPenaltyPercent = 0 };

        var expected = Enumerable.Range(5, 5).Sum(level => BeastMastery.GetRequiredXP(level, 0)) - 300;

        BeastMastery.GetMaxDistributableXP(beast, 10).Should().Be(expected);
    }

    [Test]
    public void MaxDistributableXP_UsesTheBeastXPPenalty()
    {
        var beast = new Beast { Level = 20, XP = 0, XPPenaltyPercent = 25 };

        BeastMastery.GetMaxDistributableXP(beast, 21).Should().Be(BeastMastery.GetRequiredXP(20, 25));
    }

    [TestCase(30, 30)]
    [TestCase(35, 30)]
    [TestCase(50, 50)]
    public void MaxDistributableXP_IsZeroAtOrAboveTheCap(int level, int levelCap)
    {
        var beast = new Beast { Level = level, XP = 100 };

        BeastMastery.GetMaxDistributableXP(beast, levelCap).Should().Be(0);
    }

    [Test]
    public void MaxDistributableXP_NeverPassesMaxLevel()
    {
        var beast = new Beast { Level = 49, XP = 0 };

        BeastMastery.GetMaxDistributableXP(beast, 60).Should().Be(BeastMastery.GetRequiredXP(49, 0));
    }

    [Test]
    public void Migration_SetsBeastsOverTheNewRequirementOnePointShortOfTheNextLevel()
    {
        var beast = new Beast { Level = 30, XP = 40000, XPPenaltyPercent = 20 };

        _29_CorrectStoredCarapaceBracers.ClampBeastXP(beast).Should().BeTrue();

        beast.Level.Should().Be(30, "the migration never levels a beast up");
        beast.XP.Should().Be(BeastMastery.GetRequiredXP(30, 20) - 1, "the extra XP is not refunded");
        _29_CorrectStoredCarapaceBracers.ClampBeastXP(beast).Should().BeFalse("the migration can rerun safely");
    }

    [Test]
    public void Migration_LeavesBeastsUnderTheNewRequirementAlone()
    {
        var beast = new Beast { Level = 30, XP = BeastMastery.GetRequiredXP(30, 0) - 1 };

        _29_CorrectStoredCarapaceBracers.ClampBeastXP(beast).Should().BeFalse();
        beast.XP.Should().Be(BeastMastery.GetRequiredXP(30, 0) - 1);
    }

    [Test]
    public void Migration_LeavesMaxLevelBeastsAlone()
    {
        var beast = new Beast { Level = BeastMastery.MaxLevel, XP = 0 };

        _29_CorrectStoredCarapaceBracers.ClampBeastXP(beast).Should().BeFalse();
        beast.XP.Should().Be(0);
    }

    [Test]
    public void CombatXP_GivesBeastsTheAreaXPBonusBeforeAwarding()
    {
        var source = ReadServerSource("Service", "BeastMastery.cs");
        var start = source.IndexOf("public static void CombatPointXPDistributed()", StringComparison.Ordinal);
        start.Should().BeGreaterThan(-1);
        var method = source[start..source.IndexOf("GiveBeastXP(beast, xp, false);", start, StringComparison.Ordinal)];

        method.Should().Contain("\"AREA_XP_BONUS_PERCENTAGE\"");
        method.Should().Contain("xp += (int)(xp * areaBonus);");
    }

    [Test]
    public void DesignBibleXPChart_MatchesTheBeastXPRequirements()
    {
        var columns = ReadXPChartColumns();
        var levels = columns["Level"];
        var required = columns[BeastXPRequiredHeader];
        var cumulative = columns[BeastCumulativeHeader];

        var total = 0;
        for (var level = 1; level <= BeastMastery.MaxLevel; level++)
        {
            var row = levels.Single(entry => ParseNumber(entry.Value) == level).Key;
            var expected = BeastMastery.GetRequiredXP(level - 1, 0);
            total += expected;

            ParseNumber(required[row]).Should().Be(expected, $"Bible beast XP for level {level}");
            ParseNumber(cumulative[row]).Should().Be(total, $"Bible beast cumulative XP for level {level}");
        }
    }

    private static Dictionary<string, Dictionary<int, string>> ReadXPChartColumns()
    {
        var workbookPath = Path.Combine(FindRepositoryRoot().FullName, "design", "bible", "SWLOR Design Bible.xlsx");
        using var stream = new FileStream(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sharedStrings = ReadSharedStrings(archive, ns);
        var worksheet = ReadWorksheetByName(archive, XPChartSheet);

        var cellsByColumn = new Dictionary<string, Dictionary<int, string>>();
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

            if (!cellsByColumn.TryGetValue(column, out var cells))
                cellsByColumn[column] = cells = new Dictionary<int, string>();
            cells[rowNumber] = value;
        }

        return cellsByColumn
            .Where(column => column.Value.TryGetValue(1, out var header) && !string.IsNullOrWhiteSpace(header))
            .ToDictionary(
                column => column.Value[1].Trim(),
                column => column.Value
                    .Where(cell => cell.Key > 1 && !string.IsNullOrWhiteSpace(cell.Value))
                    .ToDictionary(cell => cell.Key, cell => cell.Value));
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

    private static string ReadServerSource(params string[] pathParts)
    {
        var path = Path.Combine(new[] { FindRepositoryRoot().FullName, "SWLOR.Game.Server" }.Concat(pathParts).ToArray());
        return Regex.Replace(File.ReadAllText(path), "\r\n", "\n");
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
