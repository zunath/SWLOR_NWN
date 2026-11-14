using FitVector = System.Numerics.Vector3;
using System.Numerics;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal readonly record struct RifleGripContact(FitVector Primary, FitVector IdleSupport, FitVector ReadySupport);
