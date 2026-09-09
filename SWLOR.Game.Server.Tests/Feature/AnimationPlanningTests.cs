#nullable enable
using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AnimationPlanningTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static string Normalize(string value) => Regex.Replace(Regex.Replace(value, @"\s+(I|II|III|IV|V|VI)$", ""), "[^A-Za-z0-9]", "").ToLowerInvariant();
    private static string Category(PerkDetail perk) => typeof(PerkCategoryType).GetField(perk.Category.ToString())!
        .GetCustomAttribute<PerkCategoryAttribute>()!.Name.Split(" - ")[0];

    private static Dictionary<string, string>[] Csv(string path)
    {
        using var reader = new TextFieldParser(Path.Combine(Root, path));
        reader.SetDelimiters(",");
        var headers = reader.ReadFields()!;
        var rows = new List<Dictionary<string, string>>();
        while (!reader.EndOfData)
        {
            var fields = reader.ReadFields()!;
            fields.Should().HaveCount(headers.Length);
            rows.Add(headers.Select((header, index) => (header, fields[index])).ToDictionary(p => p.header, p => p.Item2));
        }
        return rows.ToArray();
    }

    private static PerkDetail[] AllPerks()
    {
        var perks = new List<PerkDetail>();
        // Match the existing Bible audit's offline construction path; icon lookup needs the NWN VM.
        foreach (var type in typeof(IPerkListDefinition).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPerkListDefinition).IsAssignableFrom(type)))
        {
            var definition = Activator.CreateInstance(type)!;
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 && !method.Name.Contains('<'))
                         .OrderBy(method => method.MetadataToken)) method.Invoke(definition, null);
            var builder = type.GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
            perks.AddRange(((Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!).Values);
        }
        return perks.ToArray();
    }

    internal static PerkDetail[] CurrentPerks() => AllPerks().Where(perk => perk.IsActive &&
        perk.GroupType == PerkGroupType.Player && typeof(PerkCategoryType).GetField(perk.Category.ToString())!
            .GetCustomAttribute<PerkCategoryAttribute>()!.IsActive).ToArray();

    [Test]
    public void BeastPerksAreExcludedWhilePlayerBeastMasteryRemains()
    {
        var beasts = AllPerks().Where(perk => perk.GroupType == PerkGroupType.Beast).Select(perk => perk.Type.ToString()).ToArray();
        beasts.Should().NotBeEmpty();
        var plan = Csv("design/animations/animation-plan.csv");
        plan.Select(row => row["PerkId"]).Should().NotIntersectWith(beasts);
        plan.Where(row => row["Category"] == "Beast Mastery").Select(row => row["PerkId"]).Should().BeEquivalentTo(
            "Tame", "ReviveBeast", "Reward", "SoothePet", "GuardingBond", "PredatoryBond");
    }

    [Test]
    public void PlanCoversCurrentActivePerksAndAccuratelyReportsInstalledClips()
    {
        var abilities = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities()).ToDictionary(p => p.Key, p => p.Value);
        AbilityDetail[] Active(PerkDetail perk) => perk.PerkLevels.Values.SelectMany(level => level.GrantedFeats).Distinct()
            .Where(feat => abilities.ContainsKey(feat) && !abilities[feat].IsMimicryTrait).Select(feat => abilities[feat]).ToArray();
        var bible = Csv("SWLOR.Game.Server/Readmes/CombatUpgradeBiblePerkManifest.csv");
        var requirements = CurrentPerks().Where(perk => Active(perk).Length > 0 || bible.Any(row =>
            Normalize(row["Tab"]) == Normalize(Category(perk)) && Normalize(row["PerkName"]) == Normalize(perk.Name) &&
            new[] { "Combat", "Stance", "Toggle", "Aura" }.Contains(row["Type"]))).ToArray();
        var plan = Csv("design/animations/animation-plan.csv");
        plan.Select(row => row["PerkId"]).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(requirements.Select(perk => perk.Type.ToString()));
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/registry.json")));
        var installedIds = registry.RootElement.EnumerateArray().Select(entry => entry.GetProperty("Name").GetString()).ToHashSet();
        foreach (var perk in requirements)
        {
            var row = plan.Single(row => row["PerkId"] == perk.Type.ToString());
            row["Name"].Should().Be(perk.Name);
            row["Category"].Should().Be(Category(perk));
            var active = Active(perk);
            var installed = installedIds.Contains(perk.Type.ToString());
            row["Status"].Should().Be(installed ? "Installed" : active.Length > 0 ? "Needed" : "Native");
            foreach (var file in row["DefinitionFiles"].Split("; ", StringSplitOptions.RemoveEmptyEntries))
                File.Exists(Path.Combine(Root, file)).Should().BeTrue($"{perk.Name} must point to its current ability source");
        }
    }

    private sealed record PlannedAnimation(string Id, string Name, string InternalName, string Category,
        string PerkId, string[] Feats, string[] DefinitionFiles, string Reference);

    private static PlannedAnimation[] ActivePlan() => JsonSerializer.Deserialize<PlannedAnimation[]>(
        File.ReadAllText(Path.Combine(Root, "design/animations/active-abilities.json")))!;

    [Test]
    public void ActiveManifestCoversPlayerAbilitiesIncludingLearnedTechniques()
    {
        var definitions = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var perks = CurrentPerks();
        var granted = perks.SelectMany(perk => perk.PerkLevels.Values.SelectMany(level => level.GrantedFeats))
            .Distinct().Where(feat => definitions.TryGetValue(feat, out var ability) && !ability.IsMimicryTrait);
        var techniques = definitions.Where(pair => pair.Value.IsMimicryTechnique && !pair.Value.IsMimicryTrait)
            .Select(pair => pair.Key);
        var expected = granted.Concat(techniques).Distinct().Select(feat => feat.ToString()).ToArray();
        var plan = ActivePlan();
        plan.Select(entry => entry.Id).Should().OnlyHaveUniqueItems();
        plan.SelectMany(entry => entry.Feats).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(expected);
        plan.Single(entry => entry.Id == "CallBeast").Feats.Should().Equal("CallBeast");
        plan.Single(entry => entry.Id == "Tame").Feats.Should().NotContain("CallBeast");
        plan.Should().NotContain(entry => entry.Id == "Stealth", "stealth uses the native action mode without a custom clip");
        plan.Should().OnlyContain(entry => entry.Feats.Length > 0);
        var perkIds = perks.Select(perk => perk.Type.ToString()).ToHashSet();
        foreach (var entry in plan)
        {
            perkIds.Should().Contain(entry.PerkId);
            foreach (var file in entry.DefinitionFiles)
                File.Exists(Path.Combine(Root, file)).Should().BeTrue($"{entry.Id} references a current definition");
            foreach (var feat in entry.Feats)
                definitions[Enum.Parse<FeatType>(feat)].IsMimicryTrait.Should().BeFalse();
        }
    }

    [Test]
    public void NativeStealthIsExcludedWhileAllActiveFirstAidAbilitiesRemainCovered()
    {
        var plan = ActivePlan();
        CurrentPerks().Should().Contain(perk => perk.Type == PerkType.Stealth);
        var stealth = Csv("design/animations/animation-plan.csv").Single(row => row["PerkId"] == "Stealth");
        stealth["Status"].Should().Be("Native");
        stealth["InternalName"].Should().BeEmpty();
        stealth["BibleAnimationRow"].Should().BeEmpty();
        plan.Should().NotContain(entry => entry.Id == "Stealth");
        File.Exists(Path.Combine(Root, "design/animations/espionage/Stealth.swlanim")).Should().BeFalse();
        var firstAid = CurrentPerks().Where(perk => Category(perk) == "First Aid")
            .SelectMany(perk => perk.PerkLevels.Values.SelectMany(level => level.GrantedFeats)).ToHashSet();
        var definitions = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .Where(pair => firstAid.Contains(pair.Key) && !pair.Value.IsMimicryTrait)
            .Select(pair => pair.Key.ToString()).Distinct().ToArray();
        plan.Where(entry => entry.Category == "First Aid").SelectMany(entry => entry.Feats)
            .Should().BeEquivalentTo(definitions);
    }

    [Test]
    public void AnimatorNamesAreStableUniqueAndLeaveRoomForPlaybackPhases()
    {
        var names = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(
            Path.Combine(Root, "design/animations/animation-names.json")))!;
        var plan = ActivePlan();
        names.Keys.Should().BeEquivalentTo(plan.Select(entry => entry.Id));
        names.Values.Should().OnlyHaveUniqueItems();
        foreach (var entry in plan)
        {
            entry.InternalName.Should().Be(names[entry.Id]);
            entry.InternalName.Should().MatchRegex(@"\Asw_[a-z0-9_]{1,9}\z");
            (entry.InternalName + "_out").Length.Should().BeLessThanOrEqualTo(16);
        }
        using var registry = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "design/animations/registry.json")));
        foreach (var entry in registry.RootElement.EnumerateArray())
            names[entry.GetProperty("Name").GetString()!].Should().Be(entry.GetProperty("AnimationName").GetString());
    }

    [Test]
    public void ProjectProvenanceHashesArePortableAcrossGitLineEndings()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root,
            "design/animations/active-manifest.json")));
        var rows = manifest.RootElement.GetProperty("Animations").EnumerateArray().ToArray();
        rows.Select(row => row.GetProperty("Id").GetString()).Should().BeEquivalentTo(ActivePlan().Select(entry => entry.Id));
        foreach (var row in rows)
        {
            var path = Path.Combine(Root, "design/animations", row.GetProperty("Project").GetString()!);
            var normalized = File.ReadAllText(path).Replace("\r\n", "\n");
            var expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
            row.GetProperty("ProjectSha256").GetString().Should().Be(expected,
                $"{row.GetProperty("Id").GetString()} provenance must survive Git LF/CRLF conversion");
        }
    }

    [Test]
    public void BibleRetainsOnlyUsedPlansAndEveryReferenceMatchesItsNewRow()
    {
        var plan = ActivePlan();
        using var zip = ZipFile.OpenRead(Path.Combine(Root, "design/bible/SWLOR Design Bible - Combat Upgrade.xlsx"));
        XDocument Read(string path) { using var stream = zip.GetEntry(path)!.Open(); return XDocument.Load(stream); }
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var sheet = Read("xl/workbook.xml").Descendants(ns + "sheet").Single(s => (string?)s.Attribute("name") == "Animations");
        var target = (string)Read("xl/_rels/workbook.xml.rels").Root!.Elements()
            .Single(r => (string?)r.Attribute("Id") == (string?)sheet.Attribute(rel + "id")).Attribute("Target")!;
        var path = target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
        var xml = Read(path);
        var strings = zip.GetEntry("xl/sharedStrings.xml") == null ? Array.Empty<string>() : Read("xl/sharedStrings.xml")
            .Descendants(ns + "si").Select(s => string.Concat(s.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        string Text(XElement? cell) => cell == null ? "" : (string?)cell.Attribute("t") switch
        {
            "s" => strings[int.Parse(cell.Element(ns + "v")!.Value)],
            "inlineStr" => string.Concat(cell.Descendants(ns + "t").Select(t => t.Value)),
            _ => cell.Element(ns + "v")?.Value ?? ""
        };
        var header = xml.Descendants(ns + "row").Single(row => (int)row.Attribute("r")! == 1);
        var internalNameColumn = Regex.Replace((string)header.Elements(ns + "c")
            .Single(cell => Normalize(Text(cell)) == "internalname").Attribute("r")!, "[0-9]", "");
        var rows = xml.Descendants(ns + "row").Select(row => new
        {
            Number = (int)row.Attribute("r")!,
            Cells = row.Elements(ns + "c").ToDictionary(c => Regex.Replace((string)c.Attribute("r")!, "[0-9]", ""), Text)
        }).Where(row => row.Number > 1 && row.Cells.ContainsKey("C")).ToArray();
        rows.Should().NotBeEmpty();
        rows.Select(row => row.Number).Should().Equal(Enumerable.Range(2, rows.Length));
        foreach (var row in rows)
        {
            var entry = plan.Single(p => p.Category == row.Cells["B"] && p.Name == row.Cells["C"]);
            entry.Reference.Should().Be(row.Cells.GetValueOrDefault("E", ""));
            row.Cells.GetValueOrDefault(internalNameColumn, "").Should().Be(entry.InternalName);
        }
        rows.Should().HaveCount(plan.Length);
        rows.Select(row => (row.Cells["B"], row.Cells["C"])).Should().OnlyHaveUniqueItems();
        var links = xml.Descendants(ns + "hyperlink").ToArray();
        links.Should().HaveCount(rows.Count(row => row.Cells.GetValueOrDefault("E", "") != ""));
        var relationships = Read("xl/worksheets/_rels/" + Path.GetFileName(path) + ".rels").Root!.Elements()
            .ToDictionary(r => (string)r.Attribute("Id")!, r => (string)r.Attribute("Target")!);
        foreach (var link in links)
        {
            var number = int.Parse(Regex.Replace((string)link.Attribute("ref")!, "[A-Z]", ""));
            relationships[(string)link.Attribute(rel + "id")!].Should().Be(rows.Single(row => row.Number == number).Cells["E"]);
        }
    }
}
