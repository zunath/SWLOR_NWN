using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;
using FitQuaternion = System.Numerics.Quaternion;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal static class WeaponGripFits
{
    private static readonly Dictionary<(BaseItem Type, int Top, int Middle, int Bottom, string Hand, string SupportHand), WeaponGripFit> Fits = new()
    {
        // Full-size native 301 calibration; source handle neck is (0, .06, -.022215).
        [(BaseItem.Sling, 101, 301, 101, "pfh0_handr204", "")] = new(
            new FitVector(.003f, -.04f, .02f), FitVector.Zero, 1f),
        [(BaseItem.Sling, 101, 301, 101, "pfh0_handl204", "")] = new(
            new FitVector(-.003f, -.04f, .02f), FitVector.Zero, 1f)
    };

    internal static WeaponGripFit GetPlacement(BaseItem type, int middle, int top, int bottom, string hand, string supportHand)
    {
        return TryGetPlacement(type, middle, top, bottom, hand, supportHand, out var fit)
            ? fit : WeaponGripFit.Identity;
    }

    internal static bool TryGetPlacement(BaseItem type, int middle, int top, int bottom, string hand,
        string supportHand, out WeaponGripFit fit)
    {
        fit = WeaponGripFit.Identity;
        if (string.IsNullOrWhiteSpace(hand)) return false;
        if (type is BaseItem.Pistol or BaseItem.LegacyPistol or BaseItem.CompanionPistol) type = BaseItem.Sling;
        var key = (type, top, middle, bottom, hand.ToLowerInvariant(),
            RequiresSupportHand(type) ? (supportHand ?? string.Empty).ToLowerInvariant() : string.Empty);
        if (Fits.TryGetValue(key, out fit)) return true;
        if (type == BaseItem.Sling && top == 101 && bottom == 101 &&
            WeaponGripPlacements.TryGet(hand, middle, WeaponGripHold.Pistol, out fit)) return true;
        if (type == BaseItem.Rifle && top == 101 && bottom == 101)
            return RifleGripFits.TryGetPlacement(middle, hand, supportHand, out fit);
        if (type != BaseItem.Sling || top != 101 || bottom != 101 ||
            !PistolGripMeasurements.Hands.TryGetValue(hand, out var frame) ||
            !PistolGripMeasurements.Handles.TryGetValue(middle, out var neck))
        {
            fit = WeaponGripFit.Identity;
            return false;
        }
        fit = PistolGripMeasurements.Pincers.TryGetValue(hand, out var pinch)
            ? new WeaponGripFit(pinch - PistolGripMeasurements.GripCenters[middle], FitVector.Zero, 1f)
            : SeatHandle(frame, neck);
        return true;
    }

    private static WeaponGripFit SeatHandle(WeaponGripFrame hand, FitVector neck)
    {
        const float radians = MathF.PI / 180f;
        // NWN's visual axes are heading, pitch, roll. In MDL space these
        // rotate about Z, X, Y respectively, composed as Rz * Rx * Ry.
        var heading = FitQuaternion.CreateFromAxisAngle(FitVector.UnitZ, hand.Rotation.X * radians);
        var pitch = FitQuaternion.CreateFromAxisAngle(FitVector.UnitX, hand.Rotation.Y * radians);
        var roll = FitQuaternion.CreateFromAxisAngle(FitVector.UnitY, hand.Rotation.Z * radians);
        var rotation = FitQuaternion.Concatenate(FitQuaternion.Concatenate(roll, pitch), heading);
        return new WeaponGripFit(hand.Neck - FitVector.Transform(neck, rotation), hand.Rotation, 1f);
    }

    internal static FitVector GetFit(BaseItem type, int middle, int top, int bottom, string hand, string supportHand) =>
        GetPlacement(type, middle, top, bottom, hand, supportHand).Translation;

    private static bool RequiresSupportHand(BaseItem type) => type is BaseItem.Rifle or BaseItem.GreatSword or
        BaseItem.QuarterStaff or BaseItem.ShortSpear or BaseItem.TwoBladedSword or BaseItem.TwinElectroBlade or BaseItem.Saberstaff;

    internal static string ResolveHandModel(string race, Gender gender, string side, int natural, int armor, bool handHiddenByRobe)
    {
        // Costume hands require a measured costume profile, never the hidden natural hand's fit.
        if (handHiddenByRobe || string.IsNullOrWhiteSpace(race) || race.Length != 1 ||
            gender is not (Gender.Male or Gender.Female) || side is not ("r" or "l")) return string.Empty;
        var part = armor > 2 ? armor : natural;
        return part > 0 ? $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}0_hand{side}{part:D3}" : string.Empty;
    }
}
