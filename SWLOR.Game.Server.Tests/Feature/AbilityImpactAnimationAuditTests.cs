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

    /// <summary>
    /// Verifies queued weapon impacts return before every scripted animation playback path.
    /// </summary>
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

        var queuedWeaponEarlyReturn = System.Text.RegularExpressions.Regex.Match(
            impactAnimationBody,
            @"if\s*\(\s*trackedAbility\?\.ActivationType\s*==\s*AbilityActivationType\.Weapon\s*\)\s*return\s*;",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        var playAnimationCalls = System.Text.RegularExpressions.Regex.Matches(
                impactAnimationBody,
                @"(?:ActionPlayAnimation|PistolAnimationRemap\.PlayAnimation\w*)\s*\(",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant)
            .Cast<System.Text.RegularExpressions.Match>()
            .ToArray();

        queuedWeaponEarlyReturn.Success.Should().BeTrue(
            "queued weapon impacts must immediately return before scripted animation handling");
        playAnimationCalls.Should().NotBeEmpty();
        playAnimationCalls.Should().OnlyContain(
            call => call.Index >= queuedWeaponEarlyReturn.Index + queuedWeaponEarlyReturn.Length,
            "the unconditional queued-weapon return must dominate every scripted animation call");
    }

    /// <summary>
    /// Verifies configured activation replacements use explicit-throw-preserving playback.
    /// </summary>
    [Test]
    public void ActivationAnimationOverwrite_UsesExplicitThrowPreservingPlayback()
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

        var playIndex = processAnimationBody.IndexOf(
            "PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow",
            StringComparison.Ordinal);

        playIndex.Should().BeGreaterThanOrEqualTo(0);
        processAnimationBody.Should().Contain("sourceAnimationName");
        processAnimationBody.Should().Contain("replacementAnimationName");
        processAnimationBody.Should().Contain("ability.AnimationRestoreDelaySeconds");
    }

    /// <summary>
    /// Verifies configured impact replacements use explicit-throw-preserving playback.
    /// </summary>
    [Test]
    public void ImpactAnimationOverwrite_UsesExplicitThrowPreservingPlayback()
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

        var playIndex = impactAnimationBody.IndexOf(
            "PistolAnimationRemap.PlayAnimationWithTemporaryReplacementPreservingExplicitThrow",
            StringComparison.Ordinal);

        playIndex.Should().BeGreaterThanOrEqualTo(0);
        impactAnimationBody.Should().Contain("sourceAnimationName");
        impactAnimationBody.Should().Contain("replacementAnimationName");
        impactAnimationBody.Should().Contain("restoreDelaySeconds");
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
