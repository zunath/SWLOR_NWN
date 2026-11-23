using System;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Combines two horn fits in the engine's item-part transform convention (see <see cref="HornFitOffset"/>), so one
/// instrument can be placed relative to where another instrument's fit puts it.
/// </summary>
public static class HornFitTransform
{
    /// <summary>The single fit that applies <paramref name="inner"/> first and then <paramref name="outer"/>.</summary>
    public static HornFitOffset Compose(HornFitOffset outer, HornFitOffset inner)
    {
        var outerRotation = Rotation(outer);
        var innerRotation = Rotation(inner);
        var rotation = Multiply(outerRotation, innerRotation);
        var translation = Transform(outerRotation, inner.TranslateX, inner.TranslateY, inner.TranslateZ);

        return new HornFitOffset(
            (float)(translation[0] + outer.TranslateX),
            (float)(translation[1] + outer.TranslateY),
            (float)(translation[2] + outer.TranslateZ),
            (float)Degrees(Math.Asin(Math.Clamp(rotation[2, 1], -1d, 1d))),
            (float)Degrees(Math.Atan2(-rotation[2, 0], rotation[2, 2])),
            (float)Degrees(Math.Atan2(-rotation[0, 1], rotation[1, 1])));
    }

    /// <summary>Where <paramref name="fit"/> draws a model-space point.</summary>
    public static (double X, double Y, double Z) Apply(HornFitOffset fit, double x, double y, double z)
    {
        var point = Transform(Rotation(fit), x, y, z);
        return (point[0] + fit.TranslateX, point[1] + fit.TranslateY, point[2] + fit.TranslateZ);
    }

    private static double[,] Rotation(HornFitOffset fit) =>
        Multiply(Multiply(AboutZ(fit.RotateX), AboutX(fit.RotateY)), AboutY(fit.RotateZ));

    private static double[,] AboutX(double degrees)
    {
        var (sin, cos) = Math.SinCos(Radians(degrees));
        return new[,] { { 1d, 0d, 0d }, { 0d, cos, -sin }, { 0d, sin, cos } };
    }

    private static double[,] AboutY(double degrees)
    {
        var (sin, cos) = Math.SinCos(Radians(degrees));
        return new[,] { { cos, 0d, sin }, { 0d, 1d, 0d }, { -sin, 0d, cos } };
    }

    private static double[,] AboutZ(double degrees)
    {
        var (sin, cos) = Math.SinCos(Radians(degrees));
        return new[,] { { cos, -sin, 0d }, { sin, cos, 0d }, { 0d, 0d, 1d } };
    }

    private static double[,] Multiply(double[,] left, double[,] right)
    {
        var result = new double[3, 3];
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 3; column++)
        for (var k = 0; k < 3; k++)
            result[row, column] += left[row, k] * right[k, column];
        return result;
    }

    private static double[] Transform(double[,] rotation, double x, double y, double z) => new[]
    {
        rotation[0, 0] * x + rotation[0, 1] * y + rotation[0, 2] * z,
        rotation[1, 0] * x + rotation[1, 1] * y + rotation[1, 2] * z,
        rotation[2, 0] * x + rotation[2, 1] * y + rotation[2, 2] * z
    };

    private static double Radians(double degrees) => degrees * Math.PI / 180d;

    private static double Degrees(double radians) => radians * 180d / Math.PI;
}
