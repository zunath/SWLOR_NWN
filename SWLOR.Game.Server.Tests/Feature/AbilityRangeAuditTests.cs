using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityRangeAuditTests
{
    [Test]
    public void TargetedRangedPlayerAbilities_HaveReviewedRanges()
    {
        var abilities = BuildAllAbilities();
        var expectedRangesBySkill = new Dictionary<SkillType, float>
        {
            [SkillType.Devices] = 15f,
            [SkillType.Leadership] = 15f,
            [SkillType.Pistol] = 25f,
            [SkillType.Rifle] = 30f,
            [SkillType.Throwing] = 20f,
        };

        var auditedAbilities = abilities
            .Where(entry =>
                entry.Value.RequiresTarget &&
                !entry.Value.IsAreaAbility &&
                expectedRangesBySkill.ContainsKey(entry.Value.SkillType))
            .ToArray();

        auditedAbilities.Should().NotBeEmpty();

        foreach (var (feat, ability) in auditedAbilities)
        {
            ability.MaxRange.Should().Be(
                expectedRangesBySkill[ability.SkillType],
                $"{feat} is a targeted {ability.SkillType} ability and must not inherit the 5m melee default");
        }
    }

    [Test]
    public void TargetedForceAbilities_HaveNonMeleeRanges()
    {
        var abilities = BuildAllAbilities()
            .Where(entry =>
                entry.Value.RequiresTarget &&
                !entry.Value.IsAreaAbility &&
                entry.Value.SkillType == SkillType.Force)
            .ToArray();

        abilities.Should().NotBeEmpty();

        foreach (var (feat, ability) in abilities)
        {
            ability.MaxRange.Should().BeGreaterThan(5f, $"{feat} is a targeted Force ability");
        }
    }

    [Test]
    public void NPCProjectileAbilities_HaveReviewedRanges()
    {
        var abilities = BuildAllAbilities();

        abilities[FeatType.ToxicSpit].MaxRange.Should().Be(8f);
        abilities[FeatType.StimCanister].MaxRange.Should().Be(8f);
    }

    private static Dictionary<FeatType, AbilityDetail> BuildAllAbilities()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var abilities = new Dictionary<FeatType, AbilityDetail>();

        var definitions = definitionType.Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                definitionType.IsAssignableFrom(type))
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!);

        foreach (var definition in definitions)
        {
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities[feat] = ability;
            }
        }

        return abilities;
    }
}
