using System.Collections;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber;
using SWLOR.Game.Server.Feature.AbilityDefinition.Staff;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class AreaAbilityFragmentationTests
{
    private const uint Creature = 61240;
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    private static Dictionary<uint, CreatureStatusEffect> CreatureEffects =>
        (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", PrivateStatic)!.GetValue(null)!;

    [TearDown]
    public void ClearStatContributors()
    {
        CreatureEffects.Remove(Creature);
        ((IDictionary)typeof(TemporaryStatModifier).GetField("_modifiers", PrivateStatic)!
            .GetValue(null)!).Remove(Creature);
    }

    [TestCase(1, 3)]
    [TestCase(2, 5)]
    [TestCase(3, 7)]
    public void ShrapnelCasing_OnlyAddsFragmentationToThrownAbilities(int rank, int expectedDamage)
    {
        AddShrapnelCasingStats(rank);

        foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Invalid))
        {
            var matches = skill == SkillType.Throwing;
            GetFragmentation(skill, StatType.AreaAbilityFragmentationDamage).Should().Be(matches ? expectedDamage : 0);
            GetFragmentation(skill, StatType.AreaAbilityFragmentationDurationSeconds).Should().Be(matches ? 30 : 0);
            GetFragmentation(skill, StatType.AreaAbilityFragmentationPulseSeconds).Should().Be(matches ? 3 : 0);
        }
    }

    [Test]
    public void RainOfSteel_OnlyAddsTemporaryFragmentationToThrownAbilities()
    {
        var ability = new RainOfSteelAbilityDefinition().BuildAbilities()[FeatType.RainOfSteel1];
        AddTemporaryFragmentation(GetProfile(ability));

        GetFragmentation(SkillType.Throwing, StatType.AreaAbilityFragmentationDamage).Should().Be(8);
        GetFragmentation(SkillType.Throwing, StatType.AreaAbilityFragmentationDurationSeconds).Should().Be(30);
        GetFragmentation(SkillType.Throwing, StatType.AreaAbilityFragmentationPulseSeconds).Should().Be(6);
        GetFragmentation(SkillType.Staff, StatType.AreaAbilityFragmentationDamage).Should().Be(0);
        GetFragmentation(SkillType.TwinBlade, StatType.AreaAbilityFragmentationDamage).Should().Be(0);
    }

    [Test]
    public void GlobalAreaBuff_DoesNotCarryThrowingOnlyTraitStatsIntoOtherSkills()
    {
        AddShrapnelCasingStats(3);
        var ability = new TempestBloomAbilityDefinition().BuildAbilities()[FeatType.TempestBloom1];
        AddTemporaryFragmentation(GetProfile(ability));

        GetFragmentation(SkillType.Staff, StatType.AreaAbilityFragmentationDamage).Should().Be(8);
        GetFragmentation(SkillType.Staff, StatType.AreaAbilityFragmentationDurationSeconds).Should().Be(30);
        GetFragmentation(SkillType.Staff, StatType.AreaAbilityFragmentationPulseSeconds).Should().Be(3);
        GetFragmentation(SkillType.Throwing, StatType.AreaAbilityFragmentationDamage).Should().Be(15);
    }

    [Test]
    public void Worldbreaker_ConfiguresDazedForThirtySecondsOnControlledTargets()
    {
        var ability = new WorldbreakerAbilityDefinition().BuildAbilities()[FeatType.Worldbreaker1];
        var profile = GetProfile(ability);

        GetProperty<Type>(profile, "ConditionalTargetStatusEffect").Should().Be(typeof(DazedStatusEffect));
        GetProperty<int>(profile, "ConditionalTargetStatusDurationSeconds").Should().Be(30);
        GetProperty<StatusEffectCategory>(profile, "RequiredTargetStatusCategoryForConditionalStatus")
            .Should().Be(StatusEffectCategory.Control);
        GetProperty<int>(profile, "ExtraDamageIfTargetControlled").Should().Be(40);
        GetProperty<int>(profile, "TemporaryAreaAbilityFragmentationDamage").Should().Be(0);
        ability.SkillType.Should().Be(SkillType.Staff);
        ability.IsAreaAbility.Should().BeTrue();
    }

    [TestCase(FeatType.SerratedArc1)]
    [TestCase(FeatType.SerratedArc2)]
    [TestCase(FeatType.SerratedArc3)]
    public void SerratedArc_ConfiguresBleedSpreadWithoutFragmentation(FeatType feat)
    {
        var ability = new SerratedArcAbilityDefinition().BuildAbilities()[feat];
        var profile = GetProfile(ability);

        GetProperty<bool>(profile, "SpreadBleedFromTarget").Should().BeTrue();
        GetProperty<int>(profile, "SpreadBleedDurationSeconds").Should().Be(30);
        GetProperty<int>(profile, "MaximumStatusSpreadsPerCast").Should().Be(0);
        GetProperty<int>(profile, "TemporaryAreaAbilityFragmentationDamage").Should().Be(0);
        ability.SkillType.Should().Be(SkillType.TwinBlade);
        ability.IsAreaAbility.Should().BeTrue();
    }

    [TestCase(FeatType.SunderingSweep1)]
    [TestCase(FeatType.SunderingSweep2)]
    [TestCase(FeatType.SunderingSweep3)]
    public void SunderingSweep_LimitsSunderToOneSpreadPerCast(FeatType feat)
    {
        var profile = GetProfile(new SunderingSweepAbilityDefinition().BuildAbilities()[feat]);
        GetProperty<bool>(profile, "SpreadSunderFromTarget").Should().BeTrue();
        GetProperty<int>(profile, "MaximumStatusSpreadsPerCast").Should().Be(1);
    }

    private static void AddShrapnelCasingStats(int rank)
    {
        const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        var definition = new ThrowingPerkDefinition();
        typeof(ThrowingPerkDefinition).GetMethod("ShrapnelCasing", PrivateInstance)!.Invoke(definition, null);
        var builder = typeof(ThrowingPerkDefinition).GetField("_builder", PrivateInstance)!.GetValue(definition);
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", PrivateInstance)!.GetValue(builder)!;
        var perk = perks[PerkType.ShrapnelCasing];
        var tracker = new CreatureStatusEffect();
        foreach (var bonus in perk.PerkLevels[rank].StatBonuses)
            tracker.StatGroup.Stats[bonus.Stat] = bonus.Calculate(Creature);
        CreatureEffects[Creature] = tracker;
    }

    private static void AddTemporaryFragmentation(object profile)
    {
        var add = typeof(TemporaryStatModifier).GetMethod("AddInternal", PrivateStatic)!;
        foreach (var stat in new[]
                 {
                     StatType.AreaAbilityFragmentationSkillType,
                     StatType.AreaAbilityFragmentationDamage,
                     StatType.AreaAbilityFragmentationDurationSeconds,
                     StatType.AreaAbilityFragmentationPulseSeconds
                 })
        {
            var value = Convert.ToInt32(profile.GetType().GetProperty($"Temporary{stat}")!.GetValue(profile));
            add.Invoke(null, new object[] { Creature, stat, value, 45f, "FragmentationTest", false });
        }
    }

    private static int GetFragmentation(SkillType skill, StatType stat) =>
        (int)typeof(Combat).GetMethod("GetAreaAbilityFragmentationStatAdjustment", PrivateStatic)!
            .Invoke(null, new object[] { Creature, skill, stat })!;

    private static object GetProfile(AbilityDetail ability) =>
        ability.ImpactAction.Target!.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(field => field.FieldType.Name == "GeneratedWeaponAbilityProfile")
            .GetValue(ability.ImpactAction.Target)!;

    private static T GetProperty<T>(object profile, string name) =>
        (T)profile.GetType().GetProperty(name)!.GetValue(profile)!;
}
