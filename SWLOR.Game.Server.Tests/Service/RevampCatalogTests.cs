using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The generated space revamp catalog (tools/SpaceRevampBalance/catalog.py) and the definitions built from it.
/// </summary>
public class RevampCatalogTests
{
    private static readonly int[] StandardRanks = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 };
    private static readonly int[] CapitalRanks = { 40, 45 };

    [Test]
    public void ModuleSteps_HaveUniqueShortTagsAndFitTheHotbar()
    {
        var steps = RevampModuleCatalog.Steps;

        steps.Should().HaveCount(345, "278 + the Salvager and Gas Harvester ranks (W6a) + the Smuggling Compartment ranks (W7b) + " +
            "the Probe Launcher ranks (W12a) + 16 named rank-50 modules (W9b) + the Data Analyzer (W12b) + the Cloaking Device ranks");
        steps.Select(x => x.Tag).Should().OnlyHaveUniqueItems();
        steps.Select(x => x.Name).Should().OnlyHaveUniqueItems();
        foreach (var step in steps)
        {
            step.Tag.Length.Should().BeLessThanOrEqualTo(16, step.Tag);
            step.Texture.Length.Should().BeLessThanOrEqualTo(16, step.Tag);
            step.Description.Should().NotBeNullOrWhiteSpace(step.Tag);
            if (step.Activation != ShipModuleActivation.Invalid)
                step.ShortName.Length.Should().BeLessThanOrEqualTo(14, $"{step.Tag} is an active module shown on the hotbar");
        }
    }

    [Test]
    public void ModuleFamilies_HaveTheirRankLaddersAndShareRowSkillAndUse()
    {
        foreach (var family in RevampModuleCatalog.Steps.GroupBy(x => x.Family))
        {
            var first = family.First();
            var ladder = family.Key == ShipModuleFamily.DataAnalyzer ? new[] { 0 } : first.Capital ? CapitalRanks : StandardRanks;
            if (family.Any(x => x.Named))
                ladder = ladder.Append(50).ToArray();
            family.Select(x => x.Rank).Should().Equal(ladder, family.Key.ToString());
            family.Where(x => x.Named).Should().OnlyContain(x => x.Rank == 50, family.Key.ToString());
            family.Select(x => x.Row).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.Skill).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.Activation).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.Capital).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.Texture).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.Range).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.CapacitorCost).Distinct().Should().ContainSingle(family.Key.ToString());
            family.Select(x => x.CycleSeconds).Distinct().Should().ContainSingle(family.Key.ToString());
            first.Row.Should().BeOneOf(ShipModulePowerType.High, ShipModulePowerType.Mid, ShipModulePowerType.Low);
            first.Skill.Should().BeOneOf(SkillType.Piloting, SkillType.Gathering, SkillType.Espionage);
            if (first.Activation != ShipModuleActivation.Invalid)
                first.CycleSeconds.Should().BeGreaterThan(0, family.Key.ToString());
        }

        RevampModuleCatalog.Steps.Select(x => x.Family).Distinct().Should().HaveCount(45);
        Enum.GetValues<ShipModuleFamily>().Where(x => x != ShipModuleFamily.Invalid)
            .Should().BeEquivalentTo(RevampModuleCatalog.Steps.Select(x => x.Family).Distinct(), "every family has steps");
    }

    [Test]
    public void WeaponSteps_CarryEverythingTheShotNeeds()
    {
        foreach (var step in RevampModuleCatalog.Steps.Where(x => x.Dmg > 0))
        {
            step.Row.Should().Be(ShipModulePowerType.High, step.Tag);
            step.Dmg.Should().BePositive(step.Tag);
            step.DamageType.Should().NotBe(CombatDamageType.Invalid, step.Tag);
            step.Attack.Should().Be(step.Accuracy, step.Tag);
            step.ShieldDamageMultiplier.Should().BeGreaterThan(0, step.Tag);
            if (step.Capital)
                step.FixedSize.Should().Be(ShipSize.Capital, step.Tag);
        }
    }

    [Test]
    public void ModuleSteps_HaveABlueprintWithTheSameTagAndName()
    {
        foreach (var step in RevampModuleCatalog.Steps)
        {
            using var blueprint = ReadBlueprint(step.Tag);
            blueprint.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be(step.Tag);
            blueprint.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(step.Tag);
            blueprint.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be(step.Name);
        }
    }

    [Test]
    public void Hulls_HaveABlueprintWithTheSameTagAndNameAndTheSlotsTheWindowCanShow()
    {
        var hulls = RevampShipCatalog.Hulls;

        hulls.Should().HaveCount(50);
        hulls.Select(x => x.Tag).Should().OnlyHaveUniqueItems();
        hulls.Select(x => x.Name).Should().OnlyHaveUniqueItems();
        foreach (var hull in hulls)
        {
            hull.Tag.Length.Should().BeLessThanOrEqualTo(16, hull.Tag);
            hull.High.Should().BeInRange(1, 8, hull.Tag);
            hull.Mid.Should().BeInRange(1, 8, hull.Tag);
            hull.Low.Should().BeInRange(1, 8, hull.Tag);
            hull.StationHigh.Should().BeInRange(0, hull.High, hull.Tag);
            hull.StationMid.Should().BeInRange(0, hull.Mid, hull.Tag);
            hull.StationLow.Should().BeInRange(0, hull.Low, hull.Tag);
            hull.Speed.Should().BeGreaterThan(0, hull.Tag);
            hull.Size.Should().BeOneOf(ShipSize.Small, ShipSize.Medium, ShipSize.Large, ShipSize.Capital);

            using var blueprint = ReadBlueprint(hull.Tag);
            blueprint.RootElement.GetProperty("Tag").GetProperty("value").GetString().Should().Be(hull.Tag);
            blueprint.RootElement.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be(hull.Tag);
            blueprint.RootElement.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString()
                .Should().EndWith(hull.Name);
        }

        hulls.Count(x => x.Size == ShipSize.Capital).Should().Be(11);
        hulls.Where(x => x.Size != ShipSize.Capital).Should().OnlyContain(x => x.StationHigh + x.StationMid + x.StationLow == 0 || x.Size == ShipSize.Large);
    }

    [Test]
    public void RecipeTypes_AreNamedAfterTheirItemAndAreUnique()
    {
        var steps = RevampModuleCatalog.Steps;
        var hulls = RevampShipCatalog.Hulls;

        foreach (var step in steps)
        {
            Enum.IsDefined(step.RecipeType).Should().BeTrue(step.Tag);
            step.RecipeType.ToString().Should().Be($"Ship{step.Family}{step.Rank:00}");
            step.RecipeLevel.Should().BeInRange(1, step.Named ? 52 : 50, step.Tag);
            step.Components.Should().NotBeEmpty(step.Tag);
        }

        foreach (var hull in hulls)
        {
            Enum.IsDefined(hull.RecipeType).Should().BeTrue(hull.Tag);
            hull.RecipeType.ToString().Should().StartWith("ShipDeed");
            hull.RecipeLevel.Should().BeInRange(1, 50, hull.Tag);
            hull.Components.Should().NotBeEmpty(hull.Tag);
        }

        var all = steps.Select(x => x.RecipeType).Concat(hulls.Select(x => x.RecipeType)).ToList();
        all.Should().OnlyHaveUniqueItems();
        all.Select(x => x.ToString()).Should().OnlyHaveUniqueItems();
        // The modules and hulls take 7000-7357; the Probe Launcher (W12a) follows the component recipes (7358-7487); the
        // named modules (W9b) open the next block (7498-7531, with the rare components and ammo); the Data Analyzer (W12b)
        // follows it, then the Cloaking Device ranks (7533-7542).
        all.Where(x => !steps.Any(s => s.Named && s.RecipeType == x) && x != RecipeType.ShipDataAnalyzer00 &&
                       steps.All(s => s.Family != ShipModuleFamily.CloakingDevice || s.RecipeType != x))
            .Should().OnlyContain(x => ((int)x >= 7000 && (int)x < 7000 + 358) || ((int)x >= 7488 && (int)x < 7498));
        steps.Where(x => x.Named).Select(x => (int)x.RecipeType).Should().OnlyContain(x => x >= 7498 && x < 7498 + 16);
        ((int)RecipeType.ShipDataAnalyzer00).Should().Be(7532);
        steps.Where(x => x.Family == ShipModuleFamily.CloakingDevice).Select(x => (int)x.RecipeType).Should().Equal(Enumerable.Range(7533, 10));
    }

    [Test]
    public void RevampDefinitions_BuildEveryStepAndHullWithoutPerks()
    {
        var modules = new RevampModuleDefinition().BuildShipModules();
        var ships = new RevampPlayerShipDefinition().BuildShips();

        modules.Keys.Should().BeEquivalentTo(RevampModuleCatalog.Steps.Select(x => x.Tag));
        ships.Keys.Should().BeEquivalentTo(RevampShipCatalog.Hulls.Select(x => x.Tag));

        foreach (var step in RevampModuleCatalog.Steps)
        {
            var module = modules[step.Tag];
            module.Step.Should().BeSameAs(step);
            module.RequiredPerks.Should().BeEmpty(step.Tag);
            module.PowerType.Should().Be(step.Row, step.Tag);
            module.CapitalClassModule.Should().Be(step.Capital, step.Tag);
            module.Activation.Should().Be(step.Activation, step.Tag);
            module.Name.Should().Be(step.Name);
            module.Texture.Should().Be(step.Texture);

            if (step.Activation == ShipModuleActivation.Invalid)
            {
                module.Type.Should().Be(ShipModuleType.Passive, step.Tag);
                module.ModuleActivatedAction.Should().BeNull(step.Tag);
                module.CalculateRecastAction.Should().BeNull(step.Tag);
                continue;
            }

            module.Type.Should().NotBe(ShipModuleType.Passive, step.Tag);
            module.ModuleActivatedAction.Should().NotBeNull(step.Tag);
            module.ModuleMaxDistanceAction.Should().NotBeNull(step.Tag);
            module.CalculateCapacitorAction!(0, new ShipStatus(), 0).Should().Be(step.CapacitorCost, step.Tag);
            module.CalculateRecastAction!(0, new ShipStatus(), 0).Should().BeApproximately((float)step.CycleSeconds, 0.001f, step.Tag);
            if (step.Dmg > 0 || step.MiningTier > 0 || step.Family is ShipModuleFamily.ShieldRepairer or ShipModuleFamily.HullRepairer)
                module.ModuleMaxDistanceAction!(0, new ShipStatus(), 0, new ShipStatus(), 0).Should().Be(step.Range, step.Tag);
        }

        foreach (var hull in RevampShipCatalog.Hulls)
        {
            var ship = ships[hull.Tag];
            ship.Hull.Should().BeSameAs(hull);
            ship.RequiredPerks.Should().BeEmpty(hull.Tag);
            ship.ItemResref.Should().Be(hull.Tag);
            ship.Name.Should().Be(hull.Name);
            ship.Appearance.Should().Be(hull.Appearance);
            ship.HighPowerNodes.Should().Be(hull.High);
            ship.MidPowerNodes.Should().Be(hull.Mid);
            ship.LowPowerNodes.Should().Be(hull.Low);
            ship.ConfigurationNodes.Should().Be(0);
            ship.Size.Should().Be(hull.Size);
            ship.Role.Should().Be(hull.Role);
            ship.Manufacturer.Should().Be(hull.Manufacturer);
            ship.Layout.Should().Be(hull.Layout);
            ship.CapitalShip.Should().Be(hull.Size == ShipSize.Capital);
        }
    }

    [Test]
    public void AllDefinitions_RegisterWithoutDuplicateKeysAlongsideTheLegacyOnes()
    {
        var assembly = typeof(IShipModuleListDefinition).Assembly;
        var moduleTags = new HashSet<string>();
        var shipTags = new HashSet<string>();
        var shipResrefs = new Dictionary<string, string>();

        foreach (var type in assembly.GetTypes().Where(x => !x.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(x)))
        {
            foreach (var tag in ((IShipModuleListDefinition)Activator.CreateInstance(type)!).BuildShipModules().Keys)
                moduleTags.Add(tag).Should().BeTrue($"{type.Name} registers module {tag} a second time");
        }

        foreach (var type in assembly.GetTypes().Where(x => !x.IsAbstract && typeof(IShipListDefinition).IsAssignableFrom(x)))
        {
            foreach (var (tag, ship) in ((IShipListDefinition)Activator.CreateInstance(type)!).BuildShips())
            {
                shipTags.Add(tag).Should().BeTrue($"{type.Name} registers ship {tag} a second time");
                if (ship.ItemResref != null)
                    shipResrefs.TryAdd(ship.ItemResref, tag);
            }
        }

        moduleTags.Should().Contain(RevampModuleCatalog.Steps.Select(x => x.Tag)).And.Contain("com_laser_1");
        shipTags.Should().Contain(RevampShipCatalog.Hulls.Select(x => x.Tag)).And.Contain("sdeed_striker");
        foreach (var hull in RevampShipCatalog.Hulls)
            shipResrefs[hull.Tag].Should().Be(hull.Tag, "a revamp deed resref is not shared with another ship");
    }

    [Test]
    public void LegacyModulesAndShips_StayLegacy()
    {
        var assembly = typeof(IShipModuleListDefinition).Assembly;
        var legacyModules = assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(x) && x != typeof(RevampModuleDefinition))
            .SelectMany(x => ((IShipModuleListDefinition)Activator.CreateInstance(x)!).BuildShipModules().Values);
        var legacyShips = assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(IShipListDefinition).IsAssignableFrom(x) && x != typeof(RevampPlayerShipDefinition))
            .SelectMany(x => ((IShipListDefinition)Activator.CreateInstance(x)!).BuildShips().Values);

        legacyModules.Should().OnlyContain(x => x.Step == null);
        legacyShips.Should().OnlyContain(x => x.Hull == null);
    }

    [Test]
    public void FitRules_KeepLegacyAndRevampApartAndCapitalModulesOnCapitalHulls()
    {
        var revampSmall = new ShipDetail { Hull = new ShipHullEntry { Size = ShipSize.Small } };
        var revampCapital = new ShipDetail { Hull = new ShipHullEntry { Size = ShipSize.Capital }, CapitalShip = true };
        var legacySmall = new ShipDetail();
        var legacyCapital = new ShipDetail { CapitalShip = true };

        var revampModule = new ShipModuleDetail { Step = new ShipModuleStep() };
        var revampCapitalModule = new ShipModuleDetail { Step = new ShipModuleStep(), CapitalClassModule = true };
        var legacyModule = new ShipModuleDetail();
        var legacyCapitalModule = new ShipModuleDetail { CapitalClassModule = true };

        ShipFitRules.GetFitError(revampSmall, revampModule).Should().BeEmpty();
        ShipFitRules.GetFitError(revampCapital, revampModule).Should().BeEmpty("a revamp capital hull also takes ordinary modules");
        ShipFitRules.GetFitError(revampCapital, revampCapitalModule).Should().BeEmpty();
        ShipFitRules.GetFitError(revampSmall, revampCapitalModule).Should().Be(ShipFitRules.CapitalOnlyMessage);

        ShipFitRules.GetFitError(revampSmall, legacyModule).Should().Be(ShipFitRules.WrongGenerationMessage);
        ShipFitRules.GetFitError(revampCapital, legacyCapitalModule).Should().Be(ShipFitRules.WrongGenerationMessage);
        ShipFitRules.GetFitError(legacySmall, revampModule).Should().Be(ShipFitRules.WrongGenerationMessage);
        ShipFitRules.GetFitError(legacyCapital, revampCapitalModule).Should().Be(ShipFitRules.WrongGenerationMessage);

        ShipFitRules.GetFitError(legacySmall, legacyModule).Should().BeEmpty();
        ShipFitRules.GetFitError(legacySmall, legacyCapitalModule).Should().Be(ShipFitRules.CapitalOnlyMessage);
        ShipFitRules.GetFitError(legacyCapital, legacyCapitalModule).Should().BeEmpty();
        ShipFitRules.GetFitError(legacyCapital, legacyModule).Should().Be(ShipFitRules.CapitalShipMessage);
    }

    private static JsonDocument ReadBlueprint(string tag)
    {
        var path = Path.Combine(Root(), "Module", "uti", tag + ".uti.json");
        File.Exists(path).Should().BeTrue(path);
        return JsonDocument.Parse(File.ReadAllText(path).TrimStart('﻿'));
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository.");
    }
}
