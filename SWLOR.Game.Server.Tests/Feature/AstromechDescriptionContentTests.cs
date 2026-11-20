using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AstromechService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The descriptions of astromech gear. An astromech form is a stat copy of a humanoid piece, but its description is
/// its own (tools/AstromechDescriptions.json): Chassis Plating must not read as a tunic. These tests fail when a
/// humanoid piece gains a description, or a form is added, until its description is written and the generators rerun.
/// </summary>
public class AstromechDescriptionContentTests
{
    // What a humanoid wears or carries in a slot an astromech fills with a part, and who wears it.
    private static readonly Regex HumanoidWords = new(
        @"\b(tunics?|robes?|coats?|vests?|cuirass|vestments?|garments?|boots?|shoes?|leggings?|gloves?|gauntlets?|" +
        @"bracers?|vambraces?|cestus|sash|sashes|belts?|necklaces?|pendants?|amulets?|talismans?|rings?|signets?|" +
        @"cloaks?|capes?|mantles?|shawls?|helmets?|helms?|hoods?|cowls?|masks?|headdress|staff|staffs|staves|rifles?|" +
        @"carbines?|wearers?|wielders?|Armor training|Staff training|Rifle training)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Garments, jewelry and body parts an item name must not carry. A name is checked more narrowly than a
    // description: "Ringside", "Dead Hand" (a creature's name) and "Boot Hill" are names of other things.
    private static readonly Regex HumanoidNameWords = new(
        @"\b(tunics?|robes?|coats?|vests?|cuirass|vestments?|boots|shoes|leggings|gloves|gauntlets?|bracers?|" +
        @"vambraces?|cestus|sash|belts?|necklaces?|pendants?|amulets?|talismans?|signets?|band|\w*cloaks?|capes?|" +
        @"mantles?|shawls?|helmets?|helm|hood|hooded|cowls?|masks?|headdress|rebreathers?|dusters?|wraps?|\w+wrap|" +
        @"fetish|staff|staffs|rifles?|carbines?|fingers?|shoulders?|wrists?|necks?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string Root = FindRepositoryRoot();

    [Test]
    public void NoAstromechGearIsNamedAsAHumanoidPiece()
    {
        var problems = new List<string>();
        var files = Directory.GetFiles(Path.Combine(Root, "Module", "uti"), "am_*.uti.json");
        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file, Encoding.Latin1));
            var name = Description(document.RootElement, "LocalizedName");
            var words = HumanoidNameWords.Matches(name).Select(match => match.Value.ToLowerInvariant()).Distinct().ToList();
            if (words.Count > 0)
                problems.Add($"{Path.GetFileName(file).Split('.')[0]} \"{name}\" says {string.Join(", ", words)}");
        }

        files.Length.Should().BeGreaterThan(1200);
        problems.Should().BeEmpty("name the form in tools/AstromechLootNames.json and rerun the generators" +
                                  Environment.NewLine + string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void EveryFormOfADescribedPiece_HasADescriptionOfItsOwn()
    {
        var problems = new List<string>();
        var described = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "uti"), "*.uti.json"))
        {
            var source = Path.GetFileName(file).Split('.')[0];
            if (!AstromechItemTwins.HasTwin(source))
                continue;

            var form = AstromechItemTwins.Resolve(source, BodyFrame.Astromech);
            var (sourceIdentified, sourceUnidentified) = Descriptions(file);
            var (identified, unidentified) = Descriptions(Path.Combine(Root, "Module", "uti", form + ".uti.json"));
            var hasOne = sourceIdentified.Length > 0 || sourceUnidentified.Length > 0;
            if (hasOne)
                described++;

            if (hasOne != identified.Length > 0)
                problems.Add($"{form}: {(hasOne ? "has no description" : "is described, but " + source + " is not")}");
            if (identified != unidentified)
                problems.Add($"{form}: reads differently when it is not identified");
        }

        described.Should().BeGreaterThan(400, "most looted, store and reward gear is described");
        problems.Should().BeEmpty("write the description in tools/AstromechDescriptions.json and rerun the generators" +
                                  Environment.NewLine + string.Join(Environment.NewLine, problems.Take(40)));
    }

    [Test]
    public void NoAstromechGearReadsAsAHumanoidPiece()
    {
        var problems = new List<string>();
        var checkedForms = 0;
        foreach (var file in Directory.GetFiles(Path.Combine(Root, "Module", "uti"), "am_*.uti.json"))
        {
            var (identified, _) = Descriptions(file);
            if (identified.Length == 0)
                continue;

            checkedForms++;
            var name = Path.GetFileName(file).Split('.')[0];
            var words = HumanoidWords.Matches(identified).Select(match => match.Value.ToLowerInvariant()).Distinct().ToList();
            if (words.Count > 0)
                problems.Add($"{name} says {string.Join(", ", words)}: {identified}");
            if (Encoding.UTF8.GetByteCount(identified) != identified.Length || identified.Contains('\n'))
                problems.Add($"{name}: its description is not plain ASCII on one line");
        }

        checkedForms.Should().BeGreaterThan(400);
        problems.Should().BeEmpty(string.Join(Environment.NewLine, problems.Take(40)));
    }

    private static (string Identified, string Unidentified) Descriptions(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.Latin1));
        return (Description(document.RootElement, "DescIdentified"), Description(document.RootElement, "Description"));
    }

    private static string Description(JsonElement item, string field)
    {
        return item.TryGetProperty(field, out var text) && text.GetProperty("value").TryGetProperty("0", out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
