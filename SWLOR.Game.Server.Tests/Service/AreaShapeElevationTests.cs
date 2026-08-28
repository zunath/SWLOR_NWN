using System.Numerics;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.TelegraphService;

namespace SWLOR.Game.Server.Tests.Service;

public class AreaShapeElevationTests
{
    [Test]
    public void CombatImpactShape_SphereUsesHorizontalDistance()
    {
        InvokeCombatImpactShape(
                new Vector3(3f, 4f, 20f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Sphere,
                5f,
                0f)
            .Should()
            .BeTrue();
    }

    [Test]
    public void CombatImpactShape_ConeUsesHorizontalAngle()
    {
        InvokeCombatImpactShape(
                new Vector3(4f, 0f, 20f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Cone,
                6f,
                5f)
            .Should()
            .BeTrue();

        InvokeCombatImpactShape(
                new Vector3(4f, 4f, 20f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Cone,
                6f,
                5f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void CombatImpactShape_LineUsesHorizontalAxis()
    {
        InvokeCombatImpactShape(
                new Vector3(4f, 0f, 20f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Line,
                8f,
                2.5f)
            .Should()
            .BeTrue();

        InvokeCombatImpactShape(
                new Vector3(0f, 4f, 20f),
                Vector3.Zero,
                0f,
                CombatImpactAreaShape.Line,
                8f,
                2.5f)
            .Should()
            .BeFalse();
    }

    [Test]
    public void DirectionalShapeGeometry_IncludesMeleeTargetsAndPreservesForwardReach()
    {
        const float authoredLength = 8f;
        const float authoredWidth = 5f;
        var origin = CombatImpactShapeGeometry.ResolveOrigin(
            Vector3.Zero,
            0f,
            CombatImpactAreaShape.Cone);
        var length = CombatImpactShapeGeometry.ResolveLength(
            CombatImpactAreaShape.Cone,
            authoredLength);

        origin.Should().Be(new Vector3(-CombatImpactShapeGeometry.DirectionalOriginBackOffset, 0f, 0f));
        (origin.X + length).Should().Be(authoredLength,
            "moving the apex behind the caster must not shorten the authored forward reach");

        var widenedWidth = authoredWidth * length / authoredLength;
        var authoredHalfAngle = MathF.Atan(authoredWidth * 0.5f / length);
        var widenedHalfAngle = MathF.Atan(widenedWidth * 0.5f / length);
        var widenedOnlyAngle = (authoredHalfAngle + widenedHalfAngle) * 0.5f;
        var widenedOnlyProbe = origin + new Vector3(
            MathF.Cos(widenedOnlyAngle) * authoredLength,
            MathF.Sin(widenedOnlyAngle) * authoredLength,
            0f);
        InvokeCombatImpactShape(
                widenedOnlyProbe,
                origin,
                0f,
                CombatImpactAreaShape.Cone,
                length,
                authoredWidth)
            .Should().BeFalse("backing up the apex must not scale up the authored cone width");

        InvokeCombatImpactShape(
                new Vector3(0.25f, 0.45f, 0f),
                origin,
                0f,
                CombatImpactAreaShape.Cone,
                length,
                authoredWidth)
            .Should().BeTrue("a hostile touching the caster should fit within the backed-up cone");

        var lineOrigin = CombatImpactShapeGeometry.ResolveOrigin(
            Vector3.Zero,
            0f,
            CombatImpactAreaShape.Line);
        var lineLength = CombatImpactShapeGeometry.ResolveLength(
            CombatImpactAreaShape.Line,
            authoredLength);
        InvokeCombatImpactShape(
                new Vector3(-0.5f, 1f, 0f),
                lineOrigin,
                0f,
                CombatImpactAreaShape.Line,
                lineLength,
                2.5f)
            .Should().BeTrue("a hostile overlapping the caster should fit within a directional line");
        (lineOrigin.X + lineLength).Should().Be(authoredLength,
            "backing up a line must not shorten its authored forward reach");
    }

    [Test]
    public void DirectionalShapeGeometry_CapsTargetsFromTheBackedUpOrigin()
    {
        var origin = CombatImpactShapeGeometry.ResolveOrigin(
            Vector3.Zero,
            0f,
            CombatImpactAreaShape.Cone);
        var candidates = new[]
        {
            (Name: "NearCaster", Position: new Vector3(0.1f, 0f, 0f)),
            (Name: "NearApex", Position: new Vector3(-1.4f, 0f, 0f))
        };

        var selected = CombatImpactShapeGeometry.TakeClosestToOrigin(
                candidates,
                origin,
                candidate => candidate.Position,
                1)
            .Single();

        selected.Name.Should().Be("NearApex",
            "capped directional impacts must rank targets from the displayed backed-up origin, not the caster");
    }

    [Test]
    public void TelegraphShape_SphereUsesHorizontalDistance()
    {
        InvokeTelegraphShape(
                new Vector3(3f, 4f, -20f),
                TelegraphType.Sphere,
                new Vector2(5f, 5f))
            .Should()
            .BeTrue();
    }

    [Test]
    public void TelegraphShape_ConeUsesHorizontalAngle()
    {
        InvokeTelegraphShape(
                new Vector3(4f, 0f, -20f),
                TelegraphType.Cone,
                new Vector2(6f, 5f))
            .Should()
            .BeTrue();

        InvokeTelegraphShape(
                new Vector3(4f, 4f, -20f),
                TelegraphType.Cone,
                new Vector2(6f, 5f))
            .Should()
            .BeFalse();
    }

    [Test]
    public void TelegraphShape_LineUsesHorizontalAxis()
    {
        InvokeTelegraphShape(
                new Vector3(4f, 0f, -20f),
                TelegraphType.Line,
                new Vector2(8f, 2.5f))
            .Should()
            .BeTrue();

        InvokeTelegraphShape(
                new Vector3(0f, 4f, -20f),
                TelegraphType.Line,
                new Vector2(8f, 2.5f))
            .Should()
            .BeFalse();
    }

    private static bool InvokeCombatImpactShape(
        Vector3 position,
        Vector3 origin,
        float rotation,
        CombatImpactAreaShape shape,
        float lengthOrRadius,
        float width)
    {
        var method = typeof(Ability)
            .GetMethod("IsPositionInCombatImpactShape", BindingFlags.Static | BindingFlags.NonPublic)!;

        return (bool)method.Invoke(
            null,
            new object[]
            {
                position,
                origin,
                rotation,
                shape,
                lengthOrRadius,
                width
            })!;
    }

    private static bool InvokeTelegraphShape(Vector3 position, TelegraphType shape, Vector2 size)
    {
        var method = typeof(Telegraph)
            .GetMethod("IsPositionInTelegraph", BindingFlags.Static | BindingFlags.NonPublic)!;
        var data = new TelegraphData
        {
            Shape = shape,
            Position = Vector3.Zero,
            Rotation = 0f,
            Size = size
        };

        return (bool)method.Invoke(null, new object[] { position, data })!;
    }
}
