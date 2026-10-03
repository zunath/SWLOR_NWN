using System.Numerics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Service;

public class AreaImpactTelegraphTests
{
    /// <summary>
    /// Reproduces a target changing the attack direction after the activation warning was displayed.
    /// </summary>
    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void TargetMovesSidewaysDuringCast_ImpactKeepsTheWarnedDirection(TelegraphType shape)
    {
        var activation = DirectionalGeometry(shape, Vector3.Zero, new Vector3(4f, 0f, 0f));
        var impact = DirectionalGeometry(shape, Vector3.Zero, new Vector3(0f, 4f, 0f));

        var resolved = Telegraph.ResolveImpactGeometry(impact, new[] { activation });

        resolved.Should().Be(activation, "the player must be able to dodge the warned footprint");
        Telegraph.ShouldShowImpactFlash(resolved, new[] { activation }).Should().BeFalse();
    }

    /// <summary>
    /// Ensures movement along the same aim direction preserves suppression for an unchanged footprint.
    /// </summary>
    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void TargetMovesAlongTheSameDirection_DoesNotRedrawAnUnchangedArea(TelegraphType shape)
    {
        var activation = DirectionalGeometry(shape, Vector3.Zero, new Vector3(4f, 0f, 0f));
        var impact = DirectionalGeometry(shape, Vector3.Zero, new Vector3(6f, 0f, 0f));

        var resolved = Telegraph.ResolveImpactGeometry(impact, new[] { activation });
        resolved.Should().Be(activation);
        Telegraph.ShouldShowImpactFlash(resolved, new[] { activation }).Should().BeFalse();
    }

    /// <summary>
    /// A placed sphere stays at its warned center rather than following its target.
    /// </summary>
    [Test]
    public void TargetCenteredSphereMoves_ImpactKeepsTheWarnedCenter()
    {
        var activation = new TelegraphGeometry(1, TelegraphType.Sphere, Vector3.Zero, new Vector2(5f), 0f);

        Telegraph.ResolveImpactGeometry(
                activation with { Position = new Vector3(2f, 0f, 0f) }, new[] { activation })
            .Should().Be(activation);
        Telegraph.ShouldShowImpactFlash(activation with { Rotation = MathF.PI }, new[] { activation })
            .Should().BeFalse("rotating a sphere does not change its area");
    }

    /// <summary>
    /// Requires a new marker when dimensions, origin, shape, or the containing game area changes.
    /// </summary>
    [Test]
    public void ChangedSizeOriginShapeOrArea_RequiresAnImpactFlash()
    {
        var activation = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, Vector3.UnitX);
        var changedImpacts = new[]
        {
            activation with { Size = new Vector2(12f, 5f) },
            activation with { Size = new Vector2(9.5f, 7f) },
            activation with { Position = activation.Position + Vector3.UnitX },
            activation with { Shape = TelegraphType.Line },
            activation with { Area = 2 }
        };

        foreach (var impact in changedImpacts)
            Telegraph.ShouldShowImpactFlash(impact, new[] { activation }).Should().BeTrue();
    }

    /// <summary>
    /// Treats positive and negative pi as the same direction at the angle boundary.
    /// </summary>
    [Test]
    public void EquivalentDirectionsAcrossAngleWrap_DoNotRedraw()
    {
        var activation = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, -Vector3.UnitX);
        var impact = activation with { Rotation = -MathF.PI };

        Telegraph.ShouldShowImpactFlash(impact, new[] { activation }).Should().BeFalse();
    }

    /// <summary>
    /// Keeps flashes without activation snapshots and accepts a match among multiple activation markers.
    /// </summary>
    [Test]
    public void NoMatchingActivationMarker_StillFlashesInstantAndDelayedImpacts()
    {
        var impact = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, Vector3.UnitX);

        Telegraph.ShouldShowImpactFlash(impact, null).Should().BeTrue();
        Telegraph.ShouldShowImpactFlash(impact, Array.Empty<TelegraphGeometry>()).Should().BeTrue();
        Telegraph.ShouldShowImpactFlash(impact, new[] { impact with { Shape = TelegraphType.Sphere }, impact })
            .Should().BeFalse("an additional activation marker can already describe this impact");
    }

    /// <summary>
    /// Ensures captured values remain stable when live marker data changes or the marker is removed.
    /// </summary>
    [Test]
    public void CapturedGeometrySurvivesMarkerRemovalWithoutFollowingMutableData()
    {
        var telegraphs = (Dictionary<string, ActiveTelegraph>)typeof(Telegraph)
            .GetField("_allTelegraphs", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var id = Guid.NewGuid().ToString();
        var original = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, Vector3.UnitX);
        var data = new TelegraphData
        {
            Shape = original.Shape,
            Position = original.Position,
            Size = original.Size,
            Rotation = original.Rotation
        };
        telegraphs.Add(id, new ActiveTelegraph(original.Area, 0, 1, data));
        try
        {
            var snapshots = Telegraph.CaptureGeometry(new[] { id, "missing-marker" });
            data.Position = Vector3.One;
            telegraphs.Remove(id);

            snapshots.Should().Equal(original);
            Telegraph.ShouldShowImpactFlash(original, snapshots).Should().BeFalse();
            Telegraph.ResolveImpactGeometry(original with { Position = data.Position }, snapshots).Should().Be(original);
        }
        finally
        {
            telegraphs.Remove(id);
        }
    }

    [TestCase(TelegraphType.Sphere)]
    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void CasterMovesOrTurns_ImpactKeepsTheWholeWarnedFootprint(TelegraphType shape)
    {
        var activation = new TelegraphGeometry(1, shape, Vector3.Zero, new Vector2(8f, 5f), 0f);
        var impact = activation with { Position = new Vector3(3f, 2f, 0f), Rotation = MathF.PI / 2f };

        Telegraph.ResolveImpactGeometry(impact, new[] { activation }).Should().Be(activation);
    }

    [Test]
    public void SeparatePayloads_OnlyReuseTheWarningWithMatchingShapeAndDimensions()
    {
        var warning = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, Vector3.UnitX);
        var otherShape = warning with { Shape = TelegraphType.Line };
        var otherSize = warning with { Size = new Vector2(12f, 5f) };
        var otherArea = warning with { Area = 2 };
        var movedImpact = warning with { Position = Vector3.One, Rotation = MathF.PI / 2f };

        Telegraph.ResolveImpactGeometry(otherShape, new[] { warning }).Should().Be(otherShape);
        Telegraph.ResolveImpactGeometry(otherSize, new[] { warning }).Should().Be(otherSize);
        Telegraph.ResolveImpactGeometry(otherArea, new[] { warning }).Should().Be(otherArea);
        Telegraph.ResolveImpactGeometry(movedImpact, new[] { otherShape, otherSize, otherArea, warning }).Should().Be(warning);
    }

    [Test]
    public void ImpactWithoutAnActivationWarning_UsesItsCurrentGeometry()
    {
        var impact = DirectionalGeometry(TelegraphType.Line, Vector3.One, Vector3.UnitX);

        Telegraph.ResolveImpactGeometry(impact, null).Should().Be(impact);
        Telegraph.ResolveImpactGeometry(impact, Array.Empty<TelegraphGeometry>()).Should().Be(impact);
    }

    /// <summary>
    /// Builds a directional test footprint using the production origin and length offset rules.
    /// </summary>
    private static TelegraphGeometry DirectionalGeometry(TelegraphType shape, Vector3 caster, Vector3 target)
    {
        var rotation = MathF.Atan2(target.Y - caster.Y, target.X - caster.X);
        var combatShape = shape == TelegraphType.Cone ? CombatImpactAreaShape.Cone : CombatImpactAreaShape.Line;
        return new TelegraphGeometry(
            1,
            shape,
            CombatImpactShapeGeometry.ResolveOrigin(caster, rotation, combatShape, true),
            new Vector2(CombatImpactShapeGeometry.ResolveLength(combatShape, 8f, true), 5f),
            rotation);
    }
}
