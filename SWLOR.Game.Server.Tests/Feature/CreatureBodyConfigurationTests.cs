using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class CreatureBodyConfigurationTests
{
    // Dynamic appearance rows select a body family, not a complete creature model.
    // Zero is valid for individual hidden parts, but omitting the body configuration
    // entirely leaves only equipment and attachments visible.
    private static readonly string[] BodyFields =
    {
        "Appearance_Head", "BodyPart_Belt", "BodyPart_LBicep", "BodyPart_LFArm",
        "BodyPart_LFoot", "BodyPart_LHand", "BodyPart_LShin", "BodyPart_LShoul",
        "BodyPart_LThigh", "BodyPart_Neck", "BodyPart_Pelvis", "BodyPart_RBicep",
        "BodyPart_RFArm", "BodyPart_RHand", "BodyPart_RShin", "BodyPart_RShoul",
        "BodyPart_RThigh", "BodyPart_Torso",
        "ArmorPart_RFoot", // Aurora stores the creature's right foot under this name.
    };

    [TestCase("utc")]
    [TestCase("git")]
    public void DynamicCreatures_HaveCompleteBodyConfigurations(string resourceType)
    {
        var root = FindRepositoryRoot();
        var configuredHaks = Environment.GetEnvironmentVariable("SWLOR_HAKS_CORPUS");
        var haks = string.IsNullOrWhiteSpace(configuredHaks)
            ? Path.Combine(root.FullName, "SWLOR_Haks")
            : Path.GetFullPath(configuredHaks);
        var appearances = TwoDAReader.Read(Path.Combine(haks, "sw_2da", "appearance.2da"));
        appearances.HasColumn("MODELTYPE").Should().BeTrue();
        var failures = new List<string>();
        var dynamicCount = 0;

        foreach (var path in Directory.EnumerateFiles(
                     Path.Combine(root.FullName, "Module", resourceType), $"*.{resourceType}.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var creatures = resourceType == "utc"
                ? new[] { document.RootElement }
                : document.RootElement.GetProperty("Creature List").GetProperty("value")
                    .EnumerateArray().ToArray();

            foreach (var creature in creatures)
            {
                var appearance = creature.GetProperty("Appearance_Type").GetProperty("value").GetInt32();
                if (!string.Equals(appearances.GetValue(appearance, "MODELTYPE"), "P",
                        StringComparison.OrdinalIgnoreCase))
                    continue;

                dynamicCount++;
                var identity = $"{Path.GetFileName(path)} ({creature.GetProperty("TemplateResRef").GetProperty("value").GetString()})";
                var missing = BodyFields.Where(field => !creature.TryGetProperty(field, out _)).ToArray();
                if (missing.Length > 0)
                    failures.Add($"{identity}: missing {string.Join(", ", missing)}.");

                if (!creature.TryGetProperty("Gender", out var gender) ||
                    gender.GetProperty("value").GetInt32() is not (0 or 1))
                    failures.Add($"{identity}: dynamic bodies require a male or female model prefix.");
            }
        }

        dynamicCount.Should().BeGreaterThan(0, "the corpus check must inspect dynamic creatures");
        failures.Should().BeEmpty("every dynamic creature must specify its head and body parts");
        TestContext.Out.WriteLine($"Validated {dynamicCount} dynamic creatures in {resourceType} resources.");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
