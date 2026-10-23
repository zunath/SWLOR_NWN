namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// The magnitudes of the Prospector and Treasure Seeker perks and styles that act on the Prospecting session and on refining. Every
/// value is a starting value for the simulator to tune; the perk definitions, the style status effects and the Design Bible script read
/// them and nothing else, so a tuning pass changes this file only. A ranked perk's higher rank restates what the rank below it gave,
/// because only the purchased rank's stat bonuses apply.
/// </summary>
public static class ProspectingPerkRules
{
    // Efficient Strikes (Prospector, Gathering 5 and 25): Stability off a Strike and a Heavy Strike.
    public const int EfficientStrikesStrikeRank1 = 2;
    public const int EfficientStrikesHeavyRank1 = 4;
    public const int EfficientStrikesStrikeRank2 = 4;
    public const int EfficientStrikesHeavyRank2 = 8;

    // Bulk Refining (Prospector, Gathering 10, 30 and 45): items each power core refines beyond the rank's batch.
    public const int BulkRefiningRank1 = 1;
    public const int BulkRefiningRank2 = 2;
    public const int BulkRefiningRank3 = 3;

    // Rich Veins (Prospector, Gathering 15 and 40): what claiming a rich seam costs.
    public const int RichVeinsChargeReduction = 1;
    public const int RichVeinsStabilityReductionRank2 = 8;

    // Strip Mining (Prospector style): a Strike takes more units for more Stability.
    public const int StripMiningStrikeUnits = 1;
    public const int StripMiningStrikeStability = 12;

    // Mother Lode (Prospector capstone): the first Heavy Strike of each node takes this many times its units.
    public const int MotherLodeMultiplier = 2;

    // Sharp Chisel (Treasure Seeker, Gathering 10 and 30): points off a Heavy Strike's chance to shatter an unsurveyed gem pocket.
    public const int SharpChiselRank1 = 5;
    public const int SharpChiselRank2 = 10;

    // Deep Scan (Treasure Seeker, Gathering 35): seams a Survey reveals beyond the rank's, and gem pockets first.
    public const int DeepScanReveals = 1;

    // Careful Hands (Treasure Seeker style), Prospecting half: fewer units a Strike, a gentler Survey.
    public const int CarefulHandsStrikeBigChancePoints = -15;
    public const int CarefulHandsSurveyStabilityReduction = 6;
}
