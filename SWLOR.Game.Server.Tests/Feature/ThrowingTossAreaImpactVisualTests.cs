using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class ThrowingTossAreaImpactVisualTests
{
    [TestCase("ExplosiveTossAbilityDefinition.cs", "VisualEffect.Fnf_Fireball", 4)]
    [TestCase("ConcussiveTossAbilityDefinition.cs", "VisualEffect.Vfx_Fnf_Sound_Burst", 2)]
    public void Toss_EveryRankBurstsAtTheImpactArea(string fileName, string visualEffect, int ranks)
    {
        // The throw animation is skipped while moving, so the blast must still show where it lands.
        var source = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "Throwing", fileName));

        Regex.Matches(source, @"ConfigureWeaponAbility\(").Count.Should().Be(ranks);
        Regex.Matches(source, $@"areaVisualEffect:\s*{Regex.Escape(visualEffect)}\);").Count.Should().Be(ranks);
    }

    [Test]
    public void WeaponAbilities_PassTheirAreaVisualToTheSharedAreaImpact()
    {
        var baseSource = File.ReadAllText(ResolveRepositoryPath(
            "SWLOR.Game.Server", "Feature", "AbilityDefinition", "WeaponActiveAbilityDefinitionBase.cs"));
        var generator = File.ReadAllText(ResolveRepositoryPath("tools", "GenerateWeaponArchetypeImplementation.py"));

        baseSource.Should().Contain("VisualEffect areaVisualEffect = VisualEffect.None)");
        baseSource.Should().Contain("areaVisualEffect: areaVisualEffect,");
        generator.Should().Contain("areaVisualEffect: {authored_area_impacts[class_name]});",
            "regenerating weapon abilities must keep their authored area visuals");
    }

    private static string ResolveRepositoryPath(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("tests must run inside the repository");
        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }
}
