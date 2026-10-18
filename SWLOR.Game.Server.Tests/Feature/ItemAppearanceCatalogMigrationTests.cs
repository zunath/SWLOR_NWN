using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class ItemAppearanceCatalogMigrationTests
{
    // AppearanceArmor order; GFF labels cap at 16 characters, so the "x" twins are clipped.
    private static readonly string[] ArmorFields =
    {
        "RFoot", "LFoot", "RShin", "LShin", "LThigh", "RThigh", "Pelvis", "Torso", "Belt", "Neck",
        "RFArm", "LFArm", "RBicep", "LBicep", "RShoul", "LShoul", "RHand", "LHand", "Robe",
    };

    [Test]
    public void EveryRevision_EndsOnItsCurrentBlueprintAppearance()
    {
        var failures = new List<string>();
        foreach (var (resref, revision) in ItemAppearanceCatalogMigration.Revisions)
        {
            var path = Path.Combine(FindRepositoryRoot().FullName, "Module", "uti", $"{resref}.uti.json");
            if (!File.Exists(path))
            {
                failures.Add($"{resref}: blueprint is missing");
                continue;
            }

            var blueprint = JObject.Parse(File.ReadAllText(path));
            var baseItem = (BaseItem)blueprint["BaseItem"]!["value"]!.Value<int>();
            if (baseItem != revision.BaseItem)
                failures.Add($"{resref}: blueprint base item is {baseItem}, revision expects {revision.BaseItem}");
            var fields = baseItem switch
            {
                BaseItem.Armor => ArmorFields.Select(field => "ArmorPart_" + field).ToArray(),
                BaseItem.Helmet or BaseItem.Shuriken => new[] { "ModelPart1" },
                _ => new[] { "ModelPart1", "ModelPart2", "ModelPart3" },
            };
            if (revision.Previous.Length != fields.Length || revision.Current.Length != fields.Length)
            {
                failures.Add($"{resref}: expected {fields.Length} parts");
                continue;
            }

            var current = fields.Select(field => ReadPart(blueprint, field)).ToArray();
            if (!current.SequenceEqual(revision.Current))
                failures.Add($"{resref}: blueprint parts are {string.Join(",", current)}, revision ends on {string.Join(",", revision.Current)}");
            if (revision.Previous.SequenceEqual(revision.Current))
                failures.Add($"{resref}: revision changes nothing");
            if (fields.Length == 3 && revision.Previous.Concat(revision.Current).Any(part => part % 10 is < 1 or > 9))
                failures.Add($"{resref}: weapon parts must carry a color from 1 to 9");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static int ReadPart(JObject blueprint, string field) =>
        blueprint["x" + (field.Length > 15 ? field[..15] : field)]?["value"]?.Value<int>() ??
        blueprint[field]?["value"]?.Value<int>() ?? 0;

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
