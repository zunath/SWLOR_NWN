using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The appearance editor hides an armor part whose model is missing for the wearer's body,
/// so a part authored for only some bodies silently disappears for other species. Every
/// catalog part must render, with tint rows for palette and exact RGB colors, on every
/// playable body of both genders.
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

    // Armor slots that can show a species' own body part; necks, belts, shoulders and robes cannot.
    private static readonly HashSet<AppearanceArmor> BodySlots = new()
    {
        AppearanceArmor.Torso, AppearanceArmor.Pelvis, AppearanceArmor.LeftBicep, AppearanceArmor.RightBicep,
        AppearanceArmor.LeftForearm, AppearanceArmor.RightForearm, AppearanceArmor.LeftHand, AppearanceArmor.RightHand,
        AppearanceArmor.LeftThigh, AppearanceArmor.RightThigh, AppearanceArmor.LeftShin, AppearanceArmor.RightShin,
        AppearanceArmor.LeftFoot, AppearanceArmor.RightFoot,
    };

    [Test]
    public void EveryCatalogArmorPartRendersOnEveryPlayableBodyOrNone()
    {
        var tinted = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "tintmap.2da"))
            .Select(line => Regex.Split(line.Trim(), @"\s+"))
            .Where(columns => columns.Length == 4)
            .Select(columns => columns[1].ToLowerInvariant())
            .ToHashSet();
        Assert.That(tinted, Does.Contain("pmh0_chest001"));

        var speciesParts = SpeciesBodyParts();
        var gaps = new List<string>();
        // Droids wear their own catalog on the droid body; every other species shares the general one.
        foreach (var (catalog, races) in new[]
                 {
                     (ArmorAppearanceCatalog.Definitions[AppearanceType.Human], Bodies),
                     (ArmorAppearanceCatalog.Definitions[AppearanceType.Droid], new[] { 'z' }),
                 })
        foreach (var (part, model) in Slots)
        foreach (var style in ArmorAppearanceCatalog.GetStyles(catalog, part).Where(style => style > 0).Distinct())
        {
            if (races == Bodies && BodySlots.Contains(part) && speciesParts.Contains(style))
                continue;
            var bodies = races.SelectMany(race => new[] { $"pm{race}0", $"pf{race}0" }).ToList();
            var missing = bodies.Where(body => !tinted.Contains($"{body}_{model}{style:D3}")).ToList();
            // A style no body renders is hidden everywhere; partial coverage is the defect.
            if (missing.Count > 0 && missing.Count < bodies.Count)
                gaps.Add($"{model}{style:D3}: {string.Join(", ", missing)}");
        }

        Assert.That(gaps, Is.Empty,
            "Refit the missing bodies with SWLOR_Haks/tools/GenerateRacialPartVariants.py (ItemAppearanceRules.md).");
    }

    private static HashSet<int> SpeciesBodyParts()
    {
        // Racial skins (Cathar and Trandoshan 201, Mon Calamari 204, Wookiee and Ewok 208-214,
        // the human-model bicep 251) exist only for the bodies that own them. Styles every
        // species' default lists offer (hands 258-263) are not racial.
        var shared = BodyParts(new BothanRacialAppearanceDefinition());
        var parts = new HashSet<int>();
        foreach (var type in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            RacialAppearanceRegistry.TryGet(type, out var appearance);
            parts.UnionWith(BodyParts(appearance).Where(part => part > 200 && !shared.Contains(part)));
        }
        return parts;
    }

    private static HashSet<int> BodyParts(IRacialAppearanceDefinition appearance) => new[]
        {
            appearance.Torsos, appearance.Pelvis, appearance.RightBicep, appearance.LeftBicep,
            appearance.RightForearm, appearance.LeftForearm, appearance.RightHand, appearance.LeftHand,
            appearance.RightThigh, appearance.LeftThigh, appearance.RightShin, appearance.LeftShin,
            appearance.RightFoot, appearance.LeftFoot,
        }.SelectMany(list => list).ToHashSet();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
