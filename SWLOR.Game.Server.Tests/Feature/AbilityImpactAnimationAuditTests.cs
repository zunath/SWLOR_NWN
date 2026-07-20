using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactAnimationAuditTests
{
    [Test]
    public void CastedActionAbilities_DeclareUsageAnimation()
    {
        var root = FindRepositoryRoot();
        var missingAnimations = new List<string>();

        foreach (var (definitionType, abilities) in BuildAbilityDefinitions())
        {
            var source = ReadDefinitionSource(root, definitionType);
            foreach (var (feat, ability) in abilities)
            {
                if (!UsesCastedActionWithoutOwnedAnimation(ability, source))
                    continue;

                missingAnimations.Add($"{definitionType.Name}/{feat} {ability.Name}");
            }
        }

        missingAnimations.Should().BeEmpty(
            "casted abilities with activation or impact work should own their animation path through builder metadata or manual ActionPlayAnimation. Missing: {0}",
            string.Join(", ", missingAnimations));
    }

    [Test]
    public void CombatImpactAnimation_DoesNotUseSharedDefaultAnimation()
    {
        var root = FindRepositoryRoot();
        var abilityServiceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));

        abilityServiceSource.Should().NotContain(
            "Animation.DoubleStrike",
            "shared combat-impact helpers should never choose a default animation for an ability");
    }

    [Test]
    public void QueuedWeaponAbilityImpact_UsesEngineAttackAnimationOnly()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var impactAnimationBody = source.Substring(
            source.IndexOf("private static void PlayCombatImpactAnimation", StringComparison.Ordinal),
            source.IndexOf("public static int ApplyHostileCombatImpact", StringComparison.Ordinal) -
            source.IndexOf("private static void PlayCombatImpactAnimation", StringComparison.Ordinal));

        var queuedWeaponGuardIndex = impactAnimationBody.IndexOf(
            "trackedAbility?.ActivationType == AbilityActivationType.Weapon",
            StringComparison.Ordinal);
        var playAnimationIndex = impactAnimationBody.IndexOf("ActionPlayAnimation(", StringComparison.Ordinal);

        queuedWeaponGuardIndex.Should().BeGreaterThanOrEqualTo(0);
        playAnimationIndex.Should().BeGreaterThan(queuedWeaponGuardIndex,
            "queued weapon abilities must rely on the native attack animation instead of enqueueing a second swing");
    }

    [Test]
    public void ActivationAnimationOverwrite_ReplacesCarrierBeforePlayingAnimation()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var processAnimationBody = source.Substring(
            source.IndexOf("List<string> ProcessAnimationAndVisualEffects", StringComparison.Ordinal),
            source.IndexOf("// Force out of stealth", StringComparison.Ordinal) -
            source.IndexOf("List<string> ProcessAnimationAndVisualEffects", StringComparison.Ordinal));

        var replaceIndex = processAnimationBody.IndexOf(
            "ReplaceObjectAnimation(activator, sourceAnimationName, replacementAnimationName)",
            StringComparison.Ordinal);
        var playIndex = processAnimationBody.IndexOf(
            "ActionPlayAnimation(ability.AnimationType, 1.0f, animationLength)",
            StringComparison.Ordinal);
        var restoreIndex = processAnimationBody.IndexOf(
            "ReplaceObjectAnimation(activator, sourceAnimationName);",
            StringComparison.Ordinal);

        replaceIndex.Should().BeGreaterThanOrEqualTo(0);
        playIndex.Should().BeGreaterThanOrEqualTo(0);
        restoreIndex.Should().BeGreaterThanOrEqualTo(0);
        replaceIndex.Should().BeLessThan(playIndex);
        playIndex.Should().BeLessThan(restoreIndex);
    }

    [Test]
    public void ImpactAnimationOverwrite_ReplacesCarrierBeforePlayingAnimation()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs")).Replace("\r\n", "\n");
        var impactAnimationBody = source.Substring(
            source.IndexOf("private static void PlayCombatImpactAnimation", StringComparison.Ordinal),
            source.IndexOf("public static int ApplyHostileCombatImpact", StringComparison.Ordinal) -
            source.IndexOf("private static void PlayCombatImpactAnimation", StringComparison.Ordinal));

        var replaceIndex = impactAnimationBody.IndexOf(
            "ReplaceObjectAnimation(\n                        activator,\n                        sourceAnimationName,\n                        replacementAnimationName)",
            StringComparison.Ordinal);
        var playIndex = impactAnimationBody.IndexOf(
            "ActionPlayAnimation(animation, 1.0f, restoreDelaySeconds)",
            StringComparison.Ordinal);
        var restoreIndex = impactAnimationBody.IndexOf(
            "ReplaceObjectAnimation(activator, sourceAnimationName);",
            StringComparison.Ordinal);

        replaceIndex.Should().BeGreaterThanOrEqualTo(0);
        playIndex.Should().BeGreaterThanOrEqualTo(0);
        restoreIndex.Should().BeGreaterThanOrEqualTo(0);
        replaceIndex.Should().BeLessThan(playIndex);
        playIndex.Should().BeLessThan(restoreIndex);
    }

    private static bool UsesCastedActionWithoutOwnedAnimation(AbilityDetail ability, string source)
    {
        return ability.ActivationType == AbilityActivationType.Casted &&
               (ability.ActivationAction != null || ability.ImpactAction != null) &&
               ability.AnimationType == Animation.Invalid &&
               ability.ImpactAnimationType == Animation.Invalid &&
               !UsesManualAnimation(source);
    }

    private static bool UsesManualAnimation(string source)
    {
        return source.Contains("ActionPlayAnimation(", StringComparison.Ordinal);
    }

    private static IEnumerable<(Type DefinitionType, Dictionary<FeatType, AbilityDetail> Abilities)> BuildAbilityDefinitions()
    {
        var definitionTypes = typeof(ThrowRockAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            yield return (definitionType, definition.BuildAbilities());
        }
    }

    private static string ReadDefinitionSource(DirectoryInfo root, Type definitionType)
    {
        var relativeNamespace = definitionType.Namespace!
            .Replace("SWLOR.Game.Server.Feature.AbilityDefinition", string.Empty)
            .Trim('.')
            .Replace('.', Path.DirectorySeparatorChar);
        var path = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "AbilityDefinition",
            relativeNamespace,
            $"{definitionType.Name}.cs");

        File.Exists(path).Should().BeTrue($"{definitionType.FullName} should live in a matching ability definition file");
        return File.ReadAllText(path);
    }

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

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
