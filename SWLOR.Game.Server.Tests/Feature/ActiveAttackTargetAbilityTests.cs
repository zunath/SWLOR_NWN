using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class ActiveAttackTargetAbilityTests
{
    [Test]
    public void ActiveAttackTargetAbilities_ResolveCurrentAttackTargetBeforeValidation()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "UsePerkFeat.cs")).Replace("\r\n", "\n");
        var tryUseBody = source.Substring(
            source.IndexOf("public static bool TryUseAbility", StringComparison.Ordinal),
            source.IndexOf("/// <summary>\n        /// Applies effects to the activator", StringComparison.Ordinal) -
            source.IndexOf("public static bool TryUseAbility", StringComparison.Ordinal));
        var resolverBody = source.Substring(
            source.IndexOf("private static (uint Target, Location TargetLocation) ResolveAbilityTarget", StringComparison.Ordinal),
            source.IndexOf("private static void ResumeAttack", StringComparison.Ordinal) -
            source.IndexOf("private static (uint Target, Location TargetLocation) ResolveAbilityTarget", StringComparison.Ordinal));

        var resolveIndex = tryUseBody.IndexOf("ResolveAbilityTarget(", StringComparison.Ordinal);
        var canUseIndex = tryUseBody.IndexOf("Ability.CanUseAbility", StringComparison.Ordinal);

        resolveIndex.Should().BeGreaterThanOrEqualTo(0);
        canUseIndex.Should().BeGreaterThanOrEqualTo(0);
        resolveIndex.Should().BeLessThan(canUseIndex);
        resolverBody.Should().Contain("ability.UsesActiveAttackTarget");
        resolverBody.Should().Contain("GetAttackTarget(activator)");
        resolverBody.Should().Contain("return (OBJECT_INVALID, targetLocation);");
        resolverBody.Should().Contain("return (attackTarget, GetLocation(attackTarget));");
    }

}
