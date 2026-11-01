using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Ground statuses on ships: what the space code relies on from the shared status definitions, and that no
/// damage-over-time status bypasses the ship's pools.
/// </summary>
public class SpaceStatusRoutingTests
{
    /// <summary>Statuses that deal damage to someone else by design, not damage over time on their holder.</summary>
    private static readonly string[] NotDamageOverTime = { "SoulDevourerStanceStatusEffect.cs" };

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The repository root was not found.");
    }

    private static string ReadServerSource(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot().FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()))
            .Replace("\r\n", "\n");

    /// <summary>
    /// A tick that applied an EffectDamage to its holder would hurt the pilot's creature instead of the ship.
    /// Every damage-over-time status deals its tick through StatusEffect.ApplyDamageOverTime, which routes a ship's
    /// tick to its shield and hull.
    /// </summary>
    [Test]
    public void NoStatusDealsItsTickAsHitPointDamageDirectly()
    {
        var directory = Path.Combine(FindRepositoryRoot().FullName, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition");

        var offenders = Directory.GetFiles(directory, "*.cs")
            .Where(file => !NotDamageOverTime.Contains(Path.GetFileName(file)))
            .Where(file => File.ReadAllText(file).Contains("EffectDamage("))
            .Select(Path.GetFileName)
            .ToList();

        offenders.Should().BeEmpty("damage-over-time ticks go through StatusEffect.ApplyDamageOverTime");
    }

    [Test]
    public void DamageOverTimeStatuses_UseTheSharedTickHelper()
    {
        foreach (var file in new[]
                 {
                     "BurnStatusEffect.cs", "FragmentationStatusEffect.cs", "BleedStatusEffect.cs", "PoisonStatusEffect.cs",
                     "ShockStatusEffect.cs", "FreezingStatusEffect.cs", "ToxinStatusEffect.cs", "VenomStatusEffect.cs",
                     "InfectionStatusEffect.cs", "DiseaseStatusEffect.cs", "SaturationTossStatusEffect.cs",
                     "ForceDamageOverTimeStatusEffectBase.cs",
                 })
        {
            ReadServerSource("Feature", "StatusEffectDefinition", file)
                .Should().Contain("StatusEffect.ApplyDamageOverTime(", file);
        }
    }

    [Test]
    public void ApplyDamageOverTime_SendsAShipsTickToItsPoolsAndEverythingElseToHitPoints()
    {
        var source = ReadServerSource("Service", "StatusEffect.cs");
        var start = source.IndexOf("public static void ApplyDamageOverTime(", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0);
        var body = source.Substring(start, 500);

        body.IndexOf("Space.TryApplyStatusDamage(", StringComparison.Ordinal).Should().BeGreaterThan(0);
        body.Should().Contain("AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(amount, damageType), creature))",
            "a creature that is not a ship still takes the tick under the source's name");
        body.IndexOf("EffectDamage(", StringComparison.Ordinal)
            .Should().BeGreaterThan(body.IndexOf("Space.TryApplyStatusDamage(", StringComparison.Ordinal));
    }

    /// <summary>Dazed is a hard crowd-control status that incapacitates, which is what stops a ship's toggles and abilities.</summary>
    [Test]
    public void Dazed_IsIncapacitatingHardCrowdControl()
    {
        var categories = new DazedStatusEffect().Categories;

        categories.Should().HaveFlag(StatusEffectCategory.Incapacitating);
        categories.Should().HaveFlag(StatusEffectCategory.HardCrowdControl);
    }

    /// <summary>The stat statuses reach the space formulas through the ground's percent stats.</summary>
    [Test]
    public void StatStatuses_CarryTheStatsTheSpaceFormulasRead()
    {
        new WeakenedStatusEffect(15).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-15);
        new DisarmingShotStatusEffect(10).StatGroup.Stats[StatType.AttackPercentAdjustment].Should().Be(-10);
        new ExposedStatusEffect(-15).StatGroup.Stats[StatType.DefensePercentAdjustment].Should().Be(-15);
    }

    [Test]
    public void SpaceFormulas_ReadTheGroundCombatStats()
    {
        var space = ReadServerSource("Service", "Space.cs");

        foreach (var stat in new[]
                 {
                     "StatType.Accuracy", "StatType.AccuracyPercentAdjustment",
                     "StatType.Evasion", "StatType.EvasionPercentAdjustment", "StatType.RangedEvasionPercentAdjustment",
                     "StatType.Attack", "StatType.AttackPercentAdjustment",
                     "StatType.Defense", "StatType.DefensePercentAdjustment",
                     "StatType.CriticalRatePercentAdjustment", "StatType.CriticalDamagePercentAdjustment",
                     "StatType.DamageDealtPercentAdjustment", "StatType.DamageTakenPercentAdjustment",
                     "StatType.SingleTargetDamagePercentAdjustment", "StatType.AreaAbilityDamagePercentAdjustment",
                     "StatType.LeadershipOtherDamageTakenPercentAdjustment", "StatType.DamageEnmityPercentAdjustment",
                 })
        {
            space.Should().Contain(stat);
        }
    }

    [Test]
    public void ABlindedAttackerCanMissAndAnEvadedShotRaisesTheGroundAvoidedAttackHook()
    {
        var space = ReadServerSource("Service", "Space.cs");

        space.Should().Contain("IsBlinded(activator)");
        space.Should().Contain("Combat.TrackAvoidedAttack(target, activator)");
        space.Should().Contain("Combat.ApplyCriticalHitEffects(");
        space.Should().Contain("Enmity.CalculateDamageEnmity(");
    }

    [Test]
    public void Toggles_ModulesAndAbilities_RefuseToActWhileIncapacitated()
    {
        ReadServerSource("Service", "SpaceToggles.cs").Should().Contain("IsShipIncapacitated(creature)");
        ReadServerSource("Service", "Space.cs").Should().Contain("IsShipIncapacitated(activator)");
        ReadServerSource("Service", "Ability.cs").Should().Contain("Space.IsShipIncapacitated(activator)");
    }

    [Test]
    public void AbilityValidation_TreatsASpaceAbilityAsAShipAbility()
    {
        var source = ReadServerSource("Service", "Ability.cs");
        var start = source.IndexOf("public static bool CanUseAbility(", StringComparison.Ordinal);
        var body = source.Substring(start, source.IndexOf("private static bool HasAbilityLineOfSight", start, StringComparison.Ordinal) - start);

        body.Should().Contain("if (!inSpace && ability.CanBeUsedInSpace && !Space.IsShipInSpace(activator))",
            "a space ability needs a ship");
        body.Should().Contain("if (!inSpace &&\n                ability.ActivationType == AbilityActivationType.Weapon",
            "no equipped weapon is needed in space");
        body.Should().Contain("var maxRange = GetAbilityMaxRange(activator, ability, inSpace);");
        body.Should().Contain("var areaLineOfSightError = inSpace", "space has no line-of-sight check");
    }

    [Test]
    public void SpaceAbilities_HaveNoActivationDelayAndDoNotStopTheShip()
    {
        var source = ReadServerSource("Feature", "UsePerkFeat.cs");

        source.Should().Contain("var isSpaceAbility = ability.CanBeUsedInSpace && Space.IsShipInSpace(activator);");
        source.Should().Contain("if (isSpaceAbility)\n                    return 0f;");
        source.Should().Contain("if (!isSpaceAbility)\n                {\n                    AssignCommand(activator, () =>");
    }
}
