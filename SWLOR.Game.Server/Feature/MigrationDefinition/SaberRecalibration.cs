namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Calculates bounded damage and accuracy profiles for recalibrated legacy sabers.
/// </summary>
public static class SaberRecalibration
{
    public const int TierFive = 5;
    public const int ChiroTier = 6;
    public const int MaximumAccuracy = 10;
    public const int AccuracyPerEnhancementSlot = 5;
    public const int EnhancementSlotCount = 2;
    public const int BlueprintDamageAllowance = 5;
    public const int DamagePerRemainingEnhancementSlot = 4;

    /// <summary>Converts a native damage bonus to a fixed rating using its mean dice result.</summary>
    public static int CalculateLegacyDamageBonus(int dice, int die)
    {
        if (dice < 0 || die <= 0)
            return 0;
        var amount = dice == 0 ? die : dice * ((double)die + 1) / 2;
        return (int)Math.Min(int.MaxValue, Math.Round(amount, MidpointRounding.AwayFromZero));
    }

}
