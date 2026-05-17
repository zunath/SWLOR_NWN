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
