namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Offsets in the native item-part visual-transform axes. These axes are distinct from the posed hand attachment.
/// </summary>
public readonly record struct HornFitOffset(float TranslateX, float TranslateY, float TranslateZ, float RotateY,
    float RotateZ = 0f, float RotateX = 0f);
