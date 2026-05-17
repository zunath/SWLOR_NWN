using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class ArmorDualWieldTests
{
    [Test]
    public void DualWieldPerkLevels_MatchDesign()
    {
        var perk = BuildDualWieldPerkWithout2daLookup();

        perk.Name.Should().Be("Dual Wield");
        perk.Category.Should().Be(PerkCategoryType.General);

        AssertDualWieldLevel(
            perk.PerkLevels[1],
            2,
            5,
            "While dual wielding, reduces off-hand attack delay by 10%.");

        AssertDualWieldLevel(
            perk.PerkLevels[2],
            3,
            25,
            "While dual wielding, reduces off-hand attack delay by 20% total.");

        AssertDualWieldLevel(
            perk.PerkLevels[3],
            4,
            40,
            "While dual wielding, reduces off-hand attack delay by 30% total.");
    }

    [Test]
    public void DualWieldResPerkRow_IsPresent()
    {
        var root = FindRepositoryRoot();
        var perkRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "iprp_resperk.2da");

        perkRows[173]["Name"].Should().Be("16872220");
        perkRows[173]["Label"].Should().Be("DualWield");
        perkRows[173]["Cost"].Should().Be("0");
    }

    [Test]
    public void DualWieldImplementationDetails_MatchDesign()
    {
        var root = FindRepositoryRoot();

        var armorPerks = File.ReadAllText((root / "SWLOR.Game.Server" / "Feature" / "PerkDefinition" / "ArmorPerkDefinition.cs").FullName);
        armorPerks.Should().Contain("StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 10 : 0");
        armorPerks.Should().Contain("StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 20 : 0");
        armorPerks.Should().Contain("StatType.OffhandAttackDelayReductionPercent, creature => EquipmentPredicates.HasDualWield(creature) ? 30 : 0");

        var equipmentPredicates = File.ReadAllText((root / "SWLOR.Game.Server" / "Service" / "PerkService" / "EquipmentPredicates.cs").FullName);
        equipmentPredicates.Should().Contain("public static bool HasDualWield(uint creature)");
        equipmentPredicates.Should().Contain("InventorySlot.RightHand, Item.WeaponBaseItemTypes");
        equipmentPredicates.Should().Contain("InventorySlot.LeftHand, Item.WeaponBaseItemTypes");
    }

    private static void AssertDualWieldLevel(PerkLevel level, int price, int armorRank, string description)
    {
        level.Price.Should().Be(price);
        level.Description.Should().Be(description);
        level.GrantedFeats.Should().BeEmpty();
        level.StatBonuses.Should().ContainSingle(x => x.Stat == StatType.OffhandAttackDelayReductionPercent);
        AssertSkillRequirement(level, SkillType.Armor, armorRank);
    }

    private static void AssertSkillRequirement(PerkLevel level, SkillType skill, int rank)
    {
        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;

        requirement.Type.Should().Be(skill);
        requirement.RequiredRank.Should().Be(rank);
    }

    private static PerkDetail BuildDualWieldPerkWithout2daLookup()
    {
        var definition = new ArmorPerkDefinition();
        typeof(ArmorPerkDefinition)
            .GetMethod("DualWield", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = typeof(ArmorPerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[PerkType.DualWield];
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var i = 0; i < header.Length && i + 1 < cells.Length; i++)
            {
                values[header[i]] = cells[i + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "swlor2_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
