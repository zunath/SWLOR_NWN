#nullable enable
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Droid parts and controllers saved before the Unarmed and Twin Pistol droid skills gain them beside their group,
/// at the group's lowest total, as new copies carry them.
/// </summary>
public class DroidWeaponSkillMigrationTests
{
    private const int Katar = (int)DroidStatSubType.Katar;
    private const int Staff = (int)DroidStatSubType.Staff;
    private const int Pistol = (int)DroidStatSubType.Pistol;
    private const int Rifle = (int)DroidStatSubType.Rifle;
    private const int Throwing = (int)DroidStatSubType.Throwing;
    private const int Unarmed = (int)DroidStatSubType.Unarmed;
    private const int TwinPistol = (int)DroidStatSubType.TwinPistol;
    private const int Might = (int)DroidStatSubType.MGT;

    [Test]
    public void LegacyCpu_GainsBothSkillsAtItsGroupValue()
    {
        Missing((Might, 10), (Katar, 25), (Staff, 25), (Pistol, 25), (Rifle, 25), (Throwing, 25))
            .Should().Equal((DroidStatSubType.Unarmed, 25), (DroidStatSubType.TwinPistol, 25));
    }

    [Test]
    public void BoostedSkill_IsNotCopied()
    {
        Missing((Katar, 30), (Staff, 25), (Pistol, 25), (Rifle, 32), (Throwing, 25))
            .Should().Equal((DroidStatSubType.Unarmed, 25), (DroidStatSubType.TwinPistol, 25));
    }

    [Test]
    public void SeveralPropertiesOfOneSkill_CountTheirTotal()
    {
        // A crafted part can carry its base skill and a boost to it as separate properties.
        Missing((Katar, 25), (Katar, 5), (Staff, 25), (Staff, 10)).Should().Equal((DroidStatSubType.Unarmed, 30));
    }

    [Test]
    public void ControllerZeroSkills_GainZeroSkills()
    {
        // Assembly saves every skill on the controller, zero when no part grants it.
        Missing((Katar, 0), (Staff, 0), (Pistol, 0), (Rifle, 0), (Throwing, 0))
            .Should().Equal((DroidStatSubType.Unarmed, 0), (DroidStatSubType.TwinPistol, 0));
    }

    [Test]
    public void IncompleteGroups_AreLeftAlone()
    {
        Missing((Katar, 10)).Should().BeEmpty("a single Katar boost is not the martial group");
        Missing((Staff, 10), (Pistol, 10), (Rifle, 10)).Should().BeEmpty("neither group is complete");
        Missing((Might, 10)).Should().BeEmpty();
        Missing().Should().BeEmpty();
    }

    [Test]
    public void ItemsWithTheSkill_AreLeftAlone()
    {
        Missing((Katar, 25), (Staff, 25), (Unarmed, 3), (Pistol, 25), (Rifle, 25), (Throwing, 25), (TwinPistol, 0))
            .Should().BeEmpty("an existing skill keeps its value, boosted or not");
        Missing((Katar, 25), (Staff, 25), (Unarmed, 25), (Pistol, 20), (Rifle, 20), (Throwing, 20))
            .Should().Equal((DroidStatSubType.TwinPistol, 20));
    }

    [Test]
    public void Rule_IsIdempotent()
    {
        var stats = new List<(int Subtype, int Value)> { (Katar, 30), (Staff, 25), (Pistol, 0), (Rifle, 0), (Throwing, 0) };
        var added = DroidWeaponSkillMigration.GetMissingSkills(stats).ToList();
        added.Should().NotBeEmpty();
        stats.AddRange(added.Select(skill => ((int)skill.Skill, skill.Value)));
        DroidWeaponSkillMigration.GetMissingSkills(stats).Should().BeEmpty("a migrated item is already current");
    }

    [Test]
    public void CurrentBlueprints_AlreadyCarryWhatTheRuleAdds()
    {
        // New parts are made from these blueprints, so a migrated copy matches a new one only if they are fixed points.
        var checkedBlueprints = 0;
        foreach (var path in Directory.GetFiles(Path.Combine(FindRepositoryRoot(), "Module", "uti"), "d_*.uti.json"))
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
            if (!blueprint.RootElement.TryGetProperty("PropertiesList", out var list))
                continue;
            var stats = list.GetProperty("value").EnumerateArray()
                .Where(property => Value(property, "PropertyName") == (int)ItemPropertyType.DroidStat)
                .Select(property => (Value(property, "Subtype"), Value(property, "CostValue")))
                .ToList();
            if (stats.Count == 0)
                continue;
            checkedBlueprints++;
            DroidWeaponSkillMigration.GetMissingSkills(stats).Should().BeEmpty($"{Path.GetFileName(path)} already carries its new skills");
            foreach (var (skill, group) in DroidWeaponSkillMigration.Groups)
            {
                var total = stats.Where(stat => stat.Item1 == (int)skill).Sum(stat => stat.Item2);
                if (stats.Any(stat => stat.Item1 == (int)skill) && group.All(member => stats.Any(stat => stat.Item1 == (int)member)))
                    group.Min(member => stats.Where(stat => stat.Item1 == (int)member).Sum(stat => stat.Item2))
                        .Should().Be(total, $"{Path.GetFileName(path)} carries {skill} at its group's value, as a migrated copy does");
            }
        }
        checkedBlueprints.Should().BeGreaterThan(0);
    }

    private static List<(DroidStatSubType Skill, int Value)> Missing(params (int Subtype, int Value)[] stats) =>
        DroidWeaponSkillMigration.GetMissingSkills(stats).ToList();

    private static int Value(JsonElement property, string name) => property.GetProperty(name).GetProperty("value").GetInt32();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "Module", "uti")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate Module/uti.");
    }
}
