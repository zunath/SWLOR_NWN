using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// An identified item shows its DescIdentified text, and falls back to the generic "There are many unusual items..." line when
/// that field is empty. Every item the city revamp added is created identified, so each one must carry its real description
/// in DescIdentified, the same text it carries in Description.
/// </summary>
public class CityItemDescriptionTests
{
    /// <summary>Seeds, saplings, fertilizer, manure, young livestock and the crate, house drinks, the stall pie and the signature dishes and drinks.</summary>
    private static readonly Regex CityItemPrefixes = new("^(seed|sapling|fertilizer|manure|livestock|house|stall|sig)_", RegexOptions.Compiled);

    /// <summary>The city structures (5100-5105 and the planet identity and exclusive structures 5130-5199) and the player station structures (5018-5021).</summary>
    private static readonly Regex CityStructures = new("^structure_(50(18|19|20|21)|51\\d\\d)$", RegexOptions.Compiled);

    private static string UtiFolder
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return Path.Combine(directory!.FullName, "Module", "uti");
        }
    }

    private static string LocalText(JsonElement item, string field)
    {
        if (!item.TryGetProperty(field, out var property) ||
            !property.TryGetProperty("value", out var value) ||
            value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("0", out var text))
        {
            return string.Empty;
        }

        return text.GetString() ?? string.Empty;
    }

    private static List<string> CityItems() =>
        Directory.EnumerateFiles(UtiFolder, "*.uti.json")
            .Select(path => Path.GetFileName(path)[..^".uti.json".Length])
            .Where(resref => CityItemPrefixes.IsMatch(resref) || CityStructures.IsMatch(resref))
            .OrderBy(resref => resref, StringComparer.Ordinal)
            .ToList();

    [Test]
    public void TheCityItemsAreFound()
    {
        // 26 seeds, 5 saplings, 5 fertilizers, 5 manures, 8 livestock, 3 house drinks, the stall pie, 16 signature items,
        // 6 + 32 + 10 city structures and 4 station structures.
        CityItems().Count.Should().BeGreaterThanOrEqualTo(121);
    }

    [Test]
    public void EveryCityItemCarriesItsRealDescriptionWhenIdentified()
    {
        var empty = new List<string>();
        var different = new List<string>();

        foreach (var resref in CityItems())
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(UtiFolder, resref + ".uti.json")));
            var description = LocalText(document.RootElement, "Description");
            var identified = LocalText(document.RootElement, "DescIdentified");

            if (string.IsNullOrWhiteSpace(identified))
                empty.Add(resref);
            else if (identified != description)
                different.Add(resref);
        }

        empty.Should().BeEmpty("an identified copy of these items would show the generic unusual-items text: {0}", string.Join(", ", empty));
        different.Should().BeEmpty("DescIdentified repeats the item's Description: {0}", string.Join(", ", different));
    }
}
