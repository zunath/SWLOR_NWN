using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Armor;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class ArmorProvokeTests
{
    [Test]
    public void GeneralCategory_UsesGeneralSectionLabel()
    {
        var attribute = typeof(PerkCategoryType)
            .GetField(nameof(PerkCategoryType.General))!
            .GetCustomAttributes(typeof(PerkCategoryAttribute), false)
            .Cast<PerkCategoryAttribute>()
            .Single();

        attribute.Name.Should().Be("General");
        attribute.IsActive.Should().BeTrue();
    }
    [Test]
    public void ProvokeAbilities_MatchCombatBible()
    {
        var abilities = new ProvokeAbilityDefinition().BuildAbilities();

        var provoke1 = abilities[FeatType.Provoke1];
        provoke1.Name.Should().Be("Provoke I");
        provoke1.AbilityLevel.Should().Be(1);
        provoke1.RecastGroup.Should().Be(RecastGroup.Provoke);
        provoke1.RecastDelay(0).Should().Be(6f);
        provoke1.ActivationDelay(0, 0, 1).Should().Be(1f);
        provoke1.MaxRange.Should().Be(15f);
        provoke1.IsSingleTargetAbility.Should().BeTrue();
        provoke1.IsAreaAbility.Should().BeFalse();
        AssertHostileTargeting(provoke1);
        provoke1.RequiresTarget.Should().BeTrue();

        var provoke2 = abilities[FeatType.Provoke2];
        provoke2.Name.Should().Be("Provoke II");
        provoke2.AbilityLevel.Should().Be(2);
        provoke2.RecastGroup.Should().Be(RecastGroup.Provoke2);
        provoke2.RecastDelay(0).Should().Be(12f);
        provoke2.ActivationDelay(0, 0, 2).Should().Be(1f);
        provoke2.MaxRange.Should().Be(15f);
        provoke2.IsAreaAbility.Should().BeTrue();
        provoke2.IsSingleTargetAbility.Should().BeFalse();
        AssertHostileTargeting(provoke2);
        provoke2.RequiresTarget.Should().BeFalse();
        provoke2.RequiresLocationTarget.Should().BeTrue();
        provoke2.Targeting!.Flags.Should().NotHaveFlag(AbilityTargetingFlags.OriginOnSelf);
    }

    [Test]
    public void ProvokeFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var provoke1FeatIcon = featRows[1813]["ICON"];
        var provoke2FeatIcon = featRows[1814]["ICON"];
        var provoke1SpellIcon = spellRows[971]["IconResRef"];
        var provoke2SpellIcon = spellRows[972]["IconResRef"];

        provoke1FeatIcon.Should().Be("ife_provoke");
        provoke1SpellIcon.Should().Be(provoke1FeatIcon);
        provoke2FeatIcon.Should().Be("ife_provoke2");
        provoke2SpellIcon.Should().Be(provoke2FeatIcon);
        provoke2FeatIcon.Should().NotBe(provoke1FeatIcon);

        File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{provoke1FeatIcon}.tga").FullName).Should().BeTrue();
        File.Exists((root / "SWLOR_Haks" / "sw_ability" / $"{provoke2FeatIcon}.tga").FullName).Should().BeTrue();
    }

    [Test]
    public void Provoke2FeatAndSpellTargeting_IsSelectableArea()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        var provoke2Feat = featRows[1814];
        var provoke2Spell = spellRows[972];

        provoke2Feat["TARGETSELF"].Should().Be("****");
        provoke2Spell["Range"].Should().Be("S");
        provoke2Spell["TargetType"].Should().Be("0x06");
        provoke2Spell["HostileSetting"].Should().Be("0");
        provoke2Spell["FeatID"].Should().Be("****");
        provoke2Spell["TargetShape"].Should().Be("sphere");
        provoke2Spell["TargetSizeX"].Should().Be("8.0");
        provoke2Spell["TargetFlags"].Should().Be("3");
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

    private static void AssertHostileTargeting(AbilityDetail ability)
    {
        ability.IsHostileAbility.Should().BeTrue();
    }

    private static PerkDetail BuildProvokePerkWithout2daLookup()
    {
        var definition = new ArmorPerkDefinition();
        typeof(ArmorPerkDefinition)
            .GetMethod("Provoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = typeof(ArmorPerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[PerkType.Provoke];
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
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
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
