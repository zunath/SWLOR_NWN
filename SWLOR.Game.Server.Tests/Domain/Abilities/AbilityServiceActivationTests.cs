using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>The catalog and the validation of an activation (<c>CanUseAbility</c>), checked in its exact order.</summary>
[TestFixture]
public class AbilityServiceActivationTests : AbilityServiceTestBase
{
    private const FeatType Feat = FeatType.Flash1;

    private sealed class Requirement : IAbilityActivationRequirement
    {
        public string Error { get; set; } = string.Empty;

        public string CheckRequirements(uint player, AbilityDetail ability = null!) => Error;

        public void AfterActivationAction(uint player, AbilityDetail ability = null!)
        {
        }
    }

    [Test]
    public void The_catalog_is_empty_until_the_definitions_are_cached()
    {
        Service.GetAllAbilityDetails().Should().BeEmpty();
        Service.IsFeatRegistered(Feat).Should().BeFalse();
    }

    [Test]
    public void An_unregistered_feat_has_no_detail()
    {
        var read = () => Service.GetAbilityDetail(Feat);

        read.Should().Throw<KeyNotFoundException>().WithMessage($"Feat '{Feat}' is not registered to an ability.");
    }

    [Test]
    public void Caching_builds_every_definition_binds_the_animations_and_fills_the_targeting_cache()
    {
        Service.DefinitionTypes = Service.DefaultDefinitionTypes().ToList();

        Service.CacheData();

        Service.IsFeatRegistered(FeatType.Flash1).Should().BeTrue();
        Service.GetAbilityDetail(FeatType.Flash1).Should().BeSameAs(Service.GetAllAbilityDetails()[FeatType.Flash1]);
        Service.CachedTargeting.Should().BeSameAs(Service.GetAllAbilityDetails());
        Bench.Log.Should().Contain("CacheAbilityTargeting");
    }

    [Test]
    public void Caching_again_replaces_the_catalog()
    {
        Service.DefinitionTypes = Service.DefaultDefinitionTypes().ToList();
        Service.Register(FeatType.Invalid, new AbilityDetail());

        Service.CacheData();

        Service.IsFeatRegistered(FeatType.Invalid).Should().BeFalse();
    }

    [Test]
    public void The_definition_search_finds_the_definitions_of_the_server()
    {
        Service.DefaultDefinitionTypes().Should().Contain(typeof(ForcePushAbilityDefinition));
    }

    /// <summary>
    /// Every check fails at the start. Clearing them one at a time shows which refusal comes next, so a reordered check
    /// (a dead caster told about its recast, say) fails here.
    /// </summary>
    [Test]
    public void The_checks_run_in_order_and_the_first_failure_is_the_refusal()
    {
        var requirement = new Requirement { Error = "Requirement failed." };
        string customError = "Custom failed.";
        var ability = new AbilityDetail
        {
            Name = "Probe",
            AbilityLevel = 2,
            RequiresTarget = true,
            IsHostileAbility = true,
            MaxRange = 5f,
            IsMimicryTechnique = true,
            IsForcePower = true,
            SkillType = SkillType.Vibroblade,
            ActivationType = AbilityActivationType.Weapon,
            RecastGroup = RecastGroup.Flash,
            Requirements = new List<IAbilityActivationRequirement> { requirement },
            CustomValidation = (_, _, _, _) => customError,
            Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 5f, 0f,
                AbilityTargetingFlags.HarmsEnemies, null!)
        };
        Service.Register(Feat, ability);
        var location = Bench.At(AbilityBench.TheArea, Bench.Positions[Target]);

        var level = 1;
        Service.SpaceMode.Add(Caster);
        Service.EnforcesFeatReplacement = true;
        Service.RepliesActiveFeat = false;
        Bench.TechniqueEquipped = false;
        Bench.Hp[Caster] = 0;
        Bench.NotCommandable.Add(Caster);
        Bench.NoLineOfSight.Add(Target);
        Bench.Busy.Add(Caster);
        Service.HasEquippedWeapon = false;
        Service.EquipmentError = "Bad equipment.";
        Service.Stats[(Caster, StatType.ForceAbilityActivationDisabled)] = 1;
        Bench.Positions[Target] = new Vector3(50f, 0f, 0f);
        Bench.Hostile.Remove((Target, Caster));
        Bench.TechniqueError = "Technique failed.";
        Bench.RecastAnswer = (true, "3 seconds");

        void Next(string expected, Action clear)
        {
            Service.CanUseAbility(Caster, Target, Feat, level, location).Should().BeFalse(expected);
            Service.GetLastActivationDenialReason().Should().Be(expected);
            Bench.Messages.Last().Should().Be((Caster, expected));
            clear();
        }

        Next("This ability cannot be used in space.", () => Service.SpaceMode.Clear());
        Next("You do not meet the prerequisites to use this ability.", () => level = 2);
        Next("A newer rank has replaced this ability.", () => Service.RepliesActiveFeat = true);
        Next("That technique is not equipped.", () => Bench.TechniqueEquipped = true);
        Next("You are dead.", () => Bench.Hp[Caster] = 10);
        Next("You cannot take actions at this time.", () => Bench.NotCommandable.Clear());
        Next("You cannot see your target.", () => Bench.NoLineOfSight.Clear());
        Next("You are busy.", () => Bench.Busy.Clear());
        Next("You must equip a weapon to use this ability.", () => Service.HasEquippedWeapon = true);
        Next("Bad equipment.", () => Service.EquipmentError = string.Empty);
        Next("You cannot use Force abilities right now.", () => Service.Stats.Clear());
        Next("You are out of range.  This ability has a range of 5 meters.", () => Bench.Positions[Target] = new Vector3(3f, 0f, 0f));
        Next("You may only use this ability on enemies.", () => Bench.Hostile.Add((Target, Caster)));
        Next("Technique failed.", () => Bench.TechniqueError = string.Empty);
        Next("Requirement failed.", () => requirement.Error = string.Empty);
        Next("Custom failed.", () => customError = string.Empty);

        Bench.Log.Clear();
        Next("This ability can be used in 3 seconds.", () => Bench.RecastAnswer = (false, string.Empty));
        Bench.Log.Should().ContainInOrder(
            $"IsOnRecastDelay:{Caster}:{RecastGroup.Flash}",
            $"CooldownText:{Caster}:Probe:3 seconds");

        Service.CanUseAbility(Caster, Target, Feat, level, location).Should().BeTrue();
        Service.GetLastActivationDenialReason().Should().BeEmpty("every check clears the last reason first");
    }

    [Test]
    public void A_rank_that_is_not_enforced_passes_the_replacement_check()
    {
        Service.Register(Feat, Hostile());
        Service.RepliesActiveFeat = false;

        Service.CanUseAbility(Caster, Target, Feat, 1, Bench.Nowhere()).Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void A_perk_level_below_one_never_passes(int level)
    {
        var ability = Hostile();
        ability.AbilityLevel = 0;
        Service.Register(Feat, ability);

        Service.CanUseAbility(Caster, Target, Feat, level, Bench.Nowhere()).Should().BeFalse();
        Service.GetLastActivationDenialReason().Should().Be("You do not meet the prerequisites to use this ability.");
    }

    [Test]
    public void A_required_target_must_exist_and_only_refuses_after_the_force_check()
    {
        var ability = Hostile();
        ability.RequiresTarget = true;
        Service.Register(Feat, ability);

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.Nowhere()).Should().BeFalse();

        Service.GetLastActivationDenialReason().Should().Be("A target is required.");
    }

    [Test]
    public void A_foreign_force_power_is_refused_while_force_abilities_are_disabled()
    {
        var ability = Hostile(skill: SkillType.Devices);
        ability.IsForcePower = true;
        Service.Register(Feat, ability);
        Service.Stats[(Caster, StatType.ForceAbilityActivationDisabled)] = 1;

        Service.CanUseAbility(Caster, Target, Feat, 1, Bench.Nowhere()).Should().BeFalse();

        Service.GetLastActivationDenialReason().Should().Be("You cannot use Force abilities right now.");
    }

    [Test]
    public void Equipment_is_checked_for_a_hostile_weapon_skill_ability_even_when_casted()
    {
        Service.Register(Feat, Hostile(skill: SkillType.Vibroblade));
        Service.EquipmentError = "Bad equipment.";

        Service.CanUseAbility(Caster, Target, Feat, 1, Bench.Nowhere()).Should().BeFalse();

        Service.GetLastActivationDenialReason().Should().Be("Bad equipment.");
    }

    [Test]
    public void A_location_target_must_be_in_the_casters_area_and_in_range()
    {
        var ability = new AbilityDetail
        {
            Name = "Aimed",
            IsAreaAbility = true,
            HasExplicitMaxRange = true,
            MaxRange = 10f,
            ActivationType = AbilityActivationType.Casted,
            Requirements = new List<IAbilityActivationRequirement>(),
            Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Cone, 8f, 8f, AbilityTargetingFlags.None, null!)
        };
        Service.Register(Feat, ability);

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.Nowhere()).Should().BeFalse();
        Service.GetLastActivationDenialReason().Should().Be("A target location in your current area is required.");

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.At(AbilityBench.TheArea + 1, Vector3.Zero)).Should().BeFalse();
        Service.GetLastActivationDenialReason().Should().Be("A target location in your current area is required.");

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.At(AbilityBench.TheArea, new Vector3(30f, 0f, 0f))).Should().BeFalse();
        Service.GetLastActivationDenialReason().Should().Be("You are out of range.  This ability has a range of 10 meters.");

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.At(AbilityBench.TheArea, new Vector3(5f, 0f, 0f))).Should().BeTrue();
    }

    [Test]
    public void A_harmful_area_with_enemies_needs_sight_of_at_least_one_of_them()
    {
        var ability = new AbilityDetail
        {
            Name = "Burst",
            IsHostileAbility = true,
            RequiresTarget = true,
            MaxRange = 20f,
            ActivationType = AbilityActivationType.Casted,
            Requirements = new List<IAbilityActivationRequirement>(),
            Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 5f, 0f, AbilityTargetingFlags.HarmsEnemies, null!)
        };
        Service.Register(Feat, ability);
        Bench.CreaturesInArea.AddRange(new[] { Target, Other });
        Bench.NoLineOfSight.Add(Other);
        Bench.Positions[Other] = new Vector3(4f, 1f, 0f);

        Service.CanUseAbility(Caster, Target, Feat, 1, Bench.Nowhere()).Should().BeTrue("the target itself can be seen");

        Bench.Positions[Target] = new Vector3(3f, 0f, 0f);
        Bench.NoLineOfSight.Clear();
        Bench.Service.GetLastActivationDenialReason().Should().BeEmpty();
    }

    [Test]
    public void A_harmful_area_whose_enemies_are_all_hidden_from_sight_is_refused()
    {
        var ability = new AbilityDetail
        {
            Name = "Burst",
            IsHostileAbility = true,
            IsAreaAbility = true,
            ActivationType = AbilityActivationType.Casted,
            Requirements = new List<IAbilityActivationRequirement>(),
            Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 8f, 0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf, null!)
        };
        Service.Register(Feat, ability);
        Bench.CreaturesInArea.Add(Target);
        Bench.NoLineOfSight.Add(Target);

        Service.CanUseAbility(Caster, ObjectInvalid, Feat, 1, Bench.Nowhere()).Should().BeFalse();

        Service.GetLastActivationDenialReason().Should().Be("You cannot see any enemies in the target area.");
    }

    [TestCase(AbilityTargetingShapeType.None)]
    [TestCase(AbilityTargetingShapeType.Rect)]
    [TestCase(AbilityTargetingShapeType.Cone)]
    [TestCase(AbilityTargetingShapeType.HSphere)]
    public void The_area_sight_check_reads_each_targeting_shape(AbilityTargetingShapeType shape)
    {
        var ability = Hostile();
        ability.Targeting = new AbilityTargetingDetail(Spell.Invalid, shape, 5f, 2f, AbilityTargetingFlags.HarmsEnemies, null!);
        Service.Register(Feat, ability);

        Service.CanUseAbility(Caster, Target, Feat, 1, Bench.At(AbilityBench.TheArea, Vector3.One)).Should().BeTrue("no creature is in the area");
    }

    [Test]
    public void A_force_power_is_a_force_skill_or_a_flagged_ability()
    {
        Service.IsForcePower(true, SkillType.Devices).Should().BeTrue();
        Service.IsForcePower(false, SkillType.Force).Should().BeTrue();
        Service.IsForcePower(false, SkillType.Devices).Should().BeFalse();
        Service.IsForcePower(Caster, Hostile(skill: SkillType.Force)).Should().BeTrue();
        Service.IsForcePower(Caster, Hostile(skill: SkillType.Devices)).Should().BeFalse();
    }
}
