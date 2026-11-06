namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// A held horn's offset in item-local space: X is sideways, +Y lifts it back off the hands, +Z runs down the shaft,
/// and Y rotation pivots at the grip (negative tips the mouth end forward).
/// </summary>
public readonly record struct HornFitOffset(float TranslateX, float TranslateY, float TranslateZ, float RotateY);
