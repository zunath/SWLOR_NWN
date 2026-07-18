using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Service;

public class TelegraphTests
{
    private const int PackedRotationShift = 21;
    private const int PackedRotationMask = 0x3ff;

    [Test]
    public void PackTelegraphData_LineKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Line, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    [Test]
    public void PackTelegraphData_ConeKeepsGameplayRotation()
    {
        var packed = InvokePackTelegraphData(TelegraphType.Cone, 0f);

        ExtractPackedRotation(packed).Should().Be(0);
    }

    [Test]
    public void ApplyTelegraphedCombatImpact_DefaultsToAVisibleImpactFlash()
    {
        // Instant-cast area abilities cannot gate damage behind a pre-cast telegraph without
        // contradicting the Bible, so the impact flash is what makes them visible at all. If this
        // default is ever zeroed, every instant area ability silently stops rendering.
        var parameter = typeof(Ability)
            .GetMethod(nameof(Ability.ApplyTelegraphedCombatImpact), BindingFlags.Static | BindingFlags.Public)!
            .GetParameters()
            .Single(x => x.Name == "impactFlashDuration");

        parameter.HasDefaultValue.Should().BeTrue();
        parameter.DefaultValue.Should().Be(Ability.DefaultImpactFlashDuration);
        Ability.DefaultImpactFlashDuration.Should().BeGreaterThan(0f);
    }

    [Test]
    public void ApplyTelegraphedCombatImpact_FlashesTheShapeOnTheInstantPath()
    {
        // Guards the wiring itself: the zero-telegraph branch must still render something.
        var source = File.ReadAllText(ResolveRepositoryPath("SWLOR.Game.Server", "Service", "Ability.cs"));
        var branchStart = source.IndexOf("if (telegraphDuration <= 0f)", StringComparison.Ordinal);

        branchStart.Should().BeGreaterThan(
            -1,
            "the instant-cast branch must still be recognisable; update this test's anchor if it was reworded");

        source[branchStart..].IndexOf("ShowAreaImpactFlash(", StringComparison.Ordinal)
            .Should().BeGreaterThan(-1, "the instant-cast path must flash the area it just struck");
    }

    [Test]
    public void GeneratedWeaponAbilities_DoNotStackAPrecastTelegraphOnTopOfTheActivationDelay()
    {
        // UsePerkFeat already draws a pre-cast telegraph from the activation delay. TelegraphDuration
        // runs at impact, after the cast resolves, so deriving it from casting time would delay damage
        // twice and draw the shape twice. The generator must not emit it from CastingTime.
        var generator = File.ReadAllText(ResolveRepositoryPath("tools", "GenerateWeaponArchetypeImplementation.py"));
        var emitsTelegraphDuration = Regex.IsMatch(
            generator,
            @"add_profile_property\(\s*""TelegraphDuration""");

        emitsTelegraphDuration.Should().BeFalse(
            "TelegraphDuration is an impact-time delay and must stay hand-set, not derived from CastingTime");
    }

    private static string ResolveRepositoryPath(params string[] segments)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the repository root must be locatable from the test directory");

        return Path.Combine(new[] { directory!.FullName }.Concat(segments).ToArray());
    }

    private static int InvokePackTelegraphData(TelegraphType type, float rotation)
    {
        var method = typeof(Telegraph)
            .GetMethod("PackTelegraphData", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (int)method.Invoke(
            null,
            new object[]
            {
                type,
                TelegraphColorType.Self,
                new Vector2(8f, 2.5f),
                rotation
            })!;
    }

    private static int ExtractPackedRotation(int packed)
    {
        return (int)(((uint)packed >> PackedRotationShift) & PackedRotationMask);
    }
}
