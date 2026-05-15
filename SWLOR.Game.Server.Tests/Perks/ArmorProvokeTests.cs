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
    public void ProvokePerkLevels_MatchCombatBible()
    {
        var perk = BuildProvokePerkWithout2daLookup();

        perk.Name.Should().Be("Provoke");
        perk.Category.Should().Be(PerkCategoryType.ArmorGeneral);

        var provoke1 = perk.PerkLevels[1];
        provoke1.Price.Should().Be(2);
        provoke1.Description.Should().Be("Goads a single target into attacking you. Enmity generated increases by 1% per VIT.");
        provoke1.GrantedFeats.Should().ContainSingle().Which.Should().Be(FeatType.Provoke1);
        AssertSkillRequirement(provoke1, SkillType.Armor, 5);

        var provoke2 = perk.PerkLevels[2];
        provoke2.Price.Should().Be(3);
        provoke2.Description.Should().Be("Goads all enemies within range into attacking you. Enmity generated increases by 1% per VIT.");
        provoke2.GrantedFeats.Should().ContainSingle().Which.Should().Be(FeatType.Provoke2);
        AssertSkillRequirement(provoke2, SkillType.Armor, 15);
    }

    [Test]
    public void ProvokeAbilities_MatchCombatBible()
    {
        var abilities = new ProvokeAbilityDefinition().BuildAbilities();

        var provoke1 = abilities[FeatType.Provoke1];
        provoke1.Name.Should().Be("Provoke I");
        provoke1.AbilityLevel.Should().Be(1);
        provoke1.RecastGroup.Should().Be(RecastGroup.Provoke);
        provoke1.RecastDelay(0).Should().Be(10f);
        provoke1.ActivationDelay(0, 0, 1).Should().Be(1f);
        provoke1.MaxRange.Should().Be(15f);
        provoke1.IsSingleTargetAbility.Should().BeTrue();
        provoke1.IsAreaAbility.Should().BeFalse();
        AssertHostileTargeting(provoke1);

        var provoke2 = abilities[FeatType.Provoke2];
        provoke2.Name.Should().Be("Provoke II");
        provoke2.AbilityLevel.Should().Be(2);
        provoke2.RecastGroup.Should().Be(RecastGroup.Provoke2);
        provoke2.RecastDelay(0).Should().Be(20f);
        provoke2.ActivationDelay(0, 0, 2).Should().Be(1f);
        provoke2.MaxRange.Should().Be(15f);
        provoke2.IsAreaAbility.Should().BeTrue();
        provoke2.IsSingleTargetAbility.Should().BeFalse();
        AssertHostileTargeting(provoke2);
    }

    [Test]
    public void ProvokeFeatAndSpellIcons_AreUniqueAndPresent()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "swlor2_2da" / "spells.2da");

        var provoke1FeatIcon = featRows[1813]["ICON"];
        var provoke2FeatIcon = featRows[1814]["ICON"];
        var provoke1SpellIcon = spellRows[971]["IconResRef"];
        var provoke2SpellIcon = spellRows[972]["IconResRef"];

        provoke1FeatIcon.Should().Be("ife_provoke");
        provoke1SpellIcon.Should().Be(provoke1FeatIcon);
        provoke2FeatIcon.Should().Be("ife_provoke2");
        provoke2SpellIcon.Should().Be(provoke2FeatIcon);
        provoke2FeatIcon.Should().NotBe(provoke1FeatIcon);

        File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{provoke1FeatIcon}.tga").FullName).Should().BeTrue();
        File.Exists((root / "SWLOR_Haks" / "swlor2_tga" / $"{provoke2FeatIcon}.tga").FullName).Should().BeTrue();
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
        ability.RequiresTarget.Should().BeTrue();
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
