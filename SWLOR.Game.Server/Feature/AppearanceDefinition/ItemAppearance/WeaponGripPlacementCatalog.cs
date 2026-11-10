using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal sealed class WeaponGripPlacementCatalog
{
    private static readonly Regex HandPattern = new("^p[mf][hedoz]0_hand[rl][0-9]{3,5}$", RegexOptions.CultureInvariant);
    private readonly Dictionary<(string Hand, int Model, WeaponGripHold Hold), WeaponGripFit> _fits = new();

    internal WeaponGripPlacementCatalog(IEnumerable<WeaponGripPlacementRow> rows)
    {
        foreach (var row in rows)
        {
            var hand = row.Hand?.ToLowerInvariant();
            if (hand == null || !HandPattern.IsMatch(hand) || row.Model is <= 0 or > ushort.MaxValue ||
                !Enum.IsDefined(row.Hold) || row.Hold != WeaponGripHold.Pistol && hand[9] != 'r' ||
                !Finite(row.Fit.Translation) || !Finite(row.Fit.Rotation) || row.Fit.Scale != 1f)
                throw new ArgumentException("Invalid full-size weapon grip placement.");
            if (!_fits.TryAdd((hand, row.Model, row.Hold), row.Fit))
                throw new ArgumentException("Duplicate weapon grip placement.");
        }
    }

    private static bool Finite(System.Numerics.Vector3 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);

    internal bool TryGet(string hand, int model, WeaponGripHold hold, out WeaponGripFit fit)
    {
        fit = WeaponGripFit.Identity;
        if (string.IsNullOrEmpty(hand) || !_fits.TryGetValue((hand.ToLowerInvariant(), model, hold), out var measured))
            return false;
        fit = measured;
        return true;
    }
}
