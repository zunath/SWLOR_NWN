using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

internal static class RifleGripFits
{
    internal static bool IsReadyAnimation(int animation) => animation is 1 or 9;

    internal static bool TryGetPlacement(int model, string hand, string supportHand, out WeaponGripFit fit) =>
        TryGetPosePlacement(model, hand, supportHand, false, out fit);

    internal static bool TryGetPosePlacement(int model, string hand, string supportHand, bool ready,
        out WeaponGripFit fit)
    {
        fit = WeaponGripFit.Identity;
        if (string.IsNullOrEmpty(hand) || string.IsNullOrEmpty(supportHand) ||
            hand.Length < 11 || supportHand.Length < 11 || char.ToLowerInvariant(hand[9]) != 'r' || char.ToLowerInvariant(supportHand[9]) != 'l' ||
            !hand.AsSpan(0, 4).Equals(supportHand.AsSpan(0, 4), StringComparison.OrdinalIgnoreCase)) return false;

        if (WeaponGripPlacements.TryGet(hand, model,
                ready ? WeaponGripHold.RifleReady : WeaponGripHold.RifleIdle, out fit)) return true;

        if (!RifleGripMeasurements.Hands.TryGetValue(hand, out var primary) ||
            !RifleGripMeasurements.Hands.ContainsKey(supportHand) ||
            !RifleGripMeasurements.Handles.TryGetValue(model, out var handle)) return false;

        // The rifle hook rotates relative to the hand between resting and aiming.
        // Seat each hold independently; averaging pulls the grip away in both.
        var contact = ready ? primary.ReadyContact : primary.IdleContact;
        fit = new WeaponGripFit(contact - handle.Primary, FitVector.Zero, 1f);
        return true;
    }
}
