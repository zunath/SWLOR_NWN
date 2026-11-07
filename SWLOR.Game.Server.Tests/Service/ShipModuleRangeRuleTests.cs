using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class ShipModuleRangeRuleTests
{
    private static readonly Dictionary<ShipModuleType, ShipModuleRangeRule> SelfCapableModules = new()
    {
        // Enemy targets redirect the repair to the user's ship, but friendly targets are still acted on.
        [ShipModuleType.ShieldRepairer] = ShipModuleRangeRule.NonEnemyTarget,
        [ShipModuleType.HullRepairer] = ShipModuleRangeRule.NonEnemyTarget,

        // Modules that act on or around the user's ship never read the selected target. The Hypermatter Injector only
        // refills its own ship's capacitor, and the Laser Battery fires at its target, so it is no self module here.
        [ShipModuleType.HypermatterInjector] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.RepairFieldGenerator] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.BulwarkShieldGenerator] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.CapitalEwar] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.ShieldHardener] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.ProbeLauncher] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.CloakingDevice] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.Afterburner] = ShipModuleRangeRule.NoTarget,
    };

    private static IReadOnlyList<ShipModuleDetail> AllModules() => typeof(Space).Assembly.GetTypes()
        .Where(type => typeof(IShipModuleListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
        .SelectMany(type => ((IShipModuleListDefinition)Activator.CreateInstance(type)).BuildShipModules().Values)
        .ToList();

    [TestCase(ShipModuleRangeRule.AnyTarget, false, true)]
    [TestCase(ShipModuleRangeRule.AnyTarget, true, true)]
    [TestCase(ShipModuleRangeRule.NonEnemyTarget, false, true)]
    [TestCase(ShipModuleRangeRule.NonEnemyTarget, true, false)]
    [TestCase(ShipModuleRangeRule.NoTarget, false, false)]
    [TestCase(ShipModuleRangeRule.NoTarget, true, false)]
    public void RequiresTargetInRange_FollowsTheRule(ShipModuleRangeRule rule, bool targetIsEnemy, bool expected)
    {
        new ShipModuleDetail { RangeRule = rule }.RequiresTargetInRange(targetIsEnemy).Should().Be(expected);
    }

    [TestCase(ShipModuleRangeRule.AnyTarget, false)]
    [TestCase(ShipModuleRangeRule.NonEnemyTarget, true)]
    [TestCase(ShipModuleRangeRule.NoTarget, true)]
    public void FallsBackToOwnShip_FollowsTheRule(ShipModuleRangeRule rule, bool expected)
    {
        new ShipModuleDetail { RangeRule = rule }.FallsBackToOwnShip.Should().Be(expected);
    }

    [Test]
    public void TargetedWeapons_CannotFireWithoutATarget()
    {
        // Turbolaser damage reads the target's ship, so it must never activate with nothing selected.
        AllModules().Where(module => module.Type == ShipModuleType.TurboLaser)
            .Should().NotBeEmpty()
            .And.OnlyContain(module => !module.CanTargetSelf && module.ValidTargetTypes.Contains(ObjectType.Creature));
    }

    [Test]
    public void EverySelfCapableModule_HasTheRuleForItsBehavior()
    {
        var selfCapable = AllModules().Where(module => module.CanTargetSelf).ToList();

        selfCapable.Select(module => module.Type).Distinct()
            .Should().BeEquivalentTo(SelfCapableModules.Keys, "a new self-capable module type must choose its range rule");

        foreach (var module in selfCapable)
            module.RangeRule.Should().Be(SelfCapableModules[module.Type], module.Name);
    }

    [Test]
    public void ModulesThatCannotTargetSelf_AlwaysRequireTheirTargetInRange()
    {
        foreach (var module in AllModules().Where(module => !module.CanTargetSelf))
            module.RangeRule.Should().Be(ShipModuleRangeRule.AnyTarget, module.Name);
    }
}
