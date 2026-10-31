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

    [Test]
    public void SupportToggle_ResolvesItsLockTargetBeforeTheTargetChecks()
    {
        // A repairer switched on with an enemy out of range or an asteroid selected locks onto the pilot's own ship, so
        // the type, range and module checks must validate that ship, not the selection.
        var space = ReadServerSource("Service", "Space.cs");
        var activation = space.Substring(space.IndexOf("// A support toggle locks onto the friendly ship selected", StringComparison.Ordinal));
        var resolve = activation.IndexOf("target = ShipToggleRules.ChooseLockTarget(usable, target, activator);", StringComparison.Ordinal);
        var selfStatus = activation.IndexOf("if (target == activator)", StringComparison.Ordinal);
        var typeCheck = activation.IndexOf("This module cannot be used on that target type.", StringComparison.Ordinal);
        var rangeCheck = activation.IndexOf("Target is too far away.", StringComparison.Ordinal);
        var validation = activation.IndexOf("shipModuleDetails.ModuleValidationAction(activator", StringComparison.Ordinal);

        resolve.Should().BeGreaterThan(0);
        selfStatus.Should().BeGreaterThan(resolve, "the ship's own status replaces the selection's once the lock falls back to it");
        typeCheck.Should().BeGreaterThan(selfStatus);
        rangeCheck.Should().BeGreaterThan(typeCheck);
        validation.Should().BeGreaterThan(rangeCheck);
        activation.Should().Contain("lockedTarget = target;", "the toggle keeps the target the checks validated");
    }

    [Test]
    public void LauncherAbility_FiresTheAmmoVariantItSpends()
    {
        // An ability fired from a launcher with variant ammo loaded spends that variant, so its shot takes the
        // variant's modifiers and landed-hit rider, as the launcher's own cycle does (RevampModuleDefinition.AmmoWeapon).
        var source = ReadServerSource("Service", "SpaceAbilities.cs");
        var fire = source.Substring(source.IndexOf("private static SpaceFireResult FireFrom(", StringComparison.Ordinal));
        var variant = fire.IndexOf("var variant = ShipAmmoRules.Variant(ammo);", StringComparison.Ordinal);
        var apply = fire.IndexOf("var shot = ShipAmmoRules.Apply(ShipShotBuilder.BuildFor(step, status, targetStatus), variant);", StringComparison.Ordinal);
        var bonus = fire.IndexOf("shot.Dmg += bonusDmg;", StringComparison.Ordinal);
        var consume = fire.IndexOf("ShipModuleActions.ConsumeAmmo(activator, ammo);", StringComparison.Ordinal);
        var rider = fire.IndexOf("ShipModuleActions.AmmoRider(activator, target, shot, variant)", StringComparison.Ordinal);

        variant.Should().BeGreaterThan(0);
        apply.Should().BeGreaterThan(variant);
        bonus.Should().BeGreaterThan(apply, "the variant scales the weapon's DMG, and the ability adds its bonus after");
        consume.Should().BeGreaterThan(bonus, "the ammo spent is the ammo the shot was built from");
        rider.Should().BeGreaterThan(consume, "the variant's rider follows the hit it fired");

        // A volley picks its ammo once, before its first shot, so every ship it reaches takes the shot of the ammo spent.
        var area = source.Substring(source.IndexOf("public static List<SpaceFireResult> FireWeaponArea(", StringComparison.Ordinal));
        var pick = area.IndexOf("var ammo = LauncherAmmo(activator, weapon);", StringComparison.Ordinal);
        var loop = area.IndexOf("foreach (var ship in targets)", StringComparison.Ordinal);
        pick.Should().BeGreaterThan(0);
        loop.Should().BeGreaterThan(pick);
        area.Should().Contain("FireFrom(activator, status, weapon, ship, shipStatus, bonusDmg, options, ammo, consumeAmmo: !spentAmmo)");
        source.Should().Contain("FireFrom(activator, status, weapon, target, targetStatus, bonusDmg, options, LauncherAmmo(activator, weapon), consumeAmmo: true)");
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
