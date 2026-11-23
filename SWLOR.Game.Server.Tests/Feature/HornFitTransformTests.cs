using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class HornFitTransformTests
{
    private static readonly (double X, double Y, double Z)[] Points =
    {
        (0d, 0d, 0d), (-.06d, .318d, .035d), (.077d, -.26d, -.025d), (.11d, -.6d, -.06d), (.2d, .1d, -.15d)
    };

    [Test]
    public void TheClientsAxesTurnTheLongAxisOnlyForRotateZ()
    {
        Same(HornFitTransform.Apply(new HornFitOffset(0f, 0f, 0f, 0f, 180f), 0d, 1d, 0d), (0d, 1d, 0d));
        Same(HornFitTransform.Apply(new HornFitOffset(0f, 0f, 0f, 180f), 0d, 1d, 0d), (0d, -1d, 0d));
        Same(HornFitTransform.Apply(new HornFitOffset(0f, 0f, 0f, 0f, 0f, 180f), 0d, 1d, 0d), (0d, -1d, 0d));
        Same(HornFitTransform.Apply(new HornFitOffset(.1f, .2f, .3f, 0f), 0d, 1d, 0d), (.1d, 1.2d, .3d));
    }

    [Test]
    public void ComposingWithNoFitKeepsTheOtherFit()
    {
        var fit = new HornFitOffset(-.035f, .065f, -.017f, 12.3f, -152.4f, 28.6f);
        foreach (var point in Points)
        {
            Same(HornFitTransform.Apply(HornFitTransform.Compose(default, fit), point.X, point.Y, point.Z),
                HornFitTransform.Apply(fit, point.X, point.Y, point.Z));
            Same(HornFitTransform.Apply(HornFitTransform.Compose(fit, default), point.X, point.Y, point.Z),
                HornFitTransform.Apply(fit, point.X, point.Y, point.Z));
        }
    }

    [TestCase(.02f, 0f, 0f, -13f, 0f, 0f)]
    [TestCase(0f, .02f, -.26f, 12f, 0f, 0f)]
    [TestCase(.03f, -.01f, .03f, -12f, 25f, -7f)]
    public void AComposedFitDrawsEveryPointWhereTheTwoFitsInTurnDo(float x, float y, float z, float rotateY,
        float rotateZ, float rotateX)
    {
        var outer = new HornFitOffset(x, y, z, rotateY, rotateZ, rotateX);
        var inner = new HornFitOffset(-.035f, .065f, -.017f, 12.3f, -152.4f, 28.6f);
        var composed = HornFitTransform.Compose(outer, inner);
        foreach (var point in Points)
        {
            var once = HornFitTransform.Apply(inner, point.X, point.Y, point.Z);
            Same(HornFitTransform.Apply(composed, point.X, point.Y, point.Z),
                HornFitTransform.Apply(outer, once.X, once.Y, once.Z));
        }
    }

    private static void Same((double X, double Y, double Z) actual, (double X, double Y, double Z) expected)
    {
        actual.X.Should().BeApproximately(expected.X, 1e-5);
        actual.Y.Should().BeApproximately(expected.Y, 1e-5);
        actual.Z.Should().BeApproximately(expected.Z, 1e-5);
    }
}
