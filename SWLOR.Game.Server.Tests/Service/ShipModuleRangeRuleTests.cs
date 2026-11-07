using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class ShipModuleRangeRuleTests
{
    private static readonly Dictionary<ShipModuleType, ShipModuleRangeRule> SelfCapableModules = new()
    {
        // Enemy targets redirect the effect to the user's ship, but friendly targets are still acted on.
        [ShipModuleType.HypermatterInjector] = ShipModuleRangeRule.NonEnemyTarget,
        [ShipModuleType.ShieldRepairer] = ShipModuleRangeRule.NonEnemyTarget,
        [ShipModuleType.HullRepairer] = ShipModuleRangeRule.NonEnemyTarget,

        // Support pulses around the user's ship never read the selected target.
        [ShipModuleType.RepairFieldGenerator] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.BulwarkShieldGenerator] = ShipModuleRangeRule.NoTarget,
        [ShipModuleType.CapitalEwar] = ShipModuleRangeRule.NoTarget,

        // Weapons keep the firing rule they had: the selected target must be in range.
        [ShipModuleType.TurboLaser] = ShipModuleRangeRule.AnyTarget,
        [ShipModuleType.LaserBattery] = ShipModuleRangeRule.AnyTarget,
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
