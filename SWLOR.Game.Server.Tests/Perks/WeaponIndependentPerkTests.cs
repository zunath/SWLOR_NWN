using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class WeaponIndependentPerkTests
{
    [Test]
    public void EveryPerk_DeclaresWeaponIndependentAutoAttackAndCriticalTriggers()
    {
        var triggerStats = new[]
        {
            StatType.SourceStatusAutoAttackCycleSkillType,
            StatType.AutoAttackSplashSkillType,
            StatType.OpeningAutoAttackSkillType,
            StatType.CriticalNextAutoAttackNoDelayTriggerSkillType,
            StatType.CriticalNextAutoAttackNoDelaySkillType,
            StatType.CriticalHitLimitedHasteTriggerSkillType,
            StatType.SameTargetPressureBuildSkillType
        };
        var reviewed = 0;
        foreach (var type in typeof(IPerkListDefinition).Assembly.GetTypes()
                     .Where(type => !type.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(type)))
        {
            // Populate definitions without PerkBuilder.Build's native feat/2DA lookup.
            var definition = Activator.CreateInstance(type)!;
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 && !method.Name.Contains('<'))
                         .OrderBy(method => method.MetadataToken))
                method.Invoke(definition, null);
            var builder = type.GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(definition)!;
            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(builder)!;
            foreach (var perk in perks.Values)
            foreach (var rank in perk.PerkLevels)
            {
                reviewed++;
                foreach (var bonus in rank.Value.StatBonuses.Where(bonus => triggerStats.Contains(bonus.Stat)))
                    bonus.Calculate(0).Should().Be((int)SkillType.Invalid,
                        $"{perk.Name} rank {rank.Key} must not require a weapon family for {bonus.Stat}");
            }
        }
        reviewed.Should().BeGreaterThan(100);
        TestContext.Out.WriteLine($"Reviewed {reviewed} perk ranks across every perk definition.");
    }

    [Test]
    public void BackAttack_UsesWeaponEligibilityAndPreservesPositioning()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server", "Service", "Combat.cs")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory!.FullName, "SWLOR.Game.Server", "Service", "Combat.cs"));
        var start = source.IndexOf("private static bool IsMatchingBackAttack(", StringComparison.Ordinal);
        var end = source.IndexOf("public static int ApplyBackAttackDamageModifier(", start, StringComparison.Ordinal);
        var eligibility = source[start..end];
        eligibility.Should().Contain("IsWeaponSkillType(skillType)");
        eligibility.Should().Contain("IsAttackerBehindTarget(attacker, defender)");
        eligibility.Should().NotContain("IsRangedWeaponSkill");
    }

    [Test]
    public void DeadMansHand_ConsumesThreeChargesAcrossDifferentWeaponFamilies()
    {
        var effect = new DeadMansHandStatusEffect();
        foreach (var skill in Enum.GetValues<SkillType>())
            effect.AppliesToSkill(skill).Should().Be(Combat.IsWeaponSkillType(skill));
        effect.OnAttackAttemptedEffect(1, SkillType.Force, null);
        effect.RemainingAttacks.Should().Be(3);
        foreach (var skill in new[] { SkillType.Lightsaber, SkillType.Rifle, SkillType.Throwing })
            effect.OnAttackAttemptedEffect(1, skill, null);
        effect.RemainingAttacks.Should().Be(0);
        effect.IsFlaggedForRemoval.Should().BeTrue();
    }
}
