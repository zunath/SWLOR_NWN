using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class SpeciesCameraHeightTests
{
    // Stock dynamic rows whose HEIGHT matches each body model prefix's skeleton.
    private static readonly Dictionary<string, int> SkeletonBaseRows = new(StringComparer.OrdinalIgnoreCase)
    {
        ["D"] = 0, // Dwarf
        ["E"] = 1, // Elf
        ["G"] = 2, // Gnome
        ["A"] = 3, // Halfling
        ["O"] = 5, // Half-Orc
        ["H"] = 6, // Human
    };

    // The camera ignores visual scale, so Wookiee keeps a taller camera for the heights players usually choose.
    private static readonly Dictionary<AppearanceType, string> IntentionalHeights = new()
    {
        [AppearanceType.Wookiee] = "2.25",
    };

    // Species on a custom skeleton with no stock row to compare against.
    private static readonly AppearanceType[] CustomSkeletonSpecies = { AppearanceType.Droid };

    [Test]
    public void PlayableSpeciesUseTheCameraHeightOfTheSkeletonTheyRenderWith()
    {
        var appearances = TwoDAReader.Read(Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da", "appearance.2da"));
        var failures = new List<string>();
        var unmatched = new List<AppearanceType>();

        foreach (var species in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            var race = appearances.GetValue((int)species, "RACE");
            if (race == null || !SkeletonBaseRows.TryGetValue(race, out var baseRow))
            {
                unmatched.Add(species);
                continue;
            }

            var height = appearances.GetValue((int)species, "HEIGHT");
            var expected = IntentionalHeights.TryGetValue(species, out var intentional)
                ? intentional
                : appearances.GetValue(baseRow, "HEIGHT");
            if (height != expected)
                failures.Add($"{species} ({(int)species}) renders on skeleton '{race}' but has HEIGHT {height}; expected {expected}.");
        }

        failures.Should().BeEmpty("appearance.2da HEIGHT places the camera relative to the skeleton");
        unmatched.Should().BeEquivalentTo(CustomSkeletonSpecies);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
