using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerCameraHeightTests
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

    // Species on a custom skeleton with no stock row to compare against.
    private static readonly AppearanceType[] CustomSkeletonSpecies = { AppearanceType.Droid };

    [TestCase(2.0f, 1.0f, 0f)]
    [TestCase(2.0f, 1.0005f, 0f)]
    [TestCase(2.0f, 1.15f, 1.725f)]
    [TestCase(1.75f, 1.5f, 1.875f)]
    [TestCase(1.5f, 0.86f, 0.86f)]
    [TestCase(2.0f, 0.85f, 1.275f)]
    [TestCase(0.3f, 1.2f, 0f)]
    [TestCase(0f, 1.2f, 0f)]
    [TestCase(2.0f, 0f, 0f)]
    public void CameraHeightScalesTheRacialPivotWithTheBody(float racialHeight, float scale, float expected)
    {
        PlayerCameraHeight.CalculateCameraHeight(racialHeight, scale).Should().BeApproximately(expected, 0.0001f);
    }

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
            var expected = appearances.GetValue(baseRow, "HEIGHT");
            if (height != expected)
                failures.Add($"{species} ({(int)species}) renders on skeleton '{race}' but has HEIGHT {height}; " +
                             $"row {baseRow} uses {expected}.");
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
