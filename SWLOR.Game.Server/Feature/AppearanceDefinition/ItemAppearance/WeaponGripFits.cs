using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal static class WeaponGripFits
{
    // Exact assemblies and rendered hand resources only. Missing measurements retain the authored fit.
    private static readonly Dictionary<(BaseItem Type, int Top, int Middle, int Bottom, string Hand, string SupportHand), FitVector> Fits = BuildFits();

    private static Dictionary<(BaseItem Type, int Top, int Middle, int Bottom, string Hand, string SupportHand), FitVector> BuildFits()
    {
        var fits = new Dictionary<(BaseItem, int, int, int, string, string), FitVector>();
        // The inspected native baselines below retain their authored attachment.
        foreach (var gender in new[] { 'm', 'f' })
        foreach (var side in new[] { 'r', 'l' })
            fits[(BaseItem.Sling, 101, 310, 101, $"p{gender}h0_hand{side}001", "")] = FitVector.Zero;
        fits[(BaseItem.Sling, 101, 310, 101, "pmh0_handr002", "")] = FitVector.Zero;
        fits[(BaseItem.Rifle, 101, 213, 101, "pmh0_handr001", "pmh0_handl001")] = FitVector.Zero;
        // Pistol #310, male Wookiee hand #214: seat the handle behind the fingertips.
        // Measured against the actual grip neck and checked from both sides, idle and attack.
        fits[(BaseItem.Sling, 101, 310, 101, "pme0_handr214", "")] = new FitVector(0f, 0f, .035f);
        return fits;
    }

    internal static FitVector GetFit(BaseItem type, int middle, int top, int bottom, string hand, string supportHand)
    {
        if (string.IsNullOrWhiteSpace(hand) || type is BaseItem.PowerGlove or BaseItem.CompanionPowerGlove)
            return FitVector.Zero;
        if (type == BaseItem.CompanionPistol) type = BaseItem.Sling;
        return Fits.GetValueOrDefault((type, top, middle, bottom, hand.ToLowerInvariant(),
            RequiresSupportHand(type) ? (supportHand ?? string.Empty).ToLowerInvariant() : string.Empty));
    }

    private static bool RequiresSupportHand(BaseItem type) => type is BaseItem.Rifle or BaseItem.GreatSword or
        BaseItem.QuarterStaff or BaseItem.ShortSpear or BaseItem.TwoBladedSword or BaseItem.TwinElectroBlade or BaseItem.Saberstaff;

    internal static string ResolveHandModel(string race, Gender gender, string side, int natural, int armor, int robe)
    {
        // Costume hands require a measured costume profile, never the hidden natural hand's fit.
        if (robe > 0 || string.IsNullOrWhiteSpace(race) || race.Length != 1 ||
            gender is not (Gender.Male or Gender.Female) || side is not ("r" or "l")) return string.Empty;
        var part = armor > 2 ? armor : natural;
        return part > 0 ? $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}0_hand{side}{part:D3}" : string.Empty;
    }
}
