namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Offsets in the native item-part visual-transform axes. These axes are distinct from the posed hand attachment.
/// Measured in the client: a model point p is drawn at R p + T. T is in the model's own axes, and
/// R = RotateX * RotateY * RotateZ, where RotateZ turns about the model's long (Y) axis, RotateY about its X axis and
/// RotateX about its Z axis.
/// </summary>
public readonly record struct HornFitOffset(float TranslateX, float TranslateY, float TranslateZ, float RotateY,
    float RotateZ = 0f, float RotateX = 0f);
