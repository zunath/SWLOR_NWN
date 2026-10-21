using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The appearance editor hides an armor part whose model is missing for the wearer's body,
/// so a part authored for only some bodies silently disappears for other species. Every
/// catalog part must have a model, and a tint row for palette and exact RGB colors, on every
/// playable body of both genders; a species' own skin on the bodies of the species that own it.
/// </summary>
public class ArmorPartCoverageTests
{
    private static readonly char[] Bodies = { 'h', 'e', 'o', 'd' };

    private static readonly (AppearanceArmor Part, string Model)[] Slots =
    {
        (AppearanceArmor.Neck, "neck"), (AppearanceArmor.Torso, "chest"), (AppearanceArmor.Belt, "belt"),
        (AppearanceArmor.Pelvis, "pelvis"), (AppearanceArmor.LeftShoulder, "shol"),
        (AppearanceArmor.RightShoulder, "shor"), (AppearanceArmor.LeftBicep, "bicepl"),
        (AppearanceArmor.RightBicep, "bicepr"), (AppearanceArmor.LeftForearm, "forel"),
        (AppearanceArmor.RightForearm, "forer"), (AppearanceArmor.LeftHand, "handl"),
        (AppearanceArmor.RightHand, "handr"), (AppearanceArmor.LeftThigh, "legl"),
        (AppearanceArmor.RightThigh, "legr"), (AppearanceArmor.LeftShin, "shinl"),
        (AppearanceArmor.RightShin, "shinr"), (AppearanceArmor.LeftFoot, "footl"),
        (AppearanceArmor.RightFoot, "footr"), (AppearanceArmor.Robe, "robe"),
    };

    private static IEnumerable<(string Model, string Body)> RequiredParts()
    {
        var speciesParts = SpeciesBodyParts();
        var models = Slots.ToDictionary(slot => slot.Part, slot => slot.Model);
        // A species' own skin is required only on the bodies of the species that own it.
        foreach (var ((part, style), races) in speciesParts)
        foreach (var race in races)
        foreach (var gender in "mf")
            yield return ($"{models[part]}{style:D3}", $"p{gender}{race}0");
        // Droids wear their own catalog on the droid body; every other species shares the general one.
        foreach (var (catalog, races) in new[]
                 {
                     (ArmorAppearanceCatalog.Definitions[AppearanceType.Human], Bodies),
                     (ArmorAppearanceCatalog.Definitions[AppearanceType.Droid], new[] { 'z' }),
                 })
        foreach (var (part, model) in Slots)
        foreach (var style in ArmorAppearanceCatalog.GetStyles(catalog, part).Where(style => style > 0).Distinct())
        {
            if (races == Bodies && speciesParts.ContainsKey((part, style)))
                continue;
            foreach (var race in races)
            foreach (var gender in "mf")
                yield return ($"{model}{style:D3}", $"p{gender}{race}0");
        }
    }

    [Test]
    public void EveryCatalogArmorPartHasATintRowOnEveryPlayableBody()
    {
        var tinted = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "tintmap.2da"))
            .Select(line => Regex.Split(line.Trim(), @"\s+"))
            .Where(columns => columns.Length == 4)
            .Select(columns => columns[1].ToLowerInvariant())
            .ToHashSet();
        Assert.That(tinted, Does.Contain("pmh0_chest001"));
        var speciesParts = SpeciesBodyParts();
        Assert.That(speciesParts[(AppearanceArmor.Torso, 208)], Does.Contain('e'), "Wookiee torso 208 belongs to the elf body");
        Assert.That(speciesParts[(AppearanceArmor.LeftHand, 45)], Does.Contain('h'), "Kel Dor hand 45 belongs to the human body");

        var gaps = RequiredParts()
            .Where(required => !tinted.Contains($"{required.Body}_{required.Model}"))
            .GroupBy(required => required.Model, required => required.Body)
            .Select(group => $"{group.Key}: {string.Join(", ", group)}")
            .ToList();

        Assert.That(gaps, Is.Empty,
            "Refit missing bodies with SWLOR_Haks/tools/GenerateRacialPartVariants.py and bind their tint rows (ItemAppearanceRules.md).");
    }

    [Test]
    public void EveryCatalogArmorPartHasAModelOnEveryPlayableBody()
    {
        var (models, redirects) = HakModels();
        // Resolve exactly as the server does: a .lod redirect wins over an .mdl and must reach one.
        bool Exists(string model) => ModelResource.Exists(model,
            (name, type) => type == ResType.LOD
                ? redirects.ContainsKey(name.ToLowerInvariant())
                : models.Contains(name.ToLowerInvariant()),
            name => File.ReadAllText(redirects[name.ToLowerInvariant()]));
        Assert.That(Exists("pmh0_chest001"), Is.True);

        var gaps = RequiredParts()
            .Where(required => !Exists($"{required.Body}_{required.Model}"))
            .GroupBy(required => required.Model, required => required.Body)
            .Select(group => $"{group.Key}: {string.Join(", ", group)}")
            .ToList();

        Assert.That(gaps, Is.Empty,
            "Refit missing bodies with SWLOR_Haks/tools/GenerateRacialPartVariants.py (ItemAppearanceRules.md).");
    }

    private static Dictionary<(AppearanceArmor Part, int Style), HashSet<char>> SpeciesBodyParts()
    {
        // A species' body part is racial when it departs from the shared default lists (which
        // Bothans use unchanged): Cathar and Trandoshan 201, Mon Calamari 204, Wookiee and Ewok
        // 208-215 and torso 350, the human-model bicep 251, Rodian hand 44, Kel Dor hand 45.
        // Those exist only for the bodies of the species that own them.
        var shared = BodyParts(new BothanRacialAppearanceDefinition());
        var races = BodyRaces();
        var parts = new Dictionary<(AppearanceArmor, int), HashSet<char>>();
        foreach (var type in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            // Droids wear their own catalog; only bodies sharing the general catalog have exceptions.
            if (!Bodies.Contains(races[(int)type]))
                continue;
            RacialAppearanceRegistry.TryGet(type, out var appearance);
            foreach (var part in BodyParts(appearance).Where(part => !shared.Contains(part)))
            {
                if (!parts.TryGetValue(part, out var owners))
                    parts[part] = owners = new HashSet<char>();
                owners.Add(races[(int)type]);
            }
        }
        return parts;
    }

    private static Dictionary<int, char> BodyRaces()
    {
        // appearance.2da RACE names the body model letter (p<g><race>0) each appearance wears.
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "appearance.2da"));
        var race = Array.IndexOf(Regex.Split(lines[2].Trim(), @"\s+"), "RACE") + 1;
        return lines.Skip(3)
            .Select(line => Regex.Matches(line, @"""[^""]*""|\S+").Select(match => match.Value).ToArray())
            .Where(columns => columns.Length > race && int.TryParse(columns[0], out _) && columns[race].Length == 1)
            .ToDictionary(columns => int.Parse(columns[0]), columns => char.ToLowerInvariant(columns[race][0]));
    }

    private static HashSet<(AppearanceArmor Part, int Style)> BodyParts(IRacialAppearanceDefinition appearance) => new[]
        {
            (AppearanceArmor.Torso, appearance.Torsos), (AppearanceArmor.Pelvis, appearance.Pelvis),
            (AppearanceArmor.RightBicep, appearance.RightBicep), (AppearanceArmor.LeftBicep, appearance.LeftBicep),
            (AppearanceArmor.RightForearm, appearance.RightForearm), (AppearanceArmor.LeftForearm, appearance.LeftForearm),
            (AppearanceArmor.RightHand, appearance.RightHand), (AppearanceArmor.LeftHand, appearance.LeftHand),
            (AppearanceArmor.RightThigh, appearance.RightThigh), (AppearanceArmor.LeftThigh, appearance.LeftThigh),
            (AppearanceArmor.RightShin, appearance.RightShin), (AppearanceArmor.LeftShin, appearance.LeftShin),
            (AppearanceArmor.RightFoot, appearance.RightFoot), (AppearanceArmor.LeftFoot, appearance.LeftFoot),
        }.SelectMany(slot => slot.Item2.Select(style => (slot.Item1, style))).ToHashSet();

    private static (HashSet<string> Models, Dictionary<string, string> Redirects) HakModels()
    {
        var haks = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks");
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(haks, "hakbuilder.json")));
        var models = new HashSet<string>();
        var redirects = new Dictionary<string, string>();
        // HAK order is priority order; the first resource of a name wins.
        foreach (var hak in config.RootElement.GetProperty("HakList").EnumerateArray())
        {
            var directory = Path.Combine(haks, hak.GetProperty("Path").GetString()!.Trim('.', '/'));
            if (!Directory.Exists(directory))
                continue;
            foreach (var file in Directory.EnumerateFiles(directory, "*.mdl"))
                models.Add(Path.GetFileNameWithoutExtension(file).ToLowerInvariant());
            foreach (var file in Directory.EnumerateFiles(directory, "*.lod"))
                redirects.TryAdd(Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), file);
        }
        return (models, redirects);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
