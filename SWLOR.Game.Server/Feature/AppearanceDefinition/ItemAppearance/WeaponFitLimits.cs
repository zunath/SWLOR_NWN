using System;
using System.Globalization;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// The small corrections a player may apply to a held weapon. Fits are stored as whole steps so
/// they never drift, and every stored value is clamped again before it is shown or applied.
/// </summary>
public static class WeaponFitLimits
{
    public const float TranslationStep = 0.005f;
    public const int MaximumTranslationSteps = 10;
    public const float RotationStep = 1f;
    public const int MaximumRotationSteps = 10;
    public const float ScaleStep = 0.01f;
    public const int MaximumScaleSteps = 10;

    public static int GetMaximumSteps(WeaponFitAxis axis) => axis switch
    {
        WeaponFitAxis.TranslateX or WeaponFitAxis.TranslateY or WeaponFitAxis.TranslateZ => MaximumTranslationSteps,
        WeaponFitAxis.RotateX or WeaponFitAxis.RotateY or WeaponFitAxis.RotateZ => MaximumRotationSteps,
        WeaponFitAxis.Scale => MaximumScaleSteps,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };

    public static int ClampSteps(WeaponFitAxis axis, int steps)
    {
        var maximum = GetMaximumSteps(axis);
        return Math.Clamp(steps, -maximum, maximum);
    }

    /// <summary>The engine transform value for a step count: meters, degrees, or a scale multiplier.</summary>
    public static float GetValue(WeaponFitAxis axis, int steps)
    {
        steps = ClampSteps(axis, steps);
        return axis switch
        {
            WeaponFitAxis.TranslateX or WeaponFitAxis.TranslateY or WeaponFitAxis.TranslateZ => steps * TranslationStep,
            WeaponFitAxis.RotateX or WeaponFitAxis.RotateY or WeaponFitAxis.RotateZ => steps * RotationStep,
            WeaponFitAxis.Scale => 1f + steps * ScaleStep,
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
    }

    /// <summary>Player-facing text: centimeters, degrees, or percent of the authored size.</summary>
    public static string Format(WeaponFitAxis axis, int steps)
    {
        steps = ClampSteps(axis, steps);
        return axis switch
        {
            WeaponFitAxis.TranslateX or WeaponFitAxis.TranslateY or WeaponFitAxis.TranslateZ =>
                (steps * TranslationStep * 100f).ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + " cm",
            WeaponFitAxis.RotateX or WeaponFitAxis.RotateY or WeaponFitAxis.RotateZ =>
                (steps * RotationStep).ToString("+0;-0;0", CultureInfo.InvariantCulture) + " deg",
            WeaponFitAxis.Scale => (100 + steps).ToString(CultureInfo.InvariantCulture) + "%",
            _ => throw new ArgumentOutOfRangeException(nameof(axis))
        };
    }
}
