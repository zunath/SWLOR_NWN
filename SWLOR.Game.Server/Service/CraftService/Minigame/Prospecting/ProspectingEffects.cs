using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// What a harvester's perks and active style change in a Prospecting session, as the stat totals they grant. The session reads them once
/// when it begins (and again while the harvester has not acted, if the style changes) and the engine reads only this record, never a
/// perk, so previews and results stay pure. Zero (or false) changes nothing.
/// </summary>
/// <param name="StrikeStabilityReduction">Stability a Strike costs less, to a minimum of 1. A style may make it negative.</param>
/// <param name="HeavyStrikeStabilityReduction">Stability a Heavy Strike costs less, to a minimum of 1.</param>
/// <param name="StrikeUnits">Units a Strike takes beyond its usual 1 or 2.</param>
/// <param name="StrikeBigChancePoints">Percentage points added to the chance a Strike takes its larger result. A style may make it negative.</param>
/// <param name="RichExtractChargeReduction">Charges an Extract of a rich seam costs less, to a minimum of 1.</param>
/// <param name="RichExtractStabilityReduction">Stability an Extract of a rich seam costs less, to a minimum of 1.</param>
/// <param name="SeesSeamTypes">Whether the window shows how many seams the node holds and what each is, before they are found.</param>
/// <param name="MotherLode">Whether the first Heavy Strike of each node takes <see cref="ProspectingPerkRules.MotherLodeMultiplier"/> times its units.</param>
/// <param name="ShatterReduction">Percentage points off a Heavy Strike's chance to shatter a gem pocket that has not been surveyed.</param>
/// <param name="SurveyReveals">Seams a Survey reveals beyond its rank's, never more than a node holds.</param>
/// <param name="PocketsFirst">Whether a Survey reveals gem pockets before rich seams.</param>
/// <param name="SurveyStabilityReduction">Stability a Survey costs less, to a minimum of 1.</param>
public sealed record ProspectingEffects(
    int StrikeStabilityReduction = 0,
    int HeavyStrikeStabilityReduction = 0,
    int StrikeUnits = 0,
    int StrikeBigChancePoints = 0,
    int RichExtractChargeReduction = 0,
    int RichExtractStabilityReduction = 0,
    bool SeesSeamTypes = false,
    bool MotherLode = false,
    int ShatterReduction = 0,
    int SurveyReveals = 0,
    bool PocketsFirst = false,
    int SurveyStabilityReduction = 0)
{
    /// <summary>A harvester with no perks and no style.</summary>
    public static readonly ProspectingEffects None = new();

    /// <summary>The effects of a harvester whose stat totals <paramref name="read"/> returns.</summary>
    public static ProspectingEffects FromStats(Func<StatType, int> read)
    {
        if (read == null)
            throw new ArgumentNullException(nameof(read));

        return new ProspectingEffects(
            read(StatType.ProspectingStrikeStabilityReduction),
            read(StatType.ProspectingHeavyStrikeStabilityReduction),
            Math.Max(0, read(StatType.ProspectingStrikeUnits)),
            read(StatType.ProspectingStrikeBigChancePoints),
            Math.Max(0, read(StatType.ProspectingRichExtractChargeReduction)),
            Math.Max(0, read(StatType.ProspectingRichExtractStabilityReduction)),
            read(StatType.ProspectingSeamSightUnlocked) > 0,
            read(StatType.ProspectingMotherLodeUnlocked) > 0,
            Math.Max(0, read(StatType.ProspectingShatterReduction)),
            Math.Max(0, read(StatType.ProspectingSurveyReveals)),
            read(StatType.ProspectingPocketsFirstUnlocked) > 0,
            read(StatType.ProspectingSurveyStabilityReduction));
    }
}
