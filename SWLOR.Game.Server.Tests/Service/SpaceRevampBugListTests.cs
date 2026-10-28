using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Plan section 16's bug list items fixed by the gaps pass (W11d), and the section 19 check that Piloting rank adds no
/// combat stat. The other items are covered where their fixes live (see the plan's section 16 status).
/// </summary>
public class SpaceRevampBugListTests
{
    [Test]
    public void Bug6_TheUnusedCapitalPowerDiverterModuleTypeIsGone_AndStormCannonsHaveTheirOwnType()
    {
        Enum.GetNames<ShipModuleType>().Should().NotContain("CapitalPowerDiverter");
        RevampModuleCatalog.Steps.Should().Contain(x => x.Family == ShipModuleFamily.StormCannon);
        ReadServerSource("Feature", "ShipModuleDefinition", "RevampModuleDefinition.cs")
            .Should().Contain("ShipModuleFamily.StormCannon => ShipModuleType.StormCannon,");
    }

    [Test]
    public void Bug10_TheKillXpRangeCoversTheLongestSpaceWeapon()
    {
        var longest = RevampModuleCatalog.Steps.Where(x => x.Row == ShipModulePowerType.High).Max(x => x.Range);
        ShipKillXpRules.GetRange(true).Should().BeGreaterThanOrEqualTo(longest);
        ShipKillXpRules.GetRange(false).Should().Be(40f, "the ground range is unchanged");
        ReadServerSource("Service", "CombatPoint.cs")
            .Should().Contain("GetDistanceBetween(player, npc) > ShipKillXpRules.GetRange(Space.IsPlayerInSpaceMode(player))")
            .And.NotContain("GetDistanceBetween(player, npc) > 40.0f");
    }

    [Test]
    public void Bug13_Subtype58_IsLabelledAsTheAgilityBonusItBuilds()
    {
        ReadServerSource("Service", "CraftService", "EnhancementSubType.cs")
            .Should().Contain("[EnhancementSubType(\"Agility\")]\n        Agility = 58,")
            .And.NotContain("Starship - Agility");
    }

    [Test]
    public void Bug15_The35MissileRecipeIsLevel35()
    {
        var source = ReadServerSource("Feature", "RecipeDefinition", "EngineeringRecipeDefinition", "StarshipAmmoRecipes.cs");
        var start = source.IndexOf("RecipeType.Missile35", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0);
        source.Substring(start, source.IndexOf(';', start) - start).Should().Contain(".Level(35)").And.Contain(".Quantity(35)");
    }

    [Test]
    public void Bug16_ShipDetailHasNoUnreadIndustryBonus()
    {
        typeof(ShipDetail).GetProperty("IndustryBonus").Should().BeNull();
    }

    [Test]
    public void Bug18_ASpaceObjectWithoutARegisteredShip_IsSkippedInsteadOfThrowing()
    {
        ReadServerSource("Service", "Space.cs")
            .Should().Contain("if (!_shipTypes.TryGetValue(registeredEnemyType.ShipItemTag ?? string.Empty, out var shipDetail))")
            .And.NotContain("var shipDetail = _shipTypes[registeredEnemyType.ShipItemTag];");
    }

    [Test]
    public void Bug20_BuildersGuideShipExamplesUseRealMembers()
    {
        var guide = ReadServerSource("Readmes", "Builders.md");
        guide.Should().NotContain(".RequirePerk(PerkType.Pilot").And.NotContain("ShipModuleType.Weapon")
            .And.NotContain("AppearanceType.XWing").And.NotContain(".ItemTag(\"asteroid_ship\")");
        foreach (var member in new[] { "MaxArmor", "HighPowerNodes", "InteriorLayout" })
            typeof(ShipBuilder).GetMethod(member).Should().NotBeNull(member);
        foreach (var member in new[] { "Texture", "PowerType", "Toggle", "MaxDistance", "Step" })
            typeof(ShipModuleBuilder).GetMethods().Should().Contain(x => x.Name == member, member);
    }

    [Test]
    public void PilotingRank_ChangesNoCombatStat()
    {
        // Plan section 9: hull and gear carry the stats; Piloting rank only gates hulls and modules.
        foreach (var path in new[] { new[] { "Service", "SpaceService", "ShipStatCalculator.cs" },
                     new[] { "Service", "SpaceService", "SpaceCombatMath.cs" }, new[] { "Service", "Space.cs" } })
        {
            var source = ReadServerSource(path);
            source.Should().NotContain("Skills[SkillType.Piloting]", string.Join("/", path));
            source.Should().NotContain("GetSkillRank(SkillType.Piloting", string.Join("/", path));
        }
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
