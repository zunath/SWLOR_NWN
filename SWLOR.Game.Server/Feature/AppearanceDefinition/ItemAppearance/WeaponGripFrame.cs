using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal readonly record struct WeaponGripFrame(FitVector Neck, FitVector Rotation);
