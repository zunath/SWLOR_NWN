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
        // WBwSh 103 (catalog 310), actual Griff contact sections against pmh0_handr002.
        // These normal male human resources have identical rendered hand geometry and the same hook.
        foreach (var part in new[] { 2, 6, 7, 113, 150, 151, 152 })
            fits[(BaseItem.Sling, 101, 310, 101, $"pmh0_handr{part:D3}", "")] = new FitVector(.014f, -.022f, -.034f);
        return fits;
    }

    internal static FitVector GetFit(BaseItem type, int middle, int top, int bottom, string hand, string supportHand)
    {
        if (string.IsNullOrWhiteSpace(hand)) return FitVector.Zero;
        if (type == BaseItem.CompanionPistol) type = BaseItem.Sling;
        return Fits.GetValueOrDefault((type, top, middle, bottom, hand.ToLowerInvariant(),
            RequiresSupportHand(type) ? (supportHand ?? string.Empty).ToLowerInvariant() : string.Empty));
    }

    private static bool RequiresSupportHand(BaseItem type) => type is BaseItem.Rifle or BaseItem.GreatSword or
        BaseItem.QuarterStaff or BaseItem.TwoBladedSword or BaseItem.TwinElectroBlade or BaseItem.Saberstaff;

    internal static string ResolveHandModel(string race, Gender gender, string side, int natural, int armor, int robe)
    {
        // Costume hands require a measured costume profile, never the hidden natural hand's fit.
        if (robe > 0 || string.IsNullOrWhiteSpace(race) || race.Length != 1 ||
            gender is not (Gender.Male or Gender.Female) || side is not ("r" or "l")) return string.Empty;
        var part = armor > 2 ? armor : natural;
        return part > 0 ? $"p{(gender == Gender.Female ? 'f' : 'm')}{race.ToLowerInvariant()}0_hand{side}{part:D3}" : string.Empty;
    }
}
