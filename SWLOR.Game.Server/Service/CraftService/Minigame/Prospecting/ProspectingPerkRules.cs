namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// The magnitudes of the Prospector and Treasure Seeker perks and styles that act on the Prospecting session and on refining. Every
/// value is a starting value for the simulator to tune; the perk definitions, the style status effects and the Design Bible script read
/// them and nothing else, so a tuning pass changes this file only. A ranked perk's higher rank restates what the rank below it gave,
/// because only the purchased rank's stat bonuses apply.
/// <para>
/// The perks spend the resources a session has to spare. A skilled session already claims nearly every seam it can (Stability decides how
/// many) and so already sits at the plan's +15% cap on items per node; a perk that saves Stability, or adds an item or a gem, would lift
/// it past the cap, and one that cuts the actions of a session would take it under the 3 to 8 actions a node should last. The Prospector
/// line therefore saves time and power cores without changing what an action does, and its style trades items for time.
/// </para>
/// </summary>
public static class ProspectingPerkRules
{
    // Efficient Strikes (Prospector, Gathering 5 and 25): tenths of a second off a Strike and a Heavy Strike.
    public const int EfficientStrikesStrikeTenthsRank1 = 5;
    public const int EfficientStrikesHeavyTenthsRank1 = 10;
    public const int EfficientStrikesStrikeTenthsRank2 = 10;
    public const int EfficientStrikesHeavyTenthsRank2 = 20;

    // Bulk Refining (Prospector, Gathering 10, 30 and 45): items each power core refines beyond the rank's batch.
    public const int BulkRefiningRank1 = 1;
    public const int BulkRefiningRank2 = 2;
    public const int BulkRefiningRank3 = 3;

    // Rich Veins (Prospector, Gathering 15 and 40): tenths of a second off the Extract of a rich seam.
    public const int RichVeinsTimeTenthsRank1 = 5;
    public const int RichVeinsTimeTenthsRank2 = 10;

    // Strip Mining (Prospector style): a quicker Strike that costs more Stability, and slower work on the seams.
    public const int StripMiningStrikeTimeTenths = 15;
    public const int StripMiningStrikeStability = 10;
    public const int StripMiningSeamWorkTimeTenths = 10;

    // Mother Lode (Prospector capstone): the first Strike or Heavy Strike of each node takes this share of its time.
    public const int MotherLodeTimePercent = 50;

    // Sharp Chisel (Treasure Seeker, Gathering 10 and 30): points off a Heavy Strike's chance to shatter an unsurveyed gem pocket.
    public const int SharpChiselRank1 = 5;
    public const int SharpChiselRank2 = 10;

    // Deep Scan (Treasure Seeker, Gathering 35): seams a Survey reveals beyond the rank's, and gem pockets first.
    public const int DeepScanReveals = 1;

    /// <summary>A tenth-of-a-second figure as the text says it: 5 is "0.5", 10 is "1", 15 is "1.5".</summary>
    public static string SecondsText(int tenths) =>
        (tenths / 10.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>"second" for exactly one second and "seconds" for any other figure.</summary>
    public static string SecondsWord(int tenths) => tenths == 10 ? "second" : "seconds";
}
