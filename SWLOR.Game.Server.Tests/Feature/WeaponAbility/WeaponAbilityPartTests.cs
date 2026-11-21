using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;

namespace SWLOR.Game.Server.Tests.Feature.WeaponAbility;

/// <summary>
/// The parts the weapon ability definitions run through: each part takes at most ten services (its own and the parts it calls), and the
/// parts do not call each other in a circle.
/// </summary>
[TestFixture]
public class WeaponAbilityPartTests
{
    private const int MaximumConstructorServices = 10;

    private static IReadOnlyList<Type> Parts() =>
        typeof(StatusAbilityEffects).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(StatusAbilityEffects).Namespace && type.IsClass && !type.IsAbstract && !type.IsNested)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

    [Test]
    public void EveryPartTakesAtMostTenServices()
    {
        var parts = Parts();
        parts.Should().NotBeEmpty();

        var tooMany = parts
            .Select(part => (part.Name, Count: part.GetConstructors().Max(constructor => constructor.GetParameters().Length)))
            .Where(part => part.Count > MaximumConstructorServices)
            .Select(part => $"{part.Name} takes {part.Count}")
            .ToArray();

        tooMany.Should().BeEmpty();
    }

    [Test]
    public void NoPartCallsAPartThatCallsItBack()
    {
        var parts = Parts();
        var needs = parts.ToDictionary(
            part => part,
            part => part.GetConstructors().Single().GetParameters().Select(parameter => parameter.ParameterType).Where(parts.Contains).ToArray());

        bool Reaches(Type from, Type target, HashSet<Type> seen) =>
            needs[from].Any(next => next == target || seen.Add(next) && Reaches(next, target, seen));

        var cycles = parts.Where(part => Reaches(part, part, new HashSet<Type>())).Select(part => part.Name).ToArray();

        cycles.Should().BeEmpty();
    }
}
