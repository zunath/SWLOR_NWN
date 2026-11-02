using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>Keeps the Smithery tab of the Design Bible in step with the Forgemaster and Artisan perks the game registers.</summary>
public class SmitheryPerkBibleTests
{
    private static readonly string[] Lines = { "Forgemaster", "Artisan" };

    private static readonly string[] Roman = { "I", "II", "III" };

    [Test]
    public void EveryPerkLevelHasItsBibleRowWithTheCodesPriceRankAndDescription()
    {
        var rows = ReadSmitheryRows().Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();
        var perks = new SmitheryPerkDefinition().BuildPerks();
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
                Compare(failures, name, "SP Price", decimal.Parse(row["B"], CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture), perkLevel.Price.ToString(CultureInfo.InvariantCulture));
                Compare(failures, name, "Skill Reqs.", row["D"], $"Smithery {rank}");
                Compare(failures, name, "Description", row["G"], perkLevel.Description);
                Compare(failures, name, "Dev Status", row["M"], "Implemented");
                Compare(failures, name, "Line", row["A"], ForgePerkLines.Forgemaster.Contains(type) ? "Forgemaster" : "Artisan");
            }
        }

        rows.Should().HaveCount(expectedRows, "the Bible holds one row per purchasable perk level and no other Forgemaster or Artisan rows");
        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void TheSmitheryTabTotalsTheTwoLinesAtFiftySp()
    {
        var rows = ReadSmitheryRows().ToList();
        var perkRows = rows.Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();

        perkRows.Sum(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)).Should().Be(50m);
        foreach (var line in Lines)
            perkRows.Where(row => row["A"] == line).Sum(row => decimal.Parse(row["B"], CultureInfo.InvariantCulture)).Should().Be(25m, line);

        var total = rows.Single(row => row.GetValueOrDefault("A") == "Total");
        decimal.Parse(total["B"], CultureInfo.InvariantCulture).Should().Be(50m, "the Total row's cached sum counts the purchasable rows");
        var header = rows.Single(row => row.GetValueOrDefault("C") == "Total SP");
        decimal.Parse(header["D"], CultureInfo.InvariantCulture).Should().Be(50m);
    }

    [Test]
    public void ThePerkManifestHoldsTheSameRowsAsTheBible()
    {
        var bible = ReadSmitheryRows().Where(row => Lines.Contains(row.GetValueOrDefault("A"))).ToList();
        var manifest = ReadManifestRows("Smithery");

        manifest.Should().HaveCount(bible.Count);
        for (var index = 0; index < bible.Count; index++)
        {
            manifest[index]["PerkName"].Should().Be(bible[index]["C"]);
            manifest[index]["Style"].Should().Be(bible[index]["A"]);
            manifest[index]["Description"].Should().Be(bible[index]["G"]);
            manifest[index]["SkillRequirements"].Should().Be(bible[index]["D"]);
            manifest[index]["Type"].Should().Be(bible[index]["F"]);
            decimal.Parse(manifest[index]["Price"], CultureInfo.InvariantCulture).Should().Be(decimal.Parse(bible[index]["B"], CultureInfo.InvariantCulture));
            manifest[index]["DevStatus"].Should().Be("Implemented");
        }
    }

    private static void Compare(List<string> failures, string name, string column, string actual, string expected)
    {
        if (actual != expected)
            failures.Add($"{name}: Bible {column} is '{actual}' but the code has '{expected}'.");
    }

    private static DirectoryInfo FindRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }

    private static IReadOnlyList<Dictionary<string, string>> ReadManifestRows(string tab)
    {
        var path = Path.Combine(FindRoot().FullName, "SWLOR.Game.Server", "Readmes", "CombatUpgradeBiblePerkManifest.csv");
        using var parser = new Microsoft.VisualBasic.FileIO.TextFieldParser(path);
        parser.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        var header = parser.ReadFields()!;
        var rows = new List<Dictionary<string, string>>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields()!;
            if (fields[0] == tab)
                rows.Add(header.Zip(fields, (name, value) => (name, value)).ToDictionary(pair => pair.name, pair => pair.value));
        }

        return rows;
    }

    private static IReadOnlyList<Dictionary<string, string>> ReadSmitheryRows()
    {
        using var archive = ZipFile.OpenRead(Path.Combine(FindRoot().FullName, "design", "bible", "SWLOR Design Bible.xlsx"));
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument ReadXml(string path)
        {
            using var stream = archive.GetEntry(path)!.Open();
            return XDocument.Load(stream);
        }

        var sheet = ReadXml("xl/workbook.xml").Descendants(ns + "sheet").Single(element => (string)element.Attribute("name") == "Smithery");
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
