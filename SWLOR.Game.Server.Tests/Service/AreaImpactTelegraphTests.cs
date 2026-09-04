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
    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void TargetMovesSidewaysDuringCast_ShowsTheNewImpactDirection(TelegraphType shape)
    {
        var activation = DirectionalGeometry(shape, Vector3.Zero, new Vector3(4f, 0f, 0f));
        var impact = DirectionalGeometry(shape, Vector3.Zero, new Vector3(0f, 4f, 0f));

        Telegraph.ShouldShowImpactFlash(impact, new[] { activation }).Should().BeTrue(
            "the old warning faced east, but this cast now strikes north");
    }

    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void TargetMovesAlongTheSameDirection_DoesNotRedrawAnUnchangedArea(TelegraphType shape)
    {
        var activation = DirectionalGeometry(shape, Vector3.Zero, new Vector3(4f, 0f, 0f));
        var impact = DirectionalGeometry(shape, Vector3.Zero, new Vector3(6f, 0f, 0f));

        Telegraph.ShouldShowImpactFlash(impact, new[] { activation }).Should().BeFalse();
    }

    [Test]
    public void TargetCenteredSphereMoves_ShowsItsNewCenter()
    {
        var activation = new TelegraphGeometry(1, TelegraphType.Sphere, Vector3.Zero, new Vector2(5f), 0f);

        Telegraph.ShouldShowImpactFlash(
                activation with { Position = new Vector3(2f, 0f, 0f) }, new[] { activation })
            .Should().BeTrue();
        Telegraph.ShouldShowImpactFlash(activation with { Rotation = MathF.PI }, new[] { activation })
            .Should().BeFalse("rotating a sphere does not change its area");
    }

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

    [Test]
    public void EquivalentDirectionsAcrossAngleWrap_DoNotRedraw()
    {
        var activation = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, -Vector3.UnitX);
        var impact = activation with { Rotation = -MathF.PI };

        Telegraph.ShouldShowImpactFlash(impact, new[] { activation }).Should().BeFalse();
    }

    [Test]
    public void NoMatchingActivationMarker_StillFlashesInstantAndDelayedImpacts()
    {
        var impact = DirectionalGeometry(TelegraphType.Cone, Vector3.Zero, Vector3.UnitX);

        Telegraph.ShouldShowImpactFlash(impact, null).Should().BeTrue();
        Telegraph.ShouldShowImpactFlash(impact, Array.Empty<TelegraphGeometry>()).Should().BeTrue();
        Telegraph.ShouldShowImpactFlash(impact, new[] { impact with { Shape = TelegraphType.Sphere }, impact })
            .Should().BeFalse("an additional activation marker can already describe this impact");
    }

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
            Telegraph.ShouldShowImpactFlash(original with { Position = data.Position }, snapshots).Should().BeTrue();
        }
        finally
        {
            telegraphs.Remove(id);
        }
    }

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
