using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerkService;
using static SWLOR.Game.Server.Tests.Perks.AgriculturePerkLines;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>Keeps the Agriculture tab of the Design Bible in step with the Chef and Angler perks the game registers.</summary>
public class AgriculturePerkBibleTests
{
    private static readonly string[] Lines = { "Chef", "Angler" };

    private static readonly string[] Roman = { "I", "II", "III" };

    [Test]
    public void EveryPerkLevelHasItsBibleRowWithTheCodesPriceRankAndDescription()
    {
        var rows = ReadAgricultureRows().Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();
        var failures = new List<string>();
        var expectedRows = 0;

        foreach (var (line, perks) in new[] { ("Chef", Chef()), ("Angler", Angler()) })
        {
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
                    Compare(failures, name, "SP Price", decimal.Parse(row["B"], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), perkLevel.Price.ToString(CultureInfo.InvariantCulture));
                    Compare(failures, name, "Skill Reqs.", row["D"], $"Agriculture {rank}");
                    Compare(failures, name, "Description", row["G"], perkLevel.Description);
                    Compare(failures, name, "Dev Status", row["M"], "Implemented");
                    Compare(failures, name, "Line", row["A"], line);
                }
            }
        }

        rows.Should().HaveCount(expectedRows, "the Bible holds one row per purchasable perk level and no other Chef or Angler rows");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void TheStylesAreTypedAsStylesAndTheCapstonesAsCapstones()
    {
        var rows = ReadAgricultureRows().Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();

        foreach (var name in new[] { "Slow Cooking", "Flash Cooking", "Patient Angler", "Aggressive Angler" })
            rows.Single(row => row["C"] == name)["F"].Should().Be("Style", name);
        foreach (var name in new[] { "Signature Dish", "Trophy Catch" })
            rows.Single(row => row["C"] == name)["F"].Should().Be("Capstone", name);
        rows.Single(row => row["C"] == "Seasoned Palate I")["F"].Should().Be("Trait", "a Toggle row would need a combat animation plan entry");
        rows.Single(row => row["C"] == "Reduce I")["K"].Should().Be("12");
        rows.Single(row => row["C"] == "Reduce II")["K"].Should().Be("9");
    }

    [Test]
    public void TheAgricultureTabTotalsTheTwoLinesAtFiftySp_BesideTheFarmingRowsNotStarted()
    {
        var rows = ReadAgricultureRows().ToList();
        var perkRows = rows.Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();

        foreach (var line in Lines)
        {
            var lineRows = perkRows.Where(row => row["A"] == line).ToList();
            lineRows.Should().HaveCount(11, "ten purchases and the second style");
            var alternativeStyle = lineRows.Where(row => row["F"] == "Style").Skip(1).Single();
            (lineRows.Sum(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)) - decimal.Parse(alternativeStyle["B"], CultureInfo.InvariantCulture))
                .Should().Be(25m, line);
        }

        var totals = rows.Where(row => row.GetValueOrDefault("A") == "Total").ToList();
        totals.Select(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)).Should().Equal(new[] { 9m, 25m, 25m }, "Farming, Chef and Angler");
        var header = rows.Single(row => row.GetValueOrDefault("C") == "Total SP");
        decimal.Parse(header["D"], CultureInfo.InvariantCulture).Should().Be(59m, "Farming (not started) and the two lines");
    }

    private static void Compare(List<string> failures, string name, string column, string actual, string expected)
    {
        if (actual != expected)
            failures.Add($"{name}: Bible {column} is '{actual}' but the code has '{expected}'.");
    }

    private static IReadOnlyList<Dictionary<string, string>> ReadAgricultureRows()
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

        var sheet = ReadXml("xl/workbook.xml").Descendants(ns + "sheet").Single(element => (string)element.Attribute("name") == "Agriculture");
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
