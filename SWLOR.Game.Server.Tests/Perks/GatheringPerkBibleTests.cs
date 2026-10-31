using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>Keeps the Gathering tab of the Design Bible in step with the Prospector and Treasure Seeker perks the game registers.</summary>
public class GatheringPerkBibleTests
{
    private static readonly string[] Lines = { "Prospector", "Treasure Seeker" };

    private static readonly string[] Roman = { "I", "II", "III" };

    [Test]
    public void EveryPerkLevelHasItsBibleRowWithTheCodesPriceRankTypeAndDescription()
    {
        var rows = ReadGatheringRows().Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();
        // The space revamp's Space Industry perks share the Gathering definition; their rows are another line's.
        var perks = new GatheringPerkDefinition().BuildPerks()
            .Where(pair => GatheringPerkLines.Prospector.Concat(GatheringPerkLines.TreasureSeeker).Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var failures = new List<string>();
        var expectedRows = 0;

        foreach (var (type, detail) in perks.OrderBy(pair => pair.Key))
        {
            foreach (var (level, perkLevel) in detail.PerkLevels.OrderBy(pair => pair.Key))
            {
                expectedRows++;
                var name = detail.PerkLevels.Count > 1 ? $"{detail.Name} {Roman[level - 1]}" : detail.Name;
                var matches = rows.Where(row => row.GetValueOrDefault("C") == name).ToList();
                if (matches.Count != 1)
                {
                    failures.Add($"{type} level {level}: expected one Bible row named '{name}', found {matches.Count}.");
                    continue;
                }

                var row = matches[0];
                var rank = perkLevel.Requirements.OfType<PerkRequirementSkill>().Single().RequiredRank;
                var isStyle = type is PerkType.StripMiningStyle or PerkType.CarefulHandsStyle;
                Compare(failures, name, "SP Price", decimal.Parse(row["B"], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), perkLevel.Price.ToString(CultureInfo.InvariantCulture));
                Compare(failures, name, "Skill Reqs.", row["D"], $"Gathering {rank}");
                Compare(failures, name, "Type", row["F"], isStyle ? "Style" : "Trait");
                Compare(failures, name, "Description", row["G"], perkLevel.Description);
                Compare(failures, name, "Dev Status", row["O"], "Implemented");
                Compare(failures, name, "Line", row["A"], GatheringPerkLines.Prospector.Contains(type) ? "Prospector" : "Treasure Seeker");
            }
        }

        rows.Should().HaveCount(expectedRows, "the Bible holds one row per purchasable perk level and no other Prospector or Treasure Seeker rows");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void TheGatheringTabTotalsTheTwoLinesAtFiftySp_AndRetiredRowsStayOutOfIt()
    {
        var rows = ReadGatheringRows().ToList();
        var perkRows = rows.Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();

        perkRows.Sum(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)).Should().Be(50m);
        foreach (var line in Lines)
            perkRows.Where(row => row["A"] == line).Sum(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)).Should().Be(25m, line);

        var header = rows.Single(row => row.GetValueOrDefault("C") == "Total SP");
        decimal.Parse(header["D"], CultureInfo.InvariantCulture).Should().Be(50m, "the cached total counts the purchasable rows and not the retired or moved ones");
    }

    private static void Compare(List<string> failures, string name, string column, string actual, string expected)
    {
        if (actual != expected)
            failures.Add($"{name}: Bible {column} is '{actual}' but the code has '{expected}'.");
    }

    private static IReadOnlyList<Dictionary<string, string>> ReadGatheringRows()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        using var archive = ZipFile.OpenRead(Path.Combine(directory!.FullName, "design", "bible", "SWLOR Design Bible.xlsx"));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument ReadXml(string path)
        {
            using var stream = archive.GetEntry(path)!.Open();
            return XDocument.Load(stream);
        }

        var sheet = ReadXml("xl/workbook.xml").Descendants(ns + "sheet").Single(element => (string)element.Attribute("name") == "Gathering");
        var relationshipId = (string)sheet.Attribute(relationshipNs + "id")!;
        var target = (string)ReadXml("xl/_rels/workbook.xml.rels").Root!.Elements()
            .Single(element => (string)element.Attribute("Id") == relationshipId).Attribute("Target")!;
        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        var sharedStrings = ReadXml("xl/sharedStrings.xml").Root!.Elements(ns + "si")
            .Select(element => string.Concat(element.Descendants(ns + "t").Select(text => text.Value))).ToArray();

        return ReadXml(path).Descendants(ns + "row")
            .Select(row => row.Elements(ns + "c").ToDictionary(
                cell => new string(((string)cell.Attribute("r")!).TakeWhile(char.IsLetter).ToArray()),
                cell => (string)cell.Attribute("t") switch
                {
                    "s" => sharedStrings[int.Parse(cell.Element(ns + "v")!.Value, CultureInfo.InvariantCulture)],
                    "inlineStr" => string.Concat(cell.Descendants(ns + "t").Select(text => text.Value)),
                    _ => cell.Element(ns + "v")?.Value ?? string.Empty
                }))
            .ToList();
    }
}
