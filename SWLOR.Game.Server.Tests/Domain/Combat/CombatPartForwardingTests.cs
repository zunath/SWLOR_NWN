using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// Static code reaches each part of combat (and <c>ICombatService</c>, what is left of it) through the property of its name on
/// <c>GameServices</c> (P2-ZP removed the facade that forwarded to them). These tests pin what the facade's callers relied
/// on: the defaults each member binds, the handlers on the part that owns them, and the shared tables the parts read through
/// each other. <c>FacadeForwardingTests</c> proves each <c>GameServices</c> property resolves the installed instance.
/// </summary>
[TestFixture]
public class CombatPartForwardingTests
{
    private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> Parts() => new[]
    {
        typeof(ICombatAbilityActivationService),
        typeof(ICombatAbilityBonusService),
        typeof(ICombatAbilityHitService),
        typeof(ICombatAbilityImpactService),
        typeof(ICombatActivityService),
        typeof(ICombatAttackTimingService),
        typeof(ICombatDamageModifiersService),
        typeof(ICombatDamageTakenService),
        typeof(ICombatFeedbackService),
        typeof(ICombatHitEffectsService),
        typeof(ICombatQueriesService),
        typeof(ICombatResourcesService),
        typeof(ICombatService),
        typeof(ICombatSuppressionService),
    };

    // The methods of a part; a property (the activity part's shared table) is checked by its own test below.
    private static MethodInfo[] Members(Type part) => part.GetMethods().Where(method => !method.IsSpecialName).ToArray();

    [Test]
    public void OnlyTheActivityPartHasAProperty()
    {
        Parts().SelectMany(part => part.GetProperties().Select(property => $"{part.Name}.{property.Name}"))
            .Should().Equal("ICombatActivityService.RecentDamageTargets");
        Parts().SelectMany(part => part.GetMethods().Where(method => method.IsSpecialName).Select(method => method.Name))
            .Should().Equal("get_RecentDamageTargets");
    }

    // The instance class keeps its interface's defaults, so a call made on the instance binds the values a call through
    // Combat binds.
    [TestCaseSource(nameof(Parts))]
    public void ThePartServiceKeepsTheDefaultsOfItsInterface(Type part)
    {
        var service = part.Assembly.GetTypes().Single(type => type.IsClass && !type.IsAbstract && part.IsAssignableFrom(type));
        var map = service.GetInterfaceMap(part);
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
        {
            var declared = map.InterfaceMethods[i].GetParameters();
            var implemented = map.TargetMethods[i].GetParameters();
            for (var p = 0; p < declared.Length; p++)
            {
                var label = $"{service.Name}.{map.TargetMethods[i].Name}({declared[p].Name})";
                implemented[p].HasDefaultValue.Should().Be(declared[p].HasDefaultValue, label);
                if (declared[p].HasDefaultValue)
                    implemented[p].DefaultValue.Should().Be(declared[p].DefaultValue, label);
            }
        }
    }

    // The members of the ability bonus and damage modifier parts that have default arguments, with the defaults they had
    // as static members of Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatAbilityBonusService), "GetNextSkillAbilityStaminaCostAdjustment(isHostileAbility=False)")]
    [TestCase(typeof(ICombatAbilityBonusService), "ConsumeNextSkillAbilityStaminaCostAdjustment(isHostileAbility=False)")]
    [TestCase(typeof(ICombatAbilityBonusService), "ConsumeNextSkillAbilityBonuses(abilityStatusCategories=None)")]
    [TestCase(typeof(ICombatAbilityBonusService), "GetQueuedWeaponAbilityActivationHitChanceAdjustment(ability=null)")]
    [TestCase(typeof(ICombatDamageModifiersService), "CalculateDamageWithCriticalMitigation(deltaCap=0)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyCriticalDamageModifier(skillType=Invalid, defender=2130706432, criticalDamagePercentAdjustment=0)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyDamageDealtModifiers(abilityDamagePercentAdjustment=0, lowHPAbilityDamagePercentAdjustment=0, isOrdinaryWeaponAttack=False)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyDamageDealtModifiers(skillType=Invalid, damageType=Physical, isAbilityDamage=False, canApplyRandomFlatBonuses=True, isLandedAttack=True, ability=null)")]
    public void TheMovedDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 8, typeof(ICombatAbilityBonusService), typeof(ICombatDamageModifiersService));

    // The members of the ability hit and hit effects parts that have default arguments, with the defaults they had as static
    // members of Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatAbilityHitService), "CalculateAbilityCriticalRating(criticalRateAdjustment=0, defender=2130706432)")]
    [TestCase(typeof(ICombatAbilityHitService), "GetAbilityCriticalRate(criticalRateAdjustment=0, defender=2130706432)")]
    [TestCase(typeof(ICombatAbilityHitService), "TryResolveAbilityHit(hitChancePercentAdjustment=0, skillLevelOverride=-1, statOverride=Invalid, accuracyFlatAdjustment=0)")]
    [TestCase(typeof(ICombatHitEffectsService), "ApplyAutoAttackDamageModifiers(skillType=Invalid)")]
    [TestCase(typeof(ICombatHitEffectsService), "ApplyDamageDealtEffects(skillType=Invalid, damageType=Physical, deliveryType=Direct, isAbilityDamage=False, allowsHitProcs=True)")]
    [TestCase(typeof(ICombatHitEffectsService), "ApplyCriticalHitEffects(isSingleTargetImpact=False, skillType=Invalid)")]
    [TestCase(typeof(ICombatHitEffectsService), "ApplyDefeatedEnemyEffects(defeated=2130706432)")]
    [TestCase(typeof(ICombatHitEffectsService), "ApplyTriggeredDamage(skillType=Invalid, typedLeadershipReductionAlreadyApplied=False, targetStatusDamagePercentAdjustment=null, outgoingModifiersAlreadyApplied=False)")]
    public void TheMovedHitDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 8, typeof(ICombatAbilityHitService), typeof(ICombatHitEffectsService));

    // The members of the damage taken part that have default arguments, with the defaults they had as static members of
    // Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatDamageTakenService), "ApplyDamageTakenModifiers(attacker=2130706432, damageType=Physical, deliveryType=Direct, targetStatusDamagePercentAdjustment=0, isLandedAttack=True, typedLeadershipReductionAlreadyApplied=False)")]
    [TestCase(typeof(ICombatDamageTakenService), "ApplyIncomingPhysicalToForceConversion(targetStatusDamagePercentAdjustment=0, convertedDamageScheduler=null)")]
    [TestCase(typeof(ICombatDamageTakenService), "ApplyAbilityUsedAttackDeflection(deflectionFPRestoreStatType=Invalid)")]
    public void TheMovedDamageTakenDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 3, typeof(ICombatDamageTakenService));

    // The one member of the ability activation part that has a default argument, with the default it had as a static member
    // of Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatAbilityActivationService), "ApplyHitPointSpendAbilityEffects(hitPointCost=0)")]
    public void TheMovedActivationDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 1, typeof(ICombatAbilityActivationService));

    // The members of the ability impact part that have default arguments, with the defaults they had as static members of
    // Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatAbilityImpactService), "GetHighResourceAbilityDamageBonus(ability=null)")]
    [TestCase(typeof(ICombatAbilityImpactService), "ApplySuccessfulAbilityImpactRiders(allowsHitProcs=True, targetWasAttackingAlly=False)")]
    public void TheMovedImpactDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 2, typeof(ICombatAbilityImpactService));

    // The one member of what is left of Combat (CombatService) that has a default argument, with the default it had as a static
    // member of Combat, on the interface and the instance class alike.
    [TestCase(typeof(ICombatService), "CalculateDamage(deltaCap=0)")]
    public void TheMovedServiceDefaultsKeepTheirValues(Type part, string expected) =>
        AssertMovedDefaults(part, expected, 1, typeof(ICombatService));

    private static string Defaults(MethodInfo method) => $"{method.Name}(" + string.Join(", ", method.GetParameters()
        .Where(parameter => parameter.HasDefaultValue)
        .Select(parameter => $"{parameter.Name}={parameter.DefaultValue ?? "null"}")) + ")";

    // The member of the part with these defaults has them on the instance class too, and the parts of one pull request have
    // exactly the members it lists.
    private static void AssertMovedDefaults(Type part, string expected, int listed, params Type[] pullRequestParts)
    {
        var service = part.Assembly.GetTypes().Single(type => type.IsClass && !type.IsAbstract && part.IsAssignableFrom(type));
        var members = part.GetMethods().Where(method => Defaults(method) == expected).ToArray();
        members.Should().ContainSingle(expected);
        var member = members[0];
        var map = service.GetInterfaceMap(part);
        Defaults(map.TargetMethods[Array.IndexOf(map.InterfaceMethods, member)]).Should().Be(expected, service.Name);
        NWScript.OBJECT_INVALID.Should().Be(2130706432u);

        var all = Parts().Where(pullRequestParts.Contains)
            .SelectMany(type => type.GetMethods())
            .Where(method => method.GetParameters().Any(parameter => parameter.HasDefaultValue))
            .Select(Defaults);
        all.Should().HaveCount(listed, $"the PR lists these {listed} members");
    }

    [Test]
    public void TheRecentDamageTargetsTheHitEffectsAndDamageTakenPartsReadAreTheActivityPartsOwnTable()
    {
        // Both readers of the table take the activity part.
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        var activity = facades.Get<ICombatActivityService>();
        const BindingFlags Instance = BindingFlags.NonPublic | BindingFlags.Instance;

        object table = activity.RecentDamageTargets;
        table.Should().BeSameAs(typeof(CombatActivityService).GetField("_recentDamageTargets", Instance)!.GetValue(activity));
        ((object)activity.RecentDamageTargets).Should().BeSameAs(table, "every read returns the same table, so a prune is seen by the next read");
        typeof(CombatHitEffectsService).GetField("_combatActivity", Instance)!.GetValue(facades.Get<ICombatHitEffectsService>())
            .Should().BeSameAs(activity);
        typeof(CombatDamageTakenService).GetField("_combatActivity", Instance)!.GetValue(facades.Get<ICombatDamageTakenService>())
            .Should().BeSameAs(activity);
    }

    [TestCase(typeof(CombatDamageTakenService), "ApplyDamageTakenEffects", ScriptName.OnCreatureDamagedAfter)]
    [TestCase(typeof(CombatService), "ApplyDefeatedEnemyEffects", ScriptName.OnCreatureDeathBefore)]
    [TestCase(typeof(CombatService), "ClearStatTriggerState", ScriptName.OnModuleExit)]
    [TestCase(typeof(CombatService), "ClearCombatState", ScriptName.OnIntervalPC6Seconds)]
    [TestCase(typeof(CombatActivityService), "PurgeInvalidOffensiveActivity", ScriptName.OnModuleHeartbeat)]
    [TestCase(typeof(CombatActivityService), "RestoreIdleReadinessOnPlayerEnter", ScriptName.OnModuleEnter)]
    [TestCase(typeof(CombatDamageCatalogService), "LoadDamageTypes", ScriptName.OnModuleCacheBefore)]
    [TestCase(typeof(CombatDamageCatalogService), "AddDamageTypeResistances", ScriptName.OnModuleEnter)]
    public void EachHandlerOfTheRemovedFacadeIsOnTheClassThatOwnsItUnderItsName(Type service, string name, string script)
    {
        var handler = service.GetMethod(name, BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes)!;
        handler.GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(script);
        service.GetInterfaces().Should().Contain(i => i.GetMethod(name, Type.EmptyTypes) != null, "the handler runs on the part the container holds");
    }

    [Test]
    public void TheFacadeIsGone()
    {
        typeof(CombatRules).Assembly.GetType("SWLOR.Game.Server.Service.Combat").Should().BeNull("P2-ZP removed the facade");
    }

    [Test]
    public void TheMovedConstantsKeepTheirValuesOnTheirOwners()
    {
        CombatQueriesService.BossMinimumHitRate.Should().Be(35);
        CombatResourcesService.MaximumActivatedDamageHealingMaxHPPercent.Should().Be(15);
        CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerHit.Should().Be(6);
        CombatResourcesService.MaximumCooldownDamageHealingMaxHPPercentPerHit.Should().Be(12);
        CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerWindow.Should().Be(24);
        CombatResourcesService.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow.Should().Be(12);
        CombatResourcesService.CombatHealingWindowSeconds.Should().Be(6);
        CombatDamageTakenService.BaseGuardDamageReductionPercent.Should().Be(20);
        CombatDamageTakenService.MaximumGuardDamageReductionPercent.Should().Be(55);
    }

}
