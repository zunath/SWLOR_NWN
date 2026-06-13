using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityImpactAnimationAuditTests
{
    [Test]
    public void CastedImpactAbilities_DoNotFallBackToDefaultWeaponAnimation()
    {
        var root = FindRepositoryRoot();
        var abilityServiceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Ability.cs"));
        var castedImpactAbilitiesWithoutAnimation = new List<string>();

        foreach (var (definitionType, abilities) in BuildAbilityDefinitions())
        {
            foreach (var (feat, ability) in abilities)
            {
                if (!UsesCastedCombatImpactWithoutExplicitAnimation(ability))
                    continue;

                var source = ReadDefinitionSource(root, definitionType);
                if (UsesCombatImpact(source))
                    castedImpactAbilitiesWithoutAnimation.Add($"{definitionType.Name}/{feat} {ability.Name}");
            }
        }

        castedImpactAbilitiesWithoutAnimation.Should().Contain(
            entry => entry.Contains("ThrowRockAbilityDefinition/ThrowRock1", StringComparison.Ordinal),
            "Throw Rock is the regression case this audit protects");
        abilityServiceSource.Should().Contain(
            "trackedAbility?.ActivationType != AbilityActivationType.Weapon",
            "casted combat-impact abilities must not inherit the legacy weapon swing fallback");
    }

    private static bool UsesCastedCombatImpactWithoutExplicitAnimation(AbilityDetail ability)
    {
        return ability.IsHostileAbility &&
               ability.ImpactAction != null &&
               ability.ActivationType == AbilityActivationType.Casted &&
               ability.AnimationType == Animation.Invalid &&
               ability.ImpactAnimationType == Animation.Invalid;
    }

    private static bool UsesCombatImpact(string source)
    {
        return source.Contains("Ability.ApplyCombatImpact", StringComparison.Ordinal) ||
               source.Contains("Ability.ApplyTelegraphedCombatImpact", StringComparison.Ordinal);
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
