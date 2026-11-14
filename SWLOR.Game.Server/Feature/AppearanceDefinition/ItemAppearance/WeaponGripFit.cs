using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>A measured correction in the equipped weapon's attachment space. Rotation is in degrees.</summary>
// Rotation uses NWN's visual-transform heading/pitch/roll axes, in degrees.
public readonly record struct WeaponGripFit(FitVector Translation, FitVector Rotation, float Scale)
{
    public static WeaponGripFit Identity { get; } = new(FitVector.Zero, FitVector.Zero, 1f);
}
